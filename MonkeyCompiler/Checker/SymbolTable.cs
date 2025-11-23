using System;
using System.Collections.Generic;

namespace MonkeyCompiler.Checker;

// Clase abstracta que representa los diferentes tipos de dato en el lenguaje Monkey
public abstract class MonkeyType
{
    public abstract string ToString();
    public abstract bool IsCompatibleWith(MonkeyType other);
}

public class IntType : MonkeyType
{
    public override string ToString() => "int"; //Devuelve el tipo de dato 
    
    public override bool IsCompatibleWith(MonkeyType other) => other is IntType; //Devolvería true solamente si other tambien corresponde a un IntType
}

public class StringType : MonkeyType
{
    public override string ToString() => "string";
    public override bool IsCompatibleWith(MonkeyType other) => other is StringType;
}

public class BoolType : MonkeyType
{
    public override string ToString() => "bool";
    public override bool IsCompatibleWith(MonkeyType other) => other is BoolType;
}

public class CharType : MonkeyType
{
    public override string ToString() => "char";
    public override bool IsCompatibleWith(MonkeyType other) => other is CharType;
}

public class VoidType : MonkeyType
{
    public override string ToString() => "void";
    public override bool IsCompatibleWith(MonkeyType other) => other is VoidType;
}

public class ArrayType : MonkeyType
{
    public MonkeyType ElementType { get; } //Guarda el tipo de datos del arreglo
    
    //Constructor para el arreglo
    public ArrayType(MonkeyType elementType)
    {
        ElementType = elementType; //Apenas se crea se le indica de una el tipo de dato que va a tener
    }
    
    public override string ToString() => $"array<{ElementType}>"; //Devuelve de que tipo de dato es el arreglo
    
    public override bool IsCompatibleWith(MonkeyType other)
    {
        //Hay que verificar que other tambien sea arreglo, pero ademas del mismo tipo de dato
        return other is ArrayType arr && ElementType.IsCompatibleWith(arr.ElementType);
    }
}

public class HashType : MonkeyType
{
    public MonkeyType KeyType { get; } //Obtiene el tipo de datos de la llave
    public MonkeyType ValueType { get; } //Obtiene el tipo de datos del valor
    
    
    //Apenas se crea se le asigna al hash el tipo de dato de la llave y del valor
    public HashType(MonkeyType keyType, MonkeyType valueType)
    {
        KeyType = keyType;
        ValueType = valueType;
    }
    
    public override string ToString() => $"hash<{KeyType}, {ValueType}>"; //Devuelve el tipo de dato de la llave y del valor
    
    public override bool IsCompatibleWith(MonkeyType other)
    {
        //Hay que verificar que other sea un hash && que tanto la llave como el valor coincidan tambien
        return other is HashType hash && 
               KeyType.IsCompatibleWith(hash.KeyType) && 
               ValueType.IsCompatibleWith(hash.ValueType);
    }
}

public class FunctionType : MonkeyType
{
    public List<MonkeyType> ParameterTypes { get; } //Los tipos de los parametros de la función
    public MonkeyType ReturnType { get; } //El tipo de los retornos de la funcion
    
    //Define los tipos de datos de la función y el tipo de dato del retorno una vez creada
    public FunctionType(List<MonkeyType> parameterTypes, MonkeyType returnType)
    {
        ParameterTypes = parameterTypes;
        ReturnType = returnType;
    }
    
    public override string ToString()
    {
        var paramStr = string.Join(", ", ParameterTypes);
        return $"fn({paramStr}): {ReturnType}";
    }
    
    public override bool IsCompatibleWith(MonkeyType other)
    {
        if (other is not FunctionType func) return false; //Compatible con funciones
        if (!ReturnType.IsCompatibleWith(func.ReturnType)) return false; //El tipo de retorno debe de ser compatible
        if (ParameterTypes.Count != func.ParameterTypes.Count) return false; //Misma cantidad de parametros
        
        //Cada tipo del parametro debe de ser compatible entre sí
        for (int i = 0; i < ParameterTypes.Count; i++)
        {
            if (!ParameterTypes[i].IsCompatibleWith(func.ParameterTypes[i]))
                return false;
        }
        return true;
    }
}

