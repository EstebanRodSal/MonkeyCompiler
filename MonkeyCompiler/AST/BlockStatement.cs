// Ast/BlockStatement.cs
using System.Collections.Generic;

namespace MonkeyCompiler.Ast
{
    public sealed class BlockStatement : Statement
    {
        public IReadOnlyList<Statement> Statements { get; }

        public BlockStatement(
            IReadOnlyList<Statement> statements,
            int line,
            int column) : base(line, column)
        {
            Statements = statements;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitBlockStatement(this);
    }
}