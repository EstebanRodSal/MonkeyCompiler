// Ast/FunctionDeclaration.cs
using System.Collections.Generic;
using Antlr4.Runtime;

namespace MonkeyCompiler.Ast
{
    public sealed class ParameterNode : AstNode
    {
        public IToken Identifier { get; }
        public TypeNode Type { get; }

        public ParameterNode(IToken identifier, TypeNode type)
            : base(identifier.Line, identifier.Column)
        {
            Identifier = identifier;
            Type = type;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitParameter(this);
    }
    public sealed class FunctionDeclaration : AstNode
    {
        public IToken NameToken { get; }
        public IReadOnlyList<ParameterNode> Parameters { get; }
        public TypeNode ReturnType { get; }
        public BlockStatement Body { get; }

        public FunctionDeclaration(
            IToken nameToken,
            IReadOnlyList<ParameterNode> parameters,
            TypeNode returnType,
            BlockStatement body)
            : base(nameToken.Line, nameToken.Column)
        {
            NameToken = nameToken;
            Parameters = parameters;
            ReturnType = returnType;
            Body = body;
        }

        public override T Accept<T>(MonkeyAstBaseVisitor<T> v)
            => v.VisitFunctionDeclaration(this);
    }
}