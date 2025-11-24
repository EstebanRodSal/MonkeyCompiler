using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Antlr4.Runtime.Tree;
using Generated;

namespace MonkeyCompiler.Encoder
{
    /// <summary>
    /// Encoder: recorre el AST de Monkey y genera CIL dinámicamente con Reflection.Emit.
    /// 
    /// Diseño:
    /// - Cada expresión devuelve un System.Type que representa el tipo CLR dejado en la pila.
    /// - Se generan en MEMORIA:
    ///     * Un Assembly dinámico (Por temas de integración con la interfaz no genera un .exe)
    ///     * Un módulo
    ///     * Un tipo "Program" con métodos estáticos para cada función Monkey.
    ///     * Un método estático "MonkeyMain" para fn main():void
    ///     * Un método estático oculto "__InitGlobals" para ejecutar statements globales.
    ///     * Un método estático "Main" (entry point de .NET) que llama a __InitGlobals() y luego a MonkeyMain().
    /// </summary>
    public class Encoder : MonkeyBaseVisitor<Type>
    {
        // Infraestructura de Assembly / Módulo / Tipo principal
        private readonly AssemblyBuilder _assemblyBuilder;
        private ModuleBuilder _moduleBuilder;
        private TypeBuilder _programTypeBuilder;
        private Type _programType;

        // Métodos definidos a nivel de tipo Program (funciones Monkey, incluyendo main)
        private readonly Dictionary<string, MethodBuilder> _methods = new();

        // Tipos de parámetros por función (para no usar MethodBuilder.GetParameters())
        private readonly Dictionary<string, Type[]> _methodParamTypes = new();

        // Método de inicialización de globals
        private MethodBuilder _initGlobalsMethod;
        private ILGenerator _initGlobalsIL;

        // Método de entrada .NET (wrapper que llama a Monkey main)
        private MethodBuilder _dotNetMainMethod;

        // Estado actual de generación
        private ILGenerator _il;
        private MethodBuilder _currentMethod;
        private Type _currentReturnType = typeof(void);

        // Campos estáticos para variables globales (LET/CONST a nivel de programa)
        private readonly Dictionary<string, FieldBuilder> _globalFields = new();

        // ============================================================
        // Manejo de variables locales / argumentos
        // ============================================================

        private class VariableInfo
        {
            public bool IsArgument;
            public int ArgIndex;
            public LocalBuilder Local;
            public FieldBuilder GlobalField;
            public Type Type;
            public bool IsConst;
        }

        // Pila de scopes: el top es el scope actual
        private readonly Stack<Dictionary<string, VariableInfo>> _scopes =
            new Stack<Dictionary<string, VariableInfo>>();

        //  Tipos básicos
        private static readonly Type ClrInt = typeof(int);
        private static readonly Type ClrString = typeof(string);
        private static readonly Type ClrBool = typeof(bool);
        private static readonly Type ClrChar = typeof(char);
        private static readonly Type ClrVoid = typeof(void);

        public Encoder()
        {
            var asmName = new AssemblyName("MonkeyGeneratedProgram");
#pragma warning disable SYSLIB0003
            // Assembly solo en memoria (Run), sin guardar en disco
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                asmName, AssemblyBuilderAccess.Run);
#pragma warning restore SYSLIB0003

            _moduleBuilder = null!;
            _programTypeBuilder = null!;
            _programType = null!;
            _initGlobalsMethod = null!;
            _initGlobalsIL = null!;
            _dotNetMainMethod = null!;
            _il = null!;
            _currentMethod = null!;
        }
        

        /// <summary>
        /// Compila un programa Monkey a un assembly en memoria.
        /// El parámetro outputFileName solo se usa como nombre lógico del módulo.
        /// </summary>
        public void Compile(MonkeyParser.ProgramContext program, string outputFileName)
        {
            if (string.IsNullOrWhiteSpace(outputFileName))
                outputFileName = "MonkeyProgram";

            // Crear módulo y tipo principal SOLO EN MEMORIA
            var moduleName = Path.GetFileNameWithoutExtension(outputFileName);
#pragma warning disable SYSLIB0003
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(moduleName);
#pragma warning restore SYSLIB0003

            _programTypeBuilder = _moduleBuilder.DefineType(
                "Program",
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract
            );

            // Crear método para inicializar globals
            _initGlobalsMethod = _programTypeBuilder.DefineMethod(
                "__InitGlobals",
                MethodAttributes.Private | MethodAttributes.Static,
                ClrVoid,
                Type.EmptyTypes
            );
            _initGlobalsIL = _initGlobalsMethod.GetILGenerator();

            // Declarar firmas de todas las funciones user-defined (excepto main)
            foreach (var funcDecl in program.functionDeclaration())
            {
                DeclareFunction(funcDecl);
            }

            // Declarar la función main de Monkey como "MonkeyMain"
            DeclareMonkeyMain(program.mainFunction());

            // Definir el cuerpo de __InitGlobals con todos los statements globales
            GenerateGlobalStatements(program.statement());

            // Definir cuerpos de todas las funciones user-defined
            foreach (var funcDecl in program.functionDeclaration())
            {
                DefineFunctionBody(funcDecl);
            }

            //Definir el cuerpo de MonkeyMain
            DefineMonkeyMainBody(program.mainFunction());

            // Terminar __InitGlobals
            _initGlobalsIL.Emit(OpCodes.Ret);

            // Crear método Main de .NET que hace:
            //    __InitGlobals(); Program.MonkeyMain();
            DefineDotNetMainWrapper();

            // Crear el tipo Program en memoria
            _programType = _programTypeBuilder.CreateType();
            
        }

