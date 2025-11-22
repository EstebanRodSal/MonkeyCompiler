// Ast/PrintStatement.cs
namespace MonkeyCompiler.Ast
{
    public sealed class PrintStatement : Statement
    {
        public Expression Expression { get; }

        public PrintStatement(Expression expression, int line, int column)
            : base(line, column)
        {
            Expression = expression;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitPrintStatement(this);
    }
}