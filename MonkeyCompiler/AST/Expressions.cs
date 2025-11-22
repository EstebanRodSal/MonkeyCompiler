using System.Collections.Generic;
using Antlr4.Runtime;

namespace MonkeyCompiler.Ast
{
    // ================== EXPRESIONES BÁSICAS ==================

    // Literal entero: 1, 2, 3...
    public sealed class IntegerLiteral : Expression
    {
        public int Value { get; }

        public IntegerLiteral(int value, int line, int column)
            : base(line, column)
        {
            Value = value;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitIntegerLiteral(this);
    }

    // Identificador: age, name, myArray, etc.
    public sealed class IdentifierExpr : Expression
    {
        public string Name { get; }

        public IdentifierExpr(string name, int line, int column)
            : base(line, column)
        {
            Name = name;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitIdentifier(this);
    }

    // Expresión binaria: a + b, x * y, a == b, etc.
    public sealed class BinaryExpression : Expression
    {
        public string Op { get; }
        public Expression Left { get; }
        public Expression Right { get; }

        public BinaryExpression(string op, Expression left, Expression right, int line, int column)
            : base(line, column)
        {
            Op = op;
            Left = left;
            Right = right;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitBinaryExpr(this);
    }

    // ================== LITERALES ==================

    // Literal booleano: true / false
    public sealed class BooleanLiteral : Expression
    {
        public bool Value { get; }

        public BooleanLiteral(bool value, int line, int column)
            : base(line, column)
        {
            Value = value;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitBooleanLiteral(this);
    }

    // Literal de cadena: "texto"
    public sealed class StringLiteral : Expression
    {
        public string Value { get; }

        public StringLiteral(string value, int line, int column)
            : base(line, column)
        {
            Value = value;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitStringLiteral(this);
    }

    // Literal de caracter: 'a'
    public sealed class CharLiteral : Expression
    {
        public char Value { get; }

        public CharLiteral(char value, int line, int column)
            : base(line, column)
        {
            Value = value;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitCharLiteral(this);
    }

    // ================== ARRAYS Y ACCESOS ==================

    // [1, 2, 3]
    public sealed class ArrayLiteral : Expression
    {
        public IReadOnlyList<Expression> Elements { get; }

        public ArrayLiteral(IReadOnlyList<Expression> elements, int line, int column)
            : base(line, column)
        {
            Elements = elements;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitArrayLiteral(this);
    }

    // Acceso: arr[0] o map["key"]
    public sealed class ElementAccess : Expression
    {
        public Expression Target { get; }
        public Expression Index { get; }

        public ElementAccess(Expression target, Expression index, int line, int column)
            : base(line, column)
        {
            Target = target;
            Index = index;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitElementAccess(this);
    }

    // ================== LLAMADAS Y FUNCIONES ==================

    // Llamada: f(5, 7)
    public sealed class CallExpression : Expression
    {
        public Expression Callee { get; }
        public IReadOnlyList<Expression> Arguments { get; }

        public CallExpression(Expression callee, IReadOnlyList<Expression> arguments, int line, int column)
            : base(line, column)
        {
            Callee = callee;
            Arguments = arguments;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitCallExpression(this);
    }

    // fn(a:int, b:int):int { ... } usado como expresión
    public sealed class FunctionLiteral : Expression
    {
        public IReadOnlyList<ParameterNode> Parameters { get; }
        public TypeNode ReturnType { get; }
        public BlockStatement Body { get; }

        public FunctionLiteral(
            IReadOnlyList<ParameterNode> parameters,
            TypeNode returnType,
            BlockStatement body,
            int line, int column)
            : base(line, column)
        {
            Parameters = parameters;
            ReturnType = returnType;
            Body = body;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitFunctionLiteral(this);
    }

    // ================== HASHES ==================

    // entrada de hash: key : value
    public sealed class HashEntry
    {
        public Expression Key { get; }
        public Expression Value { get; }

        public HashEntry(Expression key, Expression value)
        {
            Key = key;
            Value = value;
        }
    }

    // hash literal: { expr : expr, ... }
    public sealed class HashLiteral : Expression
    {
        public IReadOnlyList<HashEntry> Entries { get; }

        public HashLiteral(IReadOnlyList<HashEntry> entries, int line, int column)
            : base(line, column)
        {
            Entries = entries;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitHashLiteral(this);
    }
}