        /// <summary>
        /// Ejecuta en memoria el programa Monkey:
        /// llama a __InitGlobals() y luego a MonkeyMain().
        /// </summary>
        public void RunMonkeyMain()
        {
            if (_programType == null)
                throw new InvalidOperationException(" Compile() must be called first before RunMonkeyMain().");

            // Obtener métodos reales del tipo ya creado
            var initGlobals = _programType.GetMethod(
                "__InitGlobals",
                BindingFlags.NonPublic | BindingFlags.Static);

            var monkeyMain = _programType.GetMethod(
                "MonkeyMain",
                BindingFlags.Public | BindingFlags.Static);

            initGlobals?.Invoke(null, null);
            monkeyMain?.Invoke(null, null);
        }

        // ============================================================
        // Declaración de funciones
        // ============================================================

        private void DeclareFunction(MonkeyParser.FunctionDeclarationContext ctx)
        {
            var name = ctx.identifier().GetText();

            var returnType = GetClrTypeFromTypeContext(ctx.type());
            var paramTypesList = new List<Type>();

            if (ctx.functionParameters() != null)
            {
                foreach (var p in ctx.functionParameters().parameter())
                {
                    paramTypesList.Add(GetClrTypeFromTypeContext(p.type()));
                }
            }

            var paramTypes = paramTypesList.ToArray();

            var method = _programTypeBuilder.DefineMethod(
                name,
                MethodAttributes.Public | MethodAttributes.Static,
                returnType,
                paramTypes
            );

            _methods[name] = method;
            _methodParamTypes[name] = paramTypes;  // Se guardan los tipos de parámetros
        }

        private void DeclareMonkeyMain(MonkeyParser.MainFunctionContext ctx)
        {
            var method = _programTypeBuilder.DefineMethod(
                "MonkeyMain",
                MethodAttributes.Public | MethodAttributes.Static,
                ClrVoid,
                Type.EmptyTypes
            );

            _methods["main"] = method; 
            _methodParamTypes["main"] = Type.EmptyTypes;
        }

        private void DefineFunctionBody(MonkeyParser.FunctionDeclarationContext ctx)
        {
            var name = ctx.identifier().GetText();
            var method = _methods[name];

            _currentMethod = method;
            _il = method.GetILGenerator();
            _currentReturnType = method.ReturnType;

            _scopes.Clear();
            EnterScope();

            var paramTypes = _methodParamTypes[name];
            int argIndex = 0; // static method: primer arg es index 0

            if (ctx.functionParameters() != null)
            {
                var paramCtxs = ctx.functionParameters().parameter();
                for (int i = 0; i < paramCtxs.Length; i++)
                {
                    var paramName = paramCtxs[i].identifier().GetText();
                    var vi = new VariableInfo
                    {
                        IsArgument = true,
                        ArgIndex = argIndex,
                        Local = null,
                        GlobalField = null,
                        Type = paramTypes[i],
                        IsConst = false
                    };
                    CurrentScope()[paramName] = vi;
                    argIndex++;
                }
            }

            // Generar cuerpo del bloque
            Visit(ctx.blockStatement());


            if (_currentReturnType != typeof(void))
            {
                if (_currentReturnType.IsValueType)
                {
                    // default(T) para value types
                    var tmp = _il.DeclareLocal(_currentReturnType);
                    _il.Emit(OpCodes.Ldloca_S, tmp);
                    _il.Emit(OpCodes.Initobj, _currentReturnType); // tmp = default(T)
                    _il.Emit(OpCodes.Ldloc, tmp);                  // push default(T)
                }
                else
                {
                    // Referencias -> null
                    _il.Emit(OpCodes.Ldnull);
                }
            }

            // Tanto para void como no-void, siempre hay un Ret al final
            _il.Emit(OpCodes.Ret);

            ExitScope();

        }

        private void DefineMonkeyMainBody(MonkeyParser.MainFunctionContext ctx)
        {
            var method = _methods["main"]; // MonkeyMain
            _currentMethod = method;
            _il = method.GetILGenerator();
            _currentReturnType = ClrVoid;

            _scopes.Clear();
            EnterScope();

            Visit(ctx.blockStatement());

            _il.Emit(OpCodes.Ret);

            ExitScope();
        }

