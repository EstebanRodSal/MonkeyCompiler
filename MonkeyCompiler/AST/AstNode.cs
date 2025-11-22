// Ast/AstNode.cs
namespace MonkeyCompiler.Ast
{
    public abstract class AstNode
    {
        public int Line { get; }
        public int Column { get; }

        protected AstNode(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public abstract T Accept<T>(MonkeyAstBaseVisitor<T> v);
    }
}