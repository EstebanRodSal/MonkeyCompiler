using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using MonkeyCompiler.Ast;

// Visitor que convierte el parse tree de ANTLR -> tu AST propio
public class ParseTreeToAstVisitor : MonkeyBaseVisitor<object>
{
    // program : (functionDeclaration | statement)* mainFunction EOF
    public override object VisitProgram([NotNull] MonkeyParser.ProgramContext ctx)
    {
        var functions = new List<FunctionDeclaration>();

        foreach (var f in ctx.functionDeclaration())
            functions.Add((FunctionDeclaration)Visit(f));

        var mainBlock = (BlockStatement)Visit(ctx.mainFunction().blockStatement());

        return new ProgramNode(
            functions,
            mainBlock,
            ctx.Start.Line,
            ctx.Start.Column);
    }

    // functionDeclaration : fn identifier ( functionParameters? ) : type blockStatement
    public override object VisitFunctionDeclaration([NotNull] MonkeyParser.FunctionDeclarationContext ctx)
    {
        // identifier es una regla: identifier : IDENTIFIER ;
        var nameToken = ctx.identifier().IDENTIFIER().Symbol; // IToken

        var parameters = new List<ParameterNode>();
        if (ctx.functionParameters() != null)
        {
            foreach (var p in ctx.functionParameters().parameter())
                parameters.Add((ParameterNode)Visit(p));
        }

        var returnType = (TypeNode)Visit(ctx.type());
        var body = (BlockStatement)Visit(ctx.blockStatement());

        return new FunctionDeclaration(nameToken, parameters, returnType, body);
    }

    // parameter : identifier : type
    public override object VisitParameter([NotNull] MonkeyParser.ParameterContext ctx)
    {
        var idToken = ctx.identifier().IDENTIFIER().Symbol; // IToken
        var type = (TypeNode)Visit(ctx.type());
        return new ParameterNode(idToken, type);
    }

    // type : int | string | bool | char | void | arrayType | hashType | functionType
    // De momento, lo tratamos como string del texto completo.
    public override object VisitType([NotNull] MonkeyParser.TypeContext ctx)
    {
        return new TypeNode(ctx.GetText());
    }

    // blockStatement : { statement* }
    public override object VisitBlockStatement([NotNull] MonkeyParser.BlockStatementContext ctx)
    {
        var stmts = new List<Statement>();
        foreach (var s in ctx.statement())
            stmts.Add((Statement)Visit(s));

        return new BlockStatement(
            stmts,
            ctx.Start.Line,
            ctx.Start.Column);
    }

    // letStatement : let const? identifier : type = expression
    public override object VisitLetStatement([NotNull] MonkeyParser.LetStatementContext ctx)
    {
        var idToken = ctx.identifier().IDENTIFIER().Symbol;
        var type = (TypeNode)Visit(ctx.type());
        var expr = (Expression)Visit(ctx.expression());

        return new LetStatement(idToken, type, expr);
    }

    // expressionStatement : expression
    public override object VisitExpressionStatement([NotNull] MonkeyParser.ExpressionStatementContext ctx)
    {
        var expr = (Expression)Visit(ctx.expression());
        return new ExpressionStatement(expr, ctx.Start.Line, ctx.Start.Column);
    }

    // returnStatement : return expression?
    public override object VisitReturnStatement([NotNull] MonkeyParser.ReturnStatementContext ctx)
    {
        Expression expr = null;
        if (ctx.expression() != null)
            expr = (Expression)Visit(ctx.expression());

        return new ReturnStatement(expr, ctx.Start.Line, ctx.Start.Column);
    }

    // ifStatement : if expression blockStatement (else blockStatement)?
    public override object VisitIfStatement([NotNull] MonkeyParser.IfStatementContext ctx)
    {
        var cond = (Expression)Visit(ctx.expression());
        var thenBlock = (BlockStatement)Visit(ctx.blockStatement(0));

        BlockStatement elseBlock = null;
        if (ctx.ELSE() != null)
            elseBlock = (BlockStatement)Visit(ctx.blockStatement(1));

        return new IfStatement(
            cond,
            thenBlock,
            elseBlock,
            ctx.Start.Line,
            ctx.Start.Column);
    }