        private void DefineDotNetMainWrapper()
        {
            _dotNetMainMethod = _programTypeBuilder.DefineMethod(
                "Main",
                MethodAttributes.Public | MethodAttributes.Static,
                ClrVoid,
                new[] { typeof(string[]) }
            );

            var il = _dotNetMainMethod.GetILGenerator();

            // Llamar __InitGlobals()
            il.Emit(OpCodes.Call, _initGlobalsMethod);

            // Llamar MonkeyMain()
            var monkeyMain = _methods["main"];
            il.Emit(OpCodes.Call, monkeyMain);

            il.Emit(OpCodes.Ret);
        }

        // ============================================================
        // Globales
        // ============================================================

        private void GenerateGlobalStatements(MonkeyParser.StatementContext[] statements)
        {
            // El scope de globals es "nivel 0"
            _scopes.Clear();
            EnterScope();

            // Asegurar que el IL actual es el de __InitGlobals
            _currentMethod = _initGlobalsMethod;
            _il = _initGlobalsIL;
            _currentReturnType = typeof(void);

            foreach (var stmt in statements)
            {
                // Las declaraciones globales crean campos estáticos,
                // pero las expresiones se ejecutan en __InitGlobals.
                Visit(stmt);
            }

            ExitScope();
        }

        // ============================================================
        // Auxiliares de Scope
        // ============================================================

        private void EnterScope()
        {
            _scopes.Push(new Dictionary<string, VariableInfo>());
        }

        private void ExitScope()
        {
            _scopes.Pop();
        }

        private Dictionary<string, VariableInfo> CurrentScope()
        {
            return _scopes.Peek();
        }

        private VariableInfo? ResolveVariable(string name)
        {
            foreach (var scope in _scopes)
            {
                if (scope.TryGetValue(name, out var vi))
                    return vi;
            }

            if (_globalFields.TryGetValue(name, out var field))
            {
                return new VariableInfo
                {
                    IsArgument = false,
                    ArgIndex = -1,
                    Local = null,
                    GlobalField = field,
                    Type = field.FieldType,
                    IsConst = false // const/let no importa en runtime
                };
            }

            return null;
        }

        private void EmitLoadVariable(VariableInfo vi)
        {
            if (vi.IsArgument)
            {
                EmitLoadArgument(vi.ArgIndex);
            }
            else if (vi.Local != null)
            {
                _il.Emit(OpCodes.Ldloc, vi.Local);
            }
            else if (vi.GlobalField != null)
            {
                _il.Emit(OpCodes.Ldsfld, vi.GlobalField);
            }
            else
            {
                throw new InvalidOperationException("VariableInfo without the right location");
            }
        }

        private void EmitStoreVariable(VariableInfo vi)
        {
            if (vi.IsConst)
            {
                throw new InvalidOperationException("Cannot be assigned to a constant");
            }

            if (vi.IsArgument)
            {
                // En Monkey no hay asignación a parámetros, solo let, así que no se usa.
                throw new InvalidOperationException("Parameter assignation not supported");
            }
            else if (vi.Local != null)
            {
                _il.Emit(OpCodes.Stloc, vi.Local);
            }
            else if (vi.GlobalField != null)
            {
                _il.Emit(OpCodes.Stsfld, vi.GlobalField);
            }
            else
            {
                throw new InvalidOperationException("VariableInfo without the right location");
            }
        }

        private void EmitLoadArgument(int index)
        {
            switch (index)
            {
                case 0: _il.Emit(OpCodes.Ldarg_0); break;
                case 1: _il.Emit(OpCodes.Ldarg_1); break;
                case 2: _il.Emit(OpCodes.Ldarg_2); break;
                case 3: _il.Emit(OpCodes.Ldarg_3); break;
                default: _il.Emit(OpCodes.Ldarg_S, (short)index); break;
            }
        }

        // ============================================================
        // Auxiliares de tipos
        // ============================================================

        private Type GetClrTypeFromTypeContext(MonkeyParser.TypeContext ctx)
        {
            if (ctx.INT_TYPE() != null) return ClrInt;
            if (ctx.STRING_TYPE() != null) return ClrString;
            if (ctx.BOOL_TYPE() != null) return ClrBool;
            if (ctx.CHAR_TYPE() != null) return ClrChar;
            if (ctx.VOID_TYPE() != null) return ClrVoid;

            if (ctx.arrayType() != null)
            {
                var elementType = GetClrTypeFromTypeContext(ctx.arrayType().type());
                return elementType.MakeArrayType();
            }

            if (ctx.hashType() != null)
            {
                var keyType = GetClrTypeFromTypeContext(ctx.hashType().type(0));
                var valueType = GetClrTypeFromTypeContext(ctx.hashType().type(1));

                var dictGeneric = typeof(Dictionary<,>);
                return dictGeneric.MakeGenericType(keyType, valueType);
            }

            if (ctx.functionType() != null)
            {
                var funcTypeCtx = ctx.functionType();
                var paramTypes = new List<Type>();
                if (funcTypeCtx.functionParameterTypes() != null)
                {
                    foreach (var t in funcTypeCtx.functionParameterTypes().type())
                    {
                        paramTypes.Add(GetClrTypeFromTypeContext(t));
                    }
                }
                var returnType = GetClrTypeFromTypeContext(funcTypeCtx.type());

                return GetDelegateType(paramTypes.ToArray(), returnType);
            }

            throw new NotSupportedException("Unsupported types in TypeContext");
        }

