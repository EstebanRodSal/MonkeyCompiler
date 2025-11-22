using Antlr4.Runtime;

namespace MonkeyCompiler.Ast
{
    
    public abstract class Expression : AstNode
    {
        protected Expression(int line, int column) : base(line, column) { }
    }
    public abstract class Statement : AstNode
    {
        protected Statement(int line, int column) : base(line, column) { }
    }

    public sealed class LetStatement : Statement
    {
        public IToken Identifier { get; }
        public TypeNode Type { get; }
        public Expression Value { get; }

        public LetStatement(IToken identifier, TypeNode type, Expression value)
            : base(identifier.Line, identifier.Column)
        {
            Identifier = identifier;
            Type = type;
            Value = value;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitLetStatement(this);
    }

    public sealed class ReturnStatement : Statement
    {
        public Expression? Expression { get; }

        public ReturnStatement(Expression? expr, int line, int column)
            : base(line, column)
        {
            Expression = expr;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitReturnStatement(this);
    }

}