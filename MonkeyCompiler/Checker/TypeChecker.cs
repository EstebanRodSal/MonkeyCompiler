using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Generated;

namespace MonkeyCompiler.Checker;

public class TypeChecker: MonkeyBaseVisitor<object>
{
    private readonly SymbolTable _symbolTable;
    private readonly List<string> _errors;
    private MonkeyType? _currentFunctionReturnType;
    private bool _isInFunction;
    private bool _isInArrayLiteral = false;
    private bool _isInGlobalScope = true; // Rastrea si está en scope global
    
    public TypeChecker()
    {
        _symbolTable = new SymbolTable();
        _errors = new List<string>();
        _currentFunctionReturnType = null;
        _isInFunction = false;
    }
    
    private void VisitFunctionBody(MonkeyParser.FunctionDeclarationContext context)
    {
        var returnType = (MonkeyType)Visit(context.type());
        var paramTypes = new List<MonkeyType>();

        if (context.functionParameters() != null)
        {
            foreach (var param in context.functionParameters().parameter())
            {
                var paramType = (MonkeyType)Visit(param.type());
                paramTypes.Add(paramType);
            }
        }

        _symbolTable.EnterScope();
        _isInFunction = true;
        _isInGlobalScope = false; // Sale del scope global
        _currentFunctionReturnType = returnType;

        if (context.functionParameters() != null)
        {
            var parameters = context.functionParameters().parameter();
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramName = parameters[i].identifier().GetText();
                if (!_symbolTable.DeclareSymbol(paramName, paramTypes[i]))
                {
                    ReportError($"Parameter '{paramName}' is already declared", 
                        parameters[i].Start.Line);
                }
            }
        }

        Visit(context.blockStatement());