        private Type GetDelegateType(Type[] paramTypes, Type returnType)
        {
            if (returnType == typeof(void))
            {
                // Action<...>
                if (paramTypes.Length == 0) return typeof(Action);
                var actionGeneric = Type.GetType($"System.Action`{paramTypes.Length}");
                if (actionGeneric == null)
                    throw new NotSupportedException("The number of parameters is too big for Action<>");
                return actionGeneric.MakeGenericType(paramTypes);
            }
            else
            {
                // Func<...,TR>
                var genArgs = paramTypes.Concat(new[] { returnType }).ToArray();
                var funcGeneric = Type.GetType($"System.Func`{genArgs.Length}");
                if (funcGeneric == null)
                    throw new NotSupportedException("The number of parameters is too big for Func<>");
                return funcGeneric.MakeGenericType(genArgs);
            }
        }

        private void EmitConvert(Type from, Type to)
        {
            if (from == to) return;

            if (from == typeof(int) && to == typeof(bool))
            {
                // int -> bool (0 = false, !=0 = true)
                _il.Emit(OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Ceq);
                _il.Emit(OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Ceq);
                return;
            }

            if (from == typeof(bool) && to == typeof(int))
            {
                // bool ya es int32 en IL, no se necesita conversión
                return;
            }

            if (from.IsValueType && to == typeof(object))
            {
                _il.Emit(OpCodes.Box, from);
                return;
            }

            if (from == typeof(object) && to.IsValueType)
            {
                _il.Emit(OpCodes.Unbox_Any, to);
                return;
            }

            throw new NotSupportedException($"Conversion from {from} to {to} not supported in encoder.");
        }

        // ============================================================
        // Statements
        // ============================================================

        public override Type VisitProgram(MonkeyParser.ProgramContext context)
        {
            // No usado: Compile se encarga de orquestar.
            return typeof(void);
        }

        public override Type VisitBlockStatement(MonkeyParser.BlockStatementContext context)
        {
            EnterScope();
            foreach (var stmt in context.statement())
            {
                Visit(stmt);
            }
            ExitScope();
            return typeof(void);
        }

        public override Type VisitBlockStmt(MonkeyParser.BlockStmtContext context)
        {
            return Visit(context.blockStatement());
        }

        public override Type VisitLetVarStmt(MonkeyParser.LetVarStmtContext context)
        {
            var name = context.identifier().GetText();
            var declaredType = GetClrTypeFromTypeContext(context.type());

            // Evaluar expresión en el IL actual
            var exprType = Visit(context.expression());
            EmitConvert(exprType, declaredType);

            if (_currentMethod == _initGlobalsMethod)
            {
                // Global -> field estático
                var field = _programTypeBuilder.DefineField(
                    name,
                    declaredType,
                    FieldAttributes.Public | FieldAttributes.Static
                );
                _globalFields[name] = field;
                _initGlobalsIL.Emit(OpCodes.Stsfld, field);
            }
            else
            {
                // Local
                var local = _il.DeclareLocal(declaredType);
                _il.Emit(OpCodes.Stloc, local);

                CurrentScope()[name] = new VariableInfo
                {
                    IsArgument = false,
                    ArgIndex = -1,
                    Local = local,
                    GlobalField = null,
                    Type = declaredType,
                    IsConst = false
                };
            }

            return typeof(void);
        }

        public override Type VisitConstVarStmt(MonkeyParser.ConstVarStmtContext context)
        {
            var name = context.identifier().GetText();
            var declaredType = GetClrTypeFromTypeContext(context.type());

            var exprType = Visit(context.expression());
            EmitConvert(exprType, declaredType);

            if (_currentMethod == _initGlobalsMethod)
            {
                var field = _programTypeBuilder.DefineField(
                    name,
                    declaredType,
                    FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly
                );
                _globalFields[name] = field;
                _initGlobalsIL.Emit(OpCodes.Stsfld, field);
            }
            else
            {
                var local = _il.DeclareLocal(declaredType);
                _il.Emit(OpCodes.Stloc, local);

                CurrentScope()[name] = new VariableInfo
                {
                    IsArgument = false,
                    ArgIndex = -1,
                    Local = local,
                    GlobalField = null,
                    Type = declaredType,
                    IsConst = true
                };
            }

            return typeof(void);
        }

        public override Type VisitReturnWithValue(MonkeyParser.ReturnWithValueContext context)
        {
            var exprType = Visit(context.expression());
            EmitConvert(exprType, _currentReturnType);
            _il.Emit(OpCodes.Ret);
            return _currentReturnType;
        }

        public override Type VisitReturnWithoutValue(MonkeyParser.ReturnWithoutValueContext context)
        {
            _il.Emit(OpCodes.Ret);
            return typeof(void);
        }

        public override Type VisitExpressionStatement(MonkeyParser.ExpressionStatementContext context)
        {
            var t = Visit(context.expression());
            if (t != typeof(void))
            {
                _il.Emit(OpCodes.Pop);
            }
            return typeof(void);
        }

