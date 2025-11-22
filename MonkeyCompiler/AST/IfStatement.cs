// Ast/IfStatement.cs
namespace MonkeyCompiler.Ast
{
    public sealed class IfStatement : Statement
    {
        public Expression Condition { get; }
        public BlockStatement ThenBlock { get; }
        public BlockStatement? ElseBlock { get; }

        public IfStatement(
            Expression condition,
            BlockStatement thenBlock,
            BlockStatement? elseBlock,
            int line,
            int column) : base(line, column)
        {
            Condition = condition;
            ThenBlock = thenBlock;
            ElseBlock = elseBlock;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitIfStatement(this);
    }
}