// Representa un símbolo en la tabla
public class Symbol
{
    public string Name { get; }
    public MonkeyType Type { get; }
    public bool IsConstant { get; }
    public bool IsFunction { get; }
    public int ScopeLevel { get; }
    
    public Symbol(string name, MonkeyType type, bool isConstant = false, 
                  bool isFunction = false, int scopeLevel = 0)
    {
        Name = name;
        Type = type;
        IsConstant = isConstant;
        IsFunction = isFunction;
        ScopeLevel = scopeLevel;
    }
}

// Tabla de símbolos
public class SymbolTable
{
    private class Scope
    {
        public Dictionary<string, Symbol> Symbols { get; } = new();
        public Scope? Parent { get; }
        public int Level { get; }
        
        public Scope(Scope? parent, int level)
        {
            Parent = parent;
            Level = level;
        }
    }
    
    private Scope _currentScope;
    private int _scopeLevel;
    
    public SymbolTable()
    {
        _scopeLevel = 0;
        _currentScope = new Scope(null, _scopeLevel);
        InitializeBuiltInFunctions();
    }
    
    // Inicializa las funciones predefinidas del lenguaje
    private void InitializeBuiltInFunctions()
    {
        // len(array<T>): int o len(string): int
        var lenType = new FunctionType(
            new List<MonkeyType> { new ArrayType(new IntType()) }, 
            new IntType()
        );
        _currentScope.Symbols["len"] = new Symbol("len", lenType, false, true, 0);
        
        // first(array<T>): T
        var firstType = new FunctionType(
            new List<MonkeyType> { new ArrayType(new IntType()) },
            new IntType() 
        );
        _currentScope.Symbols["first"] = new Symbol("first", firstType, false, true, 0);
        
        // last(array<T>): T
        var lastType = new FunctionType(
            new List<MonkeyType> { new ArrayType(new IntType()) },
            new IntType()
        );
        _currentScope.Symbols["last"] = new Symbol("last", lastType, false, true, 0);
        
        // rest(array<T>): array<T>
        var restType = new FunctionType(
            new List<MonkeyType> { new ArrayType(new IntType()) },
            new ArrayType(new IntType())
        );
        _currentScope.Symbols["rest"] = new Symbol("rest", restType, false, true, 0);
        
        // push(array<T>, T): array<T>
        var pushType = new FunctionType(
            new List<MonkeyType> { new ArrayType(new IntType()), new IntType() },
            new ArrayType(new IntType())
        );
        _currentScope.Symbols["push"] = new Symbol("push", pushType, false, true, 0);
    }
    
    // Entra a un nuevo scope
    public void EnterScope()
    {
        _scopeLevel++;
        _currentScope = new Scope(_currentScope, _scopeLevel);
    }
    
    // Sale del scope actual
    public void ExitScope()
    {
        if (_currentScope.Parent == null)
            throw new InvalidOperationException("Cannot exit global scope");
        
        _currentScope = _currentScope.Parent;
        _scopeLevel--;
    }
    
    // Declara un símbolo en el ámbito actual
    public bool DeclareSymbol(string name, MonkeyType type, bool isConstant = false, bool isFunction = false)
    {
        // Verifica si ya existe en el ámbito actual
        if (_currentScope.Symbols.ContainsKey(name))
            return false;
        
        _currentScope.Symbols[name] = new Symbol(name, type, isConstant, isFunction, _scopeLevel);
        return true;
    }
    
    // Busca un símbolo en el ámbito actual y ancestros
    public Symbol? LookupSymbol(string name)
    {
        var scope = _currentScope;
        while (scope != null)
        {
            if (scope.Symbols.TryGetValue(name, out var symbol))
                return symbol;
            scope = scope.Parent;
        }
        return null;
    }
    
    // Verifica si un símbolo existe en el ámbito actual (no busca en ancestros)
    public bool ExistsInCurrentScope(string name)
    {
        return _currentScope.Symbols.ContainsKey(name);
    }
    
    // Obtiene el nivel de ámbito actual
    public int GetCurrentLevel()
    {
        return _scopeLevel;
    }
}