        public override Type VisitIfOnly(MonkeyParser.IfOnlyContext context)
        {
            var condType = Visit(context.expression());
            EmitConvert(condType, ClrBool);

            var endLabel = _il.DefineLabel();

            _il.Emit(OpCodes.Brfalse, endLabel);

            Visit(context.blockStatement());

            _il.MarkLabel(endLabel);

            return typeof(void);
        }

        public override Type VisitIfElse(MonkeyParser.IfElseContext context)
        {
            var condType = Visit(context.expression());
            EmitConvert(condType, ClrBool);

            var elseLabel = _il.DefineLabel();
            var endLabel = _il.DefineLabel();

            _il.Emit(OpCodes.Brfalse, elseLabel);

            Visit(context.blockStatement(0));
            _il.Emit(OpCodes.Br, endLabel);

            _il.MarkLabel(elseLabel);
            Visit(context.blockStatement(1));

            _il.MarkLabel(endLabel);

            return typeof(void);
        }

        public override Type VisitPrintStatement(MonkeyParser.PrintStatementContext context)
        {
            var exprType = Visit(context.expression());

            MethodInfo mi;

            // Intentar Console.WriteLine(tipo)
            mi = typeof(Console).GetMethod("WriteLine", new[] { exprType });
            if (mi == null)
            {
                // Fallback a WriteLine(object)
                mi = typeof(Console).GetMethod("WriteLine", new[] { typeof(object) });
                if (exprType.IsValueType)
                {
                    _il.Emit(OpCodes.Box, exprType);
                }
            }

            _il.Emit(OpCodes.Call, mi);

            return typeof(void);
        }

        // ============================================================
        // Expresiones
        // ============================================================

        public override Type VisitExpression(MonkeyParser.ExpressionContext context)
        {
            // additionExpression (relOp additionExpression)*
            var leftType = Visit(context.additionExpression(0));

            for (int i = 0; i < context.relationalOp().Length; i++)
            {
                var rightType = Visit(context.additionExpression(i + 1));

                var opText = context.relationalOp(i).GetText();

                // Asegurar mismas bases
                if (leftType != rightType)
                    throw new NotSupportedException($"Comparation between {leftType} and {rightType} not supported in encoder.");

                if (leftType == ClrInt || leftType == ClrBool || leftType == ClrChar)
                {
                    // numéricos / bool / char: usamos comparaciones de enteros
                    switch (opText)
                    {
                        case "==":
                            _il.Emit(OpCodes.Ceq);
                            break;
                        case "!=":
                            _il.Emit(OpCodes.Ceq);
                            _il.Emit(OpCodes.Ldc_I4_0);
                            _il.Emit(OpCodes.Ceq);
                            break;
                        case "<":
                            _il.Emit(OpCodes.Clt);
                            break;
                        case ">":
                            _il.Emit(OpCodes.Cgt);
                            break;
                        case "<=":
                            // !(>)
                            _il.Emit(OpCodes.Cgt);
                            _il.Emit(OpCodes.Ldc_I4_0);
                            _il.Emit(OpCodes.Ceq);
                            break;
                        case ">=":
                            // !(<)
                            _il.Emit(OpCodes.Clt);
                            _il.Emit(OpCodes.Ldc_I4_0);
                            _il.Emit(OpCodes.Ceq);
                            break;
                        default:
                            throw new NotSupportedException($"Relational operator {opText} not supported");
                    }
                }
                else if (leftType == ClrString)
                {
                    // Strings: usamos String.Compare / Equals
                    MethodInfo cmp;
                    switch (opText)
                    {
                        case "==":
                            cmp = typeof(string).GetMethod("op_Equality", new[] { ClrString, ClrString });
                            _il.Emit(OpCodes.Call, cmp);
                            break;
                        case "!=":
                            cmp = typeof(string).GetMethod("op_Inequality", new[] { ClrString, ClrString });
                            _il.Emit(OpCodes.Call, cmp);
                            break;
                        case "<":
                        case ">":
                        case "<=":
                        case ">=":
                            cmp = typeof(string).GetMethod("Compare", new[] { ClrString, ClrString });
                            _il.Emit(OpCodes.Call, cmp); // int resultado
                            switch (opText)
                            {
                                case "<":
                                    _il.Emit(OpCodes.Ldc_I4_0);
                                    _il.Emit(OpCodes.Clt);
                                    break;
                                case ">":
                                    _il.Emit(OpCodes.Ldc_I4_0);
                                    _il.Emit(OpCodes.Cgt);
                                    break;
                                case "<=":
                                    // resultado <= 0
                                    _il.Emit(OpCodes.Ldc_I4_0);
                                    _il.Emit(OpCodes.Cgt); // >(0)
                                    _il.Emit(OpCodes.Ldc_I4_0);
                                    _il.Emit(OpCodes.Ceq); // !>
                                    break;
                                case ">=":
                                    _il.Emit(OpCodes.Ldc_I4_0);
                                    _il.Emit(OpCodes.Clt); // <0
                                    _il.Emit(OpCodes.Ldc_I4_0);
                                    _il.Emit(OpCodes.Ceq); // !<
                                    break;
                            }
                            break;
                        default:
                            throw new NotSupportedException($"Relational operator {opText} not supported for string.");
                    }
                }
                else
                {
                    throw new NotSupportedException($"Relatinal for type {leftType} not supported.");
                }

                leftType = ClrBool;
            }

            return leftType;
        }

