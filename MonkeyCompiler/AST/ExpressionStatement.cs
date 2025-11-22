// Ast/ExpressionStatement.cs
namespace MonkeyCompiler.Ast
{
    public sealed class ExpressionStatement : Statement
    {
        public Expression Expression { get; }

        public ExpressionStatement(Expression expression, int line, int column)
            : base(line, column)
        {
            Expression = expression;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitExpressionStatement(this);
    }
}