    // printStatement : print ( expression )
    public override object VisitPrintStatement([NotNull] MonkeyParser.PrintStatementContext ctx)
    {
        var expr = (Expression)Visit(ctx.expression());
        return new PrintStatement(expr, ctx.Start.Line, ctx.Start.Column);
    }

    // ---------------- EXPRESIONES ----------------
    // expression : additionExpression (relationalOp additionExpression)*
    public override object VisitExpression(MonkeyParser.ExpressionContext ctx)
    {
        // expression : additionExpression (relationalOp additionExpression)*
        var expr = (Expression)Visit(ctx.additionExpression(0));
        var adds = ctx.additionExpression();
        var relOps = ctx.relationalOp();

        // Si no hay operadores relacionales, devolvemos la parte aritmética
        if (relOps.Length == 0)
            return expr;

        // Encadenamos: a < b < c -> Binary( '<', Binary('<', a, b), c )
        for (int i = 1; i < adds.Length; i++)
        {
            var right = (Expression)Visit(adds[i]);
            var op = relOps[i - 1].Start;   // token del operador
            var opText = op.Text;

            expr = new BinaryExpression(
                opText,
                expr,
                right,
                op.Line,
                op.Column);
        }

        return expr;
    }

// additionExpression : multiplicationExpression ((+ | -) multiplicationExpression)*
    public override object VisitAdditionExpression([NotNull] MonkeyParser.AdditionExpressionContext ctx)
    {
        var mults = ctx.multiplicationExpression();
        var expr = (Expression)Visit(mults[0]);

        // Si no hay + o -, devolvemos solo el primer término
        if (mults.Length == 1)
            return expr;

        // Hijos: term op term op term ...
        for (int i = 1; i < mults.Length; i++)
        {
            var right = (Expression)Visit(mults[i]);
            var opNode = ctx.GetChild(2 * i - 1); // el '+' o '-'
            var opText = opNode.GetText();

            expr = new BinaryExpression(
                opText,
                expr,
                right,
                ctx.Start.Line,
                ctx.Start.Column);
        }

        return expr;
    }

    public override object VisitMultiplicationExpression([NotNull] MonkeyParser.MultiplicationExpressionContext ctx)
    {
        // multiplicationExpression : elementExpression ((* | /) elementExpression)*
        var elems = ctx.elementExpression();
        var expr = (Expression)Visit(elems[0]);

        if (elems.Length == 1)
            return expr;

        for (int i = 1; i < elems.Length; i++)
        {
            var right = (Expression)Visit(elems[i]);
            var opNode = ctx.GetChild(2 * i - 1); // '*' o '/'
            var opText = opNode.GetText();

            expr = new BinaryExpression(
                opText,
                expr,
                right,
                opNode.SourceInterval.a,
                0);
        }

        return expr;
    }

    // multiplicationExpression : elementExpression ((* | /) elementExpression)*
    public override object VisitElementExpression([NotNull] MonkeyParser.ElementExpressionContext ctx)
    {
        // primitiveExpression (elementAccess | callExpression)?
        var expr = (Expression)Visit(ctx.primitiveExpression());

        if (ctx.elementAccess() != null)
        {
            var acc = ctx.elementAccess();
            var indexExpr = (Expression)Visit(acc.expression());
            return new ElementAccess(expr, indexExpr, acc.Start.Line, acc.Start.Column);
        }

        if (ctx.callExpression() != null)
        {
            var call = ctx.callExpression();
            var args = new List<Expression>();

            if (call.expressionList() != null)
            {
                foreach (var e in call.expressionList().expression())
                    args.Add((Expression)Visit(e));
            }

            return new CallExpression(expr, args, call.Start.Line, call.Start.Column);
        }

        return expr;
    }