        public override Type VisitAdditionExpression(MonkeyParser.AdditionExpressionContext context)
        {
            var leftType = Visit(context.multiplicationExpression(0));

            int childIndex = 1;
            while (childIndex < context.ChildCount)
            {
                var opText = context.GetChild(childIndex).GetText();
                var rightType = Visit(context.multiplicationExpression((childIndex + 1) / 2));

                if (opText == "+")
                {
                    if (leftType == ClrInt && rightType == ClrInt)
                    {
                        _il.Emit(OpCodes.Add);
                        leftType = ClrInt;
                    }
                    else if (leftType == ClrString && rightType == ClrString)
                    {
                        var concat = typeof(string).GetMethod("Concat", new[] { ClrString, ClrString });
                        _il.Emit(OpCodes.Call, concat);
                        leftType = ClrString;
                    }
                    else
                    {
                        throw new NotSupportedException($"Operator '+' is not supported between {leftType} and {rightType} in encoder.");
                    }
                }
                else if (opText == "-")
                {
                    if (leftType == ClrInt && rightType == ClrInt)
                    {
                        _il.Emit(OpCodes.Sub);
                        leftType = ClrInt;
                    }
                    else
                    {
                        throw new NotSupportedException($"Operator '-' is not supported between {leftType} and {rightType} in encoder.");
                    }
                }
                else
                {
                    throw new NotSupportedException($"Adhitive operator {opText} not recognized.");
                }

                childIndex += 2;
            }

            return leftType;
        }

        public override Type VisitMultiplicationExpression(MonkeyParser.MultiplicationExpressionContext context)
        {
            var leftType = Visit(context.elementExpression(0));

            int childIndex = 1;
            while (childIndex < context.ChildCount)
            {
                var opText = context.GetChild(childIndex).GetText();
                var rightType = Visit(context.elementExpression((childIndex + 1) / 2));

                if (leftType != ClrInt || rightType != ClrInt)
                {
                    throw new NotSupportedException("'*' and '/' are only supported for int in encoder.");
                }

                if (opText == "*")
                {
                    _il.Emit(OpCodes.Mul);
                }
                else if (opText == "/")
                {
                    _il.Emit(OpCodes.Div);
                }
                else
                {
                    throw new NotSupportedException($"Multiplicative operator {opText} not supported.");
                }

                leftType = ClrInt;
                childIndex += 2;
            }

            return leftType;
        }

        public override Type VisitElementExpression(MonkeyParser.ElementExpressionContext context)
        {
            var call = context.callExpression();
            var access = context.elementAccess();

            //Caso: hay llamada f(...)
            if (call != null)
            {
                // 1.a) primitiveExpression es un identificador: ¿función global?
                if (context.primitiveExpression() is MonkeyParser.IdentifierExprContext idCtx)
                {
                    var name = idCtx.identifier().GetText();

                    if (_methods.TryGetValue(name, out var method))
                    {
                        // Llamada directa a función estática declarada en _methods
                        var paramTypes = _methodParamTypes[name];

                        if (call.expressionList() != null)
                        {
                            var exprs = call.expressionList().expression();
                            if (exprs.Length != paramTypes.Length)
                            {
                                throw new InvalidOperationException(
                                    $"La función '{name}' espera {paramTypes.Length} argumentos y se llamaron {exprs.Length}.");
                            }

                            for (int i = 0; i < exprs.Length; i++)
                            {
                                var t = Visit(exprs[i]);
                                EmitConvert(t, paramTypes[i]);
                            }
                        }
                        else if (paramTypes.Length != 0)
                        {
                            throw new InvalidOperationException(
                                $"The function '{name}' expects {paramTypes.Length} arguments but found 0");
                        }

                        _il.Emit(OpCodes.Call, method);
                        return method.ReturnType;
                    }
                }

                // 1.b) No es función global: se trata la parte base como delegate (variable)
                //   
                var baseType = Visit(context.primitiveExpression());

                var invoke = baseType.GetMethod("Invoke");
                if (invoke == null)
                    throw new NotSupportedException($"Type {baseType} is no invokable as delegate.");

                var delegateParamInfos = invoke.GetParameters();

                if (call.expressionList() != null)
                {
                    var exprs = call.expressionList().expression();
                    if (exprs.Length != delegateParamInfos.Length)
                    {
                        throw new InvalidOperationException(
                            $"Delegate expects {delegateParamInfos.Length} arguments and found only {exprs.Length}.");
                    }

                    for (int i = 0; i < exprs.Length; i++)
                    {
                        var t = Visit(exprs[i]);
                        EmitConvert(t, delegateParamInfos[i].ParameterType);
                    }
                }
                else if (delegateParamInfos.Length != 0)
                {
                    throw new InvalidOperationException(
                        $"Delegate expects {delegateParamInfos.Length} arguments but found 0");
                }

                _il.Emit(OpCodes.Callvirt, invoke);
                return invoke.ReturnType;
            }

            // 2) Caso: NO hay llamada, pero puede haber indexación: a[i] o hash[k]
            var resultType = Visit(context.primitiveExpression());

            if (access != null)
            {
                // Indexación: array[...] o hash[...]
                var indexExprType = Visit(access.expression());

                if (resultType.IsArray)
                {
                    // Array: índice int
                    EmitConvert(indexExprType, ClrInt);
                    var elemType = resultType.GetElementType();
                    _il.Emit(OpCodes.Ldelem, elemType);
                    return elemType;
                }
                else if (resultType.IsGenericType &&
                         resultType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    // Hash: Dictionary<TKey, TValue>
                    var args = resultType.GetGenericArguments();
                    var keyType = args[0];
                    var valueType = args[1];

                    EmitConvert(indexExprType, keyType);

                    var getter = resultType.GetProperty("Item").GetGetMethod();
                    _il.Emit(OpCodes.Callvirt, getter);
                    return valueType;
                }
                else
                {
                    throw new NotSupportedException($"Indexing not supported for type {resultType}.");
                }
            }

            // 3) Solo la primitiva (variable, literal, (expr), array literal, etc.)
            return resultType;
        }


