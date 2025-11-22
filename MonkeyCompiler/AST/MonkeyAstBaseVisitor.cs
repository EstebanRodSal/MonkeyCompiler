// Ast/MonkeyAstBaseVisitor.cs
namespace MonkeyCompiler.Ast
{
    public abstract class MonkeyAstBaseVisitor<T>
    {
        public virtual T DefaultResult() => default!;

        public virtual T Visit(AstNode node) => node.Accept(this);
        public virtual T VisitExpressionStatement(ExpressionStatement n) => DefaultResult();
        public virtual T VisitPrintStatement(PrintStatement n)         => DefaultResult();

        public virtual T VisitBooleanLiteral(BooleanLiteral n) => DefaultResult();
        public virtual T VisitStringLiteral(StringLiteral n) => DefaultResult();
        public virtual T VisitCharLiteral(CharLiteral n) => DefaultResult();
        public virtual T VisitArrayLiteral(ArrayLiteral n) => DefaultResult();
        public virtual T VisitElementAccess(ElementAccess n) => DefaultResult();
        public virtual T VisitCallExpression(CallExpression n) => DefaultResult();
        public virtual T VisitHashLiteral(HashLiteral n) => DefaultResult();
        public virtual T VisitFunctionLiteral(FunctionLiteral n) => DefaultResult();

        // Un método por cada tipo concreto de nodo que uses:

        public virtual T VisitProgram(ProgramNode n)                => DefaultResult();
        public virtual T VisitFunctionDeclaration(FunctionDeclaration n) => DefaultResult();

        // 👇 ESTE es el que te falta para que BlockStatement compile
        public virtual T VisitBlockStatement(BlockStatement n)      => DefaultResult();

        public virtual T VisitIfStatement(IfStatement n)            => DefaultResult();
        public virtual T VisitLetStatement(LetStatement n)          => DefaultResult();
        public virtual T VisitReturnStatement(ReturnStatement n)    => DefaultResult();

        public virtual T VisitBinaryExpr(BinaryExpression n)        => DefaultResult();
        public virtual T VisitIntegerLiteral(IntegerLiteral n)      => DefaultResult();
        public virtual T VisitIdentifier(IdentifierExpr n)          => DefaultResult();
        public virtual T VisitParameter(ParameterNode n) => DefaultResult();

        // Si tienes más nodos (PrintStatement, ArrayLiteral, etc.), les agregas su Visit...
        // public virtual T VisitPrintStatement(PrintStatement n)   => DefaultResult();
        // public virtual T VisitArrayLiteral(ArrayLiteral n)       => DefaultResult();
    }
}