    // primitiveExpression:
    //    integerLiteral | stringLiteral | charLiteral | identifier
    //  | true | false | ( expression ) | arrayLiteral | functionLiteral | hashLiteral
    public override object VisitPrimitiveExpression([NotNull] MonkeyParser.PrimitiveExpressionContext ctx)
    {
        // integerLiteral
        if (ctx.integerLiteral() != null)
        {
            var tok = ctx.integerLiteral().INTEGER_LITERAL().Symbol;
            var value = int.Parse(tok.Text);
            return new IntegerLiteral(value, tok.Line, tok.Column);
        }

        // stringLiteral
        if (ctx.stringLiteral() != null)
        {
            var tok = ctx.stringLiteral().STRING_LITERAL().Symbol;
            // quitar comillas externas
            var raw = tok.Text;
            var inner = raw.Length >= 2 ? raw.Substring(1, raw.Length - 2) : "";
            return new StringLiteral(inner, tok.Line, tok.Column);
        }

        // charLiteral
        if (ctx.charLiteral() != null)
        {
            var tok = ctx.charLiteral().CHAR_LITERAL().Symbol;
            var raw = tok.Text; // p.ej. 'a'
            char c = raw.Length >= 3 ? raw[1] : '\0';
            return new CharLiteral(c, tok.Line, tok.Column);
        }

        // identifier
        if (ctx.identifier() != null)
        {
            var tok = ctx.identifier().IDENTIFIER().Symbol;
            return new IdentifierExpr(tok.Text, tok.Line, tok.Column);
        }

        // true / false
        if (ctx.TRUE() != null || ctx.FALSE() != null)
        {
            var node = ctx.TRUE() ?? ctx.FALSE();
            var tok = node.Symbol;
            bool value = tok.Type == MonkeyLexer.TRUE;
            return new BooleanLiteral(value, tok.Line, tok.Column);
        }

        // ( expression )
        if (ctx.expression() != null)
        {
            return Visit(ctx.expression());
        }

        // arrayLiteral
        if (ctx.arrayLiteral() != null)
        {
            return Visit(ctx.arrayLiteral());
        }

        // functionLiteral
        if (ctx.functionLiteral() != null)
        {
            return Visit(ctx.functionLiteral());
        }

        // hashLiteral
        if (ctx.hashLiteral() != null)
        {
            return Visit(ctx.hashLiteral());
        }

        throw new NotImplementedException("PrimitiveExpression faltante para este caso.");
    }
    public override object VisitArrayLiteral([NotNull] MonkeyParser.ArrayLiteralContext ctx)
    {
        var elements = new List<Expression>();

        if (ctx.expressionList() != null)
        {
            foreach (var e in ctx.expressionList().expression())
                elements.Add((Expression)Visit(e));
        }

        return new ArrayLiteral(
            elements,
            ctx.Start.Line,
            ctx.Start.Column);
    }
    public override object VisitHashLiteral([NotNull] MonkeyParser.HashLiteralContext ctx)
    {
        var entries = new List<HashEntry>();

        foreach (var hc in ctx.hashContent())
        {
            // hashContent : expression ':' expression
            var key = (Expression)Visit(hc.expression(0));
            var value = (Expression)Visit(hc.expression(1));
            entries.Add(new HashEntry(key, value));
        }

        return new HashLiteral(entries, ctx.Start.Line, ctx.Start.Column);
    }
    public override object VisitFunctionLiteral([NotNull] MonkeyParser.FunctionLiteralContext ctx)
    {
        var parameters = new List<ParameterNode>();
        if (ctx.functionParameters() != null)
        {
            foreach (var p in ctx.functionParameters().parameter())
                parameters.Add((ParameterNode)Visit(p));
        }

        var returnType = (TypeNode)Visit(ctx.type());
        var body = (BlockStatement)Visit(ctx.blockStatement());

        return new FunctionLiteral(
            parameters,
            returnType,
            body,
            ctx.Start.Line,
            ctx.Start.Column);
    }


}