        // ============================================================
        // Primitivas
        // ============================================================

        public override Type VisitIntLiteralExpr(MonkeyParser.IntLiteralExprContext context)
        {
            var text = context.integerLiteral().INTEGER_LITERAL().GetText();
            int value = int.Parse(text);
            _il.Emit(OpCodes.Ldc_I4, value);
            return ClrInt;
        }

        public override Type VisitStringLiteralExpr(MonkeyParser.StringLiteralExprContext context)
        {
            var raw = context.stringLiteral().STRING_LITERAL().GetText();
            var value = DecodeString(raw);
            _il.Emit(OpCodes.Ldstr, value);
            return ClrString;
        }

        public override Type VisitCharLiteralExpr(MonkeyParser.CharLiteralExprContext context)
        {
            var raw = context.charLiteral().CHAR_LITERAL().GetText();
            char c = DecodeChar(raw);
            _il.Emit(OpCodes.Ldc_I4, (int)c);
            return ClrChar;
        }

        public override Type VisitIdentifierExpr(MonkeyParser.IdentifierExprContext context)
        {
            var name = context.identifier().GetText();
            var vi = ResolveVariable(name);
            if (vi == null)
                throw new InvalidOperationException($"Identifier '{name}' not found in encoder.");

            EmitLoadVariable(vi);
            return vi.Type;
        }

        public override Type VisitTrueLiteralExpr(MonkeyParser.TrueLiteralExprContext context)
        {
            _il.Emit(OpCodes.Ldc_I4_1);
            return ClrBool;
        }

        public override Type VisitFalseLiteralExpr(MonkeyParser.FalseLiteralExprContext context)
        {
            _il.Emit(OpCodes.Ldc_I4_0);
            return ClrBool;
        }

        public override Type VisitGroupedExpr(MonkeyParser.GroupedExprContext context)
        {
            return Visit(context.expression());
        }

        public override Type VisitArrayLiteralExpr(MonkeyParser.ArrayLiteralExprContext context)
        {
            var arrCtx = context.arrayLiteral();
            if (arrCtx.expressionList() == null ||
                arrCtx.expressionList().expression().Length == 0)
            {
                // array vacío: el tipo exacto lo determina el contexto.
                // Por simplicidad se usa int[] vacío.
                _il.Emit(OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Newarr, ClrInt);
                return ClrInt.MakeArrayType();
            }

            var exprs = arrCtx.expressionList().expression();
            // Se asume que todos son del mismo tipo y tomamos el primero
            var elementType = Visit(exprs[0]); // esto deja el primer elemento en la pila, que no se quiere aún

            // Como ya evaluamos el primero, se saca de la pila para no romper la pila
            _il.Emit(OpCodes.Pop);

            // newarr
            _il.Emit(OpCodes.Ldc_I4, exprs.Length);
            _il.Emit(OpCodes.Newarr, elementType);

            for (int i = 0; i < exprs.Length; i++)
            {
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Ldc_I4, i);
                var t = Visit(exprs[i]);
                EmitConvert(t, elementType);
                _il.Emit(OpCodes.Stelem, elementType);
            }