        _currentFunctionReturnType = null;
        _isInFunction = false;
        _isInGlobalScope = true; // Vuelve al scope global
        _symbolTable.ExitScope();
    }
    
    
    
    // =====================Reporte de errores =======================
    public List<string> GetErrors() => _errors;
    public bool HasErrors() => _errors.Count > 0;

    private void ReportError(string message, int line = 0)
    {
        _errors.Add($"Line {line}: {message}");
    }
    // ================================================================
    
    
    
    // =====================Visists =======================

    public override object VisitProgram(MonkeyParser.ProgramContext context)
    {
        // Declarar solo las FIRMAS de las funciones
        foreach (var funcDecl in context.functionDeclaration())
        {
            var funcName = funcDecl.identifier().GetText();
            var returnType = (MonkeyType)Visit(funcDecl.type());
            var paramTypes = new List<MonkeyType>();

            if (funcDecl.functionParameters() != null)
            {
                foreach (var param in funcDecl.functionParameters().parameter())
                {
                    var paramType = (MonkeyType)Visit(param.type());
                    paramTypes.Add(paramType);
                }
            }

            var functionType = new FunctionType(paramTypes, returnType);
        
            if (!_symbolTable.DeclareSymbol(funcName, functionType, false, true))
            {
                ReportError($"Function '{funcName}' is already declared in this scope", 
                    funcDecl.Start.Line);
            }
        }

        // Visitar statements globales
        foreach (var stmt in context.statement())
        {
            Visit(stmt);
        }

        // Procesar los CUERPOS de las funciones
        foreach (var funcDecl in context.functionDeclaration())
        {
            VisitFunctionBody(funcDecl);
        }

        // Visitar main
        Visit(context.mainFunction());
    
        return null;
    }

    public override object VisitMainFunction(MonkeyParser.MainFunctionContext context)
    {
        _symbolTable.EnterScope();
        _isInFunction = true;
        _isInGlobalScope = false;
        _currentFunctionReturnType = new VoidType();

        Visit(context.blockStatement());

        _currentFunctionReturnType = null;
        _isInFunction = false;
        _isInGlobalScope = true;
        _symbolTable.ExitScope();
        
        return new VoidType();
    }

    public override object VisitFunctionDeclaration(MonkeyParser.FunctionDeclarationContext context)
    {
        var returnType = (MonkeyType)Visit(context.type());
        var paramTypes = new List<MonkeyType>();

        if (context.functionParameters() != null)
        {
            foreach (var param in context.functionParameters().parameter())
            {
                var paramType = (MonkeyType)Visit(param.type());
                paramTypes.Add(paramType);
            }
        }

        var functionType = new FunctionType(paramTypes, returnType);

        _symbolTable.EnterScope();
        _isInFunction = true;
        _isInGlobalScope = false;
        _currentFunctionReturnType = returnType;

        if (context.functionParameters() != null)
        {
            var parameters = context.functionParameters().parameter();
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramName = parameters[i].identifier().GetText();
                if (!_symbolTable.DeclareSymbol(paramName, paramTypes[i]))
                {
                    ReportError($"Parameter '{paramName}' is already declared", 
                        parameters[i].Start.Line);
                }
            }
        }

        Visit(context.blockStatement());

        _currentFunctionReturnType = null;
        _isInFunction = false;
        _isInGlobalScope = true;
        _symbolTable.ExitScope();

        return functionType;
    }

    public override object VisitFunctionParameters(MonkeyParser.FunctionParametersContext context)
    {
        return base.VisitFunctionParameters(context);
    }

    public override object VisitParameter(MonkeyParser.ParameterContext context)
    {
        return base.VisitParameter(context);
    }

    public override object VisitType(MonkeyParser.TypeContext context)
    {
        if (context.INT_TYPE() != null) return new IntType();
        if (context.STRING_TYPE() != null) return new StringType();
        if (context.BOOL_TYPE() != null) return new BoolType();
        if (context.CHAR_TYPE() != null) return new CharType();
        if (context.VOID_TYPE() != null) return new VoidType();
        if (context.arrayType() != null) return Visit(context.arrayType());
        if (context.hashType() != null) return Visit(context.hashType());
        if (context.functionType() != null) return Visit(context.functionType());
        
        return new VoidType();
    }

    public override object VisitArrayType(MonkeyParser.ArrayTypeContext context)
    {
        var elementType = (MonkeyType)Visit(context.type());
        return new ArrayType(elementType);
    }

    public override object VisitHashType(MonkeyParser.HashTypeContext context)
    {
        var keyType = (MonkeyType)Visit(context.type(0));
        var valueType = (MonkeyType)Visit(context.type(1));
        
        if (!(keyType is IntType || keyType is StringType))
        {
            ReportError("Hash key type must be int or string", context.Start.Line);
        }
        
        return new HashType(keyType, valueType);
    }

    public override object VisitFunctionType(MonkeyParser.FunctionTypeContext context)
    {
        var paramTypes = new List<MonkeyType>();
        
        if (context.functionParameterTypes() != null)
        {
            foreach (var typeCtx in context.functionParameterTypes().type())
            {
                paramTypes.Add((MonkeyType)Visit(typeCtx));
            }
        }
        
        var returnType = (MonkeyType)Visit(context.type());
        return new FunctionType(paramTypes, returnType);
    }

    public override object VisitFunctionParameterTypes(MonkeyParser.FunctionParameterTypesContext context)
    {
        return base.VisitFunctionParameterTypes(context);
    }

    public override object VisitLetStmt(MonkeyParser.LetStmtContext context)
    {
        return base.VisitLetStmt(context);
    }

    public override object VisitReturnStmt(MonkeyParser.ReturnStmtContext context)
    {
        return base.VisitReturnStmt(context);
    }

    public override object VisitExpressionStmt(MonkeyParser.ExpressionStmtContext context)
    {
        return base.VisitExpressionStmt(context);
    }

    public override object VisitIfStmt(MonkeyParser.IfStmtContext context)
    {
        return base.VisitIfStmt(context);
    }

    public override object VisitBlockStmt(MonkeyParser.BlockStmtContext context)
    {
        return base.VisitBlockStmt(context);
    }

    public override object VisitPrintStmt(MonkeyParser.PrintStmtContext context)
    {
        return base.VisitPrintStmt(context);
    }

    public override object VisitLetVarStmt(MonkeyParser.LetVarStmtContext context)
    {
        var varName = context.identifier().GetText();
        var declaredType = (MonkeyType)Visit(context.type());
        var exprType = (MonkeyType)Visit(context.expression());

        if (!declaredType.IsCompatibleWith(exprType))
        {
            ReportError($"Type mismatch in declaration of '{varName}'. Expected {declaredType}, got {exprType}", 
                context.Start.Line);
        }

        if (!_symbolTable.DeclareSymbol(varName, declaredType, false, false))
        {
            ReportError($"Variable '{varName}' is already declared in this scope", 
                context.Start.Line);
        }

        return null;
    }

    public override object VisitConstVarStmt(MonkeyParser.ConstVarStmtContext context)
    {
        var varName = context.identifier().GetText();
        var declaredType = (MonkeyType)Visit(context.type());
        var exprType = (MonkeyType)Visit(context.expression());

        if (!declaredType.IsCompatibleWith(exprType))
        {
            ReportError($"Type mismatch in const declaration of '{varName}'. Expected {declaredType}, got {exprType}", 
                context.Start.Line);
        }

        if (!_symbolTable.DeclareSymbol(varName, declaredType, true, false))
        {
            ReportError($"Constant '{varName}' is already declared in this scope", 
                context.Start.Line);
        }

        return null;
    }

    public override object VisitReturnWithValue(MonkeyParser.ReturnWithValueContext context)
    {
        if (!_isInFunction)
        {
            ReportError("Return statement outside of function", context.Start.Line);
            return null;
        }

        var exprType = (MonkeyType)Visit(context.expression());

        if (_currentFunctionReturnType != null && !_currentFunctionReturnType.IsCompatibleWith(exprType))
        {
            ReportError($"Return type mismatch. Expected {_currentFunctionReturnType}, got {exprType}", 
                context.Start.Line);
        }

        return null;
    }

    public override object VisitReturnWithoutValue(MonkeyParser.ReturnWithoutValueContext context)
    {
        if (!_isInFunction)
        {
            ReportError("Return statement outside of function", context.Start.Line);
            return null;
        }

        if (_currentFunctionReturnType != null && !(_currentFunctionReturnType is VoidType))
        {
            ReportError($"Function must return a value of type {_currentFunctionReturnType}", 
                context.Start.Line);
        }

        return null;
    }

    public override object VisitExpressionStatement(MonkeyParser.ExpressionStatementContext context)
    {
        Visit(context.expression());
        return null;
    }

    public override object VisitIfOnly(MonkeyParser.IfOnlyContext context)
    {
        var condType = (MonkeyType)Visit(context.expression());
        
        if (!(condType is BoolType))
        {
            ReportError($"If condition must be of type bool, got {condType}", 
                context.Start.Line);
        }

        _symbolTable.EnterScope();
        Visit(context.blockStatement());
        _symbolTable.ExitScope();

        return null;
    }

    public override object VisitIfElse(MonkeyParser.IfElseContext context)
    {
        var condType = (MonkeyType)Visit(context.expression());
        
        if (!(condType is BoolType))
        {
            ReportError($"If condition must be of type bool, got {condType}", 
                context.Start.Line);
        }

        _symbolTable.EnterScope();
        Visit(context.blockStatement(0));
        _symbolTable.ExitScope();

        _symbolTable.EnterScope();
        Visit(context.blockStatement(1));
        _symbolTable.ExitScope();

        return null;
    }

    public override object VisitBlockStatement(MonkeyParser.BlockStatementContext context)
    {
        foreach (var stmt in context.statement())
        {
            Visit(stmt);
        }
        return null;
    }

    public override object VisitPrintStatement(MonkeyParser.PrintStatementContext context)
    {
        Visit(context.expression());
        return null;
    }

    public override object VisitExpression(MonkeyParser.ExpressionContext context)
    {
        var leftType = (MonkeyType)Visit(context.additionExpression(0));

        for (int i = 0; i < context.relationalOp().Length; i++)
        {
            var rightType = (MonkeyType)Visit(context.additionExpression(i + 1));
            var op = context.relationalOp(i).GetText();

            if (!leftType.IsCompatibleWith(rightType))
            {
                ReportError($"Type mismatch in relational operation '{op}'. Cannot compare {leftType} with {rightType}", 
                    context.Start.Line);
            }

            leftType = new BoolType();
        }

        return leftType;
    }

    public override object VisitRelationalOp(MonkeyParser.RelationalOpContext context)
    {
        return base.VisitRelationalOp(context);
    }

    public override object VisitAdditionExpression(MonkeyParser.AdditionExpressionContext context)
    {
        var leftType = (MonkeyType)Visit(context.multiplicationExpression(0));

        for (int i = 0; i < context.multiplicationExpression().Length - 1; i++)
        {
            var rightType = (MonkeyType)Visit(context.multiplicationExpression(i + 1));
            var op = context.GetChild(2 * i + 1).GetText();

            if (op == "+")
            {
                if (!((leftType is IntType && rightType is IntType) || 
                      (leftType is StringType && rightType is StringType)))
                {
                    ReportError($"Operator '+' requires both operands to be int or both to be string. Got {leftType} and {rightType}", 
                        context.Start.Line);
                }
            }
            else if (op == "-")
            {
                if (!(leftType is IntType && rightType is IntType))
                {
                    ReportError($"Operator '-' requires both operands to be int. Got {leftType} and {rightType}", 
                        context.Start.Line);
                }
            }

            leftType = (leftType is StringType && op == "+") ? new StringType() : new IntType();
        }

        return leftType;
    }

    public override object VisitMultiplicationExpression(MonkeyParser.MultiplicationExpressionContext context)
    {
        var leftType = (MonkeyType)Visit(context.elementExpression(0));

        for (int i = 0; i < context.elementExpression().Length - 1; i++)
        {
            var rightType = (MonkeyType)Visit(context.elementExpression(i + 1));

            if (!(leftType is IntType && rightType is IntType))
            {
                ReportError($"Multiplication/division requires both operands to be int. Got {leftType} and {rightType}", 
                    context.Start.Line);
            }

            leftType = new IntType();
        }

        return leftType;
    }

    public override object VisitElementExpression(MonkeyParser.ElementExpressionContext context)
    {
        var baseType = (MonkeyType)Visit(context.primitiveExpression());

        if (context.elementAccess() != null)
        {
            var indexType = (MonkeyType)Visit(context.elementAccess().expression());

            if (baseType is ArrayType arrayType)
            {
                if (!(indexType is IntType))
                {
                    ReportError($"Array index must be int, got {indexType}", 
                               context.Start.Line);
                }
                return arrayType.ElementType;
            }
            else if (baseType is HashType hashType)
            {
                if (!hashType.KeyType.IsCompatibleWith(indexType))
                {
                    ReportError($"Hash key type mismatch. Expected {hashType.KeyType}, got {indexType}", 
                               context.Start.Line);
                }
                
                return hashType.ValueType;
            }
            else
            {
                ReportError($"Cannot index type {baseType}", context.Start.Line);
                return new VoidType();
            }
        }

        if (context.callExpression() != null)
        {
            if (baseType is not FunctionType funcType)
            {
                ReportError($"Cannot call non-function type {baseType}", 
                           context.Start.Line);
                return new VoidType();
            }

            var argTypes = new List<MonkeyType>();
            if (context.callExpression().expressionList() != null)
            {
                foreach (var expr in context.callExpression().expressionList().expression())
                {
                    argTypes.Add((MonkeyType)Visit(expr));
                }
            }

            if (argTypes.Count != funcType.ParameterTypes.Count)
            {
                ReportError($"Function expects {funcType.ParameterTypes.Count} arguments, got {argTypes.Count}", 
                           context.Start.Line);
            }
            else
            {
                for (int i = 0; i < argTypes.Count; i++)
                {
                    if (!funcType.ParameterTypes[i].IsCompatibleWith(argTypes[i]))
                    {
                        ReportError($"Argument {i + 1} type mismatch. Expected {funcType.ParameterTypes[i]}, got {argTypes[i]}", 
                                   context.Start.Line);
                    }
                }
            }

            return funcType.ReturnType;
        }

        return baseType;
    }

    public override object VisitElementAccess(MonkeyParser.ElementAccessContext context)
    {
        return base.VisitElementAccess(context);
    }

    public override object VisitCallExpression(MonkeyParser.CallExpressionContext context)
    {
        return base.VisitCallExpression(context);
    }

    public override object VisitIntLiteralExpr(MonkeyParser.IntLiteralExprContext context)
    {
        return new IntType();
    }

    public override object VisitStringLiteralExpr(MonkeyParser.StringLiteralExprContext context)
    {
        return new StringType();
    }

    public override object VisitCharLiteralExpr(MonkeyParser.CharLiteralExprContext context)
    {
        return new CharType();
    }

    public override object VisitIdentifierExpr(MonkeyParser.IdentifierExprContext context)
    {
        var name = context.identifier().GetText();
        var symbol = _symbolTable.LookupSymbol(name);

        if (symbol == null)
        {
            ReportError($"Undeclared identifier '{name}'", context.Start.Line);
            return new VoidType();
        }

        return symbol.Type;
    }

    public override object VisitTrueLiteralExpr(MonkeyParser.TrueLiteralExprContext context)
    {
        return new BoolType();
    }

    public override object VisitFalseLiteralExpr(MonkeyParser.FalseLiteralExprContext context)
    {
        return new BoolType();
    }

    public override object VisitGroupedExpr(MonkeyParser.GroupedExprContext context)
    {
        return Visit(context.expression());
    }

    public override object VisitArrayLiteralExpr(MonkeyParser.ArrayLiteralExprContext context)
    {
        return Visit(context.arrayLiteral());
    }

    public override object VisitFunctionLiteralExpr(MonkeyParser.FunctionLiteralExprContext context)
    {
        return Visit(context.functionLiteral());
    }

    public override object VisitHashLiteralExpr(MonkeyParser.HashLiteralExprContext context)
    {
        return Visit(context.hashLiteral());
    }

    public override object VisitArrayLiteral(MonkeyParser.ArrayLiteralContext context)
    {
        _isInArrayLiteral = true;
    
        if (context.expressionList() == null || context.expressionList().expression().Length == 0)
        {
            _isInArrayLiteral = false;
            return new ArrayType(new IntType());
        }

        var firstType = (MonkeyType)Visit(context.expressionList().expression(0));

        foreach (var expr in context.expressionList().expression().Skip(1))
        {
            var exprType = (MonkeyType)Visit(expr);
            if (!firstType.IsCompatibleWith(exprType))
            {
                ReportError($"Array elements must have the same type. Expected {firstType}, got {exprType}", 
                    context.Start.Line);
            }
        }

        _isInArrayLiteral = false;
        return new ArrayType(firstType);
    }

    public override object VisitFunctionLiteral(MonkeyParser.FunctionLiteralContext context)
    {
        var returnType = (MonkeyType)Visit(context.type());
        var paramTypes = new List<MonkeyType>();

        if (context.functionParameters() != null)
        {
            foreach (var param in context.functionParameters().parameter())
            {
                paramTypes.Add((MonkeyType)Visit(param.type()));
            }
        }

        var funcType = new FunctionType(paramTypes, returnType);

        _symbolTable.EnterScope();
        var prevReturnType = _currentFunctionReturnType;
        var prevInFunction = _isInFunction;
        var prevInGlobalScope = _isInGlobalScope;
        
        _currentFunctionReturnType = returnType;
        _isInFunction = true;
        _isInGlobalScope = false;

        if (context.functionParameters() != null)
        {
            var parameters = context.functionParameters().parameter();
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramName = parameters[i].identifier().GetText();
                _symbolTable.DeclareSymbol(paramName, paramTypes[i]);
            }
        }

        Visit(context.blockStatement());

        _currentFunctionReturnType = prevReturnType;
        _isInFunction = prevInFunction;
        _isInGlobalScope = prevInGlobalScope;
        _symbolTable.ExitScope();

        return funcType;
    }

    public override object VisitHashLiteral(MonkeyParser.HashLiteralContext context)
    {
        if (context.hashContent() == null || context.hashContent().Length == 0)
        {
            return new HashType(new IntType(), new IntType());
        }

        var firstKeyType = (MonkeyType)Visit(context.hashContent(0).expression(0));
        var firstValueType = (MonkeyType)Visit(context.hashContent(0).expression(1));

        if (!(firstKeyType is IntType || firstKeyType is StringType))
        {
            ReportError("Hash keys must be int or string", context.Start.Line);
        }

        foreach (var hashContent in context.hashContent().Skip(1))
        {
            var keyType = (MonkeyType)Visit(hashContent.expression(0));
            var valueType = (MonkeyType)Visit(hashContent.expression(1));

            if (!firstKeyType.IsCompatibleWith(keyType))
            {
                ReportError($"Hash key type mismatch. Expected {firstKeyType}, got {keyType}", 
                    context.Start.Line);
            }

            if (!firstValueType.IsCompatibleWith(valueType))
            {
                ReportError($"Hash value type mismatch. Expected {firstValueType}, got {valueType}", 
                    context.Start.Line);
            }
        }

        return new HashType(firstKeyType, firstValueType);
    }

    public override object VisitHashContent(MonkeyParser.HashContentContext context)
    {
        return base.VisitHashContent(context);
    }

    public override object VisitExpressionList(MonkeyParser.ExpressionListContext context)
    {
        return base.VisitExpressionList(context);
    }

    public override object VisitIntegerLiteral(MonkeyParser.IntegerLiteralContext context)
    {
        return base.VisitIntegerLiteral(context);
    }

    public override object VisitStringLiteral(MonkeyParser.StringLiteralContext context)
    {
        return base.VisitStringLiteral(context);
    }

    public override object VisitCharLiteral(MonkeyParser.CharLiteralContext context)
    {
        return base.VisitCharLiteral(context);
    }

    public override object VisitIdentifier(MonkeyParser.IdentifierContext context)
    {
        return base.VisitIdentifier(context);
    }

    public override object Visit(IParseTree tree)
    {
        return base.Visit(tree);
    }
}