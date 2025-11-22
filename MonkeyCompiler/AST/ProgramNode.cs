// Ast/ProgramNode.cs
using System.Collections.Generic;

namespace MonkeyCompiler.Ast
{
    public sealed class ProgramNode : AstNode
    {
        public IReadOnlyList<FunctionDeclaration> Functions { get; }
        public BlockStatement MainBody { get; }

        public ProgramNode(
            IReadOnlyList<FunctionDeclaration> functions,
            BlockStatement mainBody,
            int line,
            int column)
            : base(line, column)
        {
            Functions = functions;
            MainBody = mainBody;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitProgram(this);
    }
}