            return elementType.MakeArrayType();
        }

        public override Type VisitFunctionLiteralExpr(MonkeyParser.FunctionLiteralExprContext context)
        {
            // Funciones anónimas sin cierre: se genera un método estático oculto.
            var funcCtx = context.functionLiteral();

            var returnType = GetClrTypeFromTypeContext(funcCtx.type());
            var paramTypes = new List<Type>();
            var paramNames = new List<string>();

            if (funcCtx.functionParameters() != null)
            {
                foreach (var p in funcCtx.functionParameters().parameter())
                {
                    paramTypes.Add(GetClrTypeFromTypeContext(p.type()));
                    paramNames.Add(p.identifier().GetText());
                }
            }

            var delegateType = GetDelegateType(paramTypes.ToArray(), returnType);

            var lambdaName = "__lambda_" + Guid.NewGuid().ToString("N");
            var lambdaMethod = _programTypeBuilder.DefineMethod(
                lambdaName,
                MethodAttributes.Private | MethodAttributes.Static,
                returnType,
                paramTypes.ToArray()
            );

            // Guardar contexto actual
            var prevMethod = _currentMethod;
            var prevIL = _il;
            var prevReturn = _currentReturnType;
            var prevScopes = new Stack<Dictionary<string, VariableInfo>>(_scopes.Reverse());

            // Generar cuerpo del lambda
            _currentMethod = lambdaMethod;
            _il = lambdaMethod.GetILGenerator();
            _currentReturnType = returnType;
            _scopes.Clear();
            EnterScope();

            // Parámetros
            for (int i = 0; i < paramNames.Count; i++)
            {
                var vi = new VariableInfo
                {
                    IsArgument = true,
                    ArgIndex = i,
                    Local = null,
                    GlobalField = null,
                    Type = paramTypes[i],
                    IsConst = false
                };
                CurrentScope()[paramNames[i]] = vi;
            }

            Visit(funcCtx.blockStatement());

            if (returnType == typeof(void))
            {
                _il.Emit(OpCodes.Ret);
            }

            ExitScope();

            // Restaurar contexto
            _currentMethod = prevMethod;
            _il = prevIL;
            _currentReturnType = prevReturn;
            _scopes.Clear();
            foreach (var sc in prevScopes.Reverse())
                _scopes.Push(sc);

            // En el punto de uso, se deja en pila un delegate hacia lambdaMethod
            var ctor = delegateType.GetConstructor(new[] { typeof(object), typeof(IntPtr) });
            _il.Emit(OpCodes.Ldnull);
            _il.Emit(OpCodes.Ldftn, lambdaMethod);
            _il.Emit(OpCodes.Newobj, ctor);

            return delegateType;
        }

        public override Type VisitHashLiteralExpr(MonkeyParser.HashLiteralExprContext context)
        {
            var hashCtx = context.hashLiteral();
            if (hashCtx.hashContent() == null || hashCtx.hashContent().Length == 0)
            {
                // hash vacío: hash<int,int> por simplicidad. 
                var dictType = typeof(Dictionary<int, int>);
                _il.Emit(OpCodes.Newobj, dictType.GetConstructor(Type.EmptyTypes));
                return dictType;
            }

            var firstKeyType = Visit(hashCtx.hashContent(0).expression(0));
            _il.Emit(OpCodes.Pop); // No queremos el valor aún
            var firstValueType = Visit(hashCtx.hashContent(0).expression(1));
            _il.Emit(OpCodes.Pop);

            var dictGeneric = typeof(Dictionary<,>).MakeGenericType(firstKeyType, firstValueType);

            var ctor = dictGeneric.GetConstructor(Type.EmptyTypes);
            var addMethod = dictGeneric.GetMethod("Add", new[] { firstKeyType, firstValueType });

            _il.Emit(OpCodes.Newobj, ctor);

            foreach (var kv in hashCtx.hashContent())
            {
                _il.Emit(OpCodes.Dup);
                var kt = Visit(kv.expression(0));
                EmitConvert(kt, firstKeyType);
                var vt = Visit(kv.expression(1));
                EmitConvert(vt, firstValueType);
                _il.Emit(OpCodes.Callvirt, addMethod);
            }

            return dictGeneric;
        }

        // ============================================================
        // Auxiliares para decodificar literales string/char
        // ============================================================

        private string DecodeString(string raw)
        {
            // raw incluye comillas, por ejemplo: "hola\n"
            if (raw.Length >= 2 && raw[0] == '"' && raw[raw.Length - 1] == '"')
            {
                raw = raw.Substring(1, raw.Length - 2);
            }

            // Procesar escapes simples
            return raw
                .Replace("\\n", "\n")
                .Replace("\\t", "\t")
                .Replace("\\r", "\r")
                .Replace("\\\"", "\"")
                .Replace("\\'", "'")
                .Replace("\\\\", "\\");
        }

        private char DecodeChar(string raw)
        {
            // raw: 'a', '\n', etc.
            if (raw.Length >= 2 && raw[0] == '\'' && raw[raw.Length - 1] == '\'')
            {
                string inner = raw.Substring(1, raw.Length - 2);

                if (inner.Length == 1)
                    return inner[0];

                // Escapes
                switch (inner)
                {
                    case "\\n": return '\n';
                    case "\\t": return '\t';
                    case "\\r": return '\r';
                    case "\\\"": return '\"';
                    case "\\'": return '\'';
                    case "\\\\": return '\\';
                    default: return inner[0];
                }
            }

            return '?';
        }
    }
}
