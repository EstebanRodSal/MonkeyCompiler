using System;
using System.Collections.Generic;

namespace MonkeyCompiler.Ast
{
    public sealed class AstPrintVisitor : MonkeyAstBaseVisitor<string>
    {
        private readonly List<string> _lines = new();
        private int _indent = 0;
        private string Indent => new string(' ', _indent * 2);

        public string Print(ProgramNode root)
        {
            Visit(root);
            return string.Join(Environment.NewLine, _lines);
        }

        private void Line(string text) => _lines.Add(Indent + text);

        public override string VisitProgram(ProgramNode n)
        {
            Line("Program");
            _indent++;

            // Imprimir funciones declaradas
            foreach (var f in n.Functions)
                Visit(f);

            // Imprimir el bloque main si existe
            if (n.MainBody is not null)
            {
                Line("Main:");
                _indent++;
                Visit(n.MainBody);
                _indent--;
            }

            _indent--;
            return "";
        }
        public override string VisitBlockStatement(BlockStatement n)
        {
            Line("Block");
            _indent++;
            foreach (var stmt in n.Statements)
                Visit(stmt);
            _indent--;
            return "";
        }

        public override string VisitExpressionStatement(ExpressionStatement n)
        {
            Line("ExpressionStatement");
            _indent++;
            Visit(n.Expression);
            _indent--;
            return "";
        }

        public override string VisitReturnStatement(ReturnStatement n)
        {
            Line("Return");
            _indent++;
            if (n.Expression is not null)
                Visit(n.Expression);
            _indent--;
            return "";
        }

        public override string VisitIfStatement(IfStatement n)
        {
            Line("If");
            _indent++;

            Line("Condition:");
            _indent++;
            Visit(n.Condition);
            _indent--;

            Line("Then:");
            _indent++;
            Visit(n.ThenBlock);
            _indent--;

            if (n.ElseBlock is not null)
            {
                Line("Else:");
                _indent++;
                Visit(n.ElseBlock);
                _indent--;
            }

            _indent--;
            return "";
        }

        public override string VisitPrintStatement(PrintStatement n)
        {
            Line("Print");
            _indent++;
            Visit(n.Expression);
            _indent--;
            return "";
        }

        public override string VisitLetStatement(LetStatement n)
        {
            Line($"Let {n.Identifier.Text} : {n.Type}");
            _indent++;
            Visit(n.Value);
            _indent--;
            return "";
        }

        public override string VisitIntegerLiteral(IntegerLiteral n)
        {
            Line($"Int({n.Value})");
            return "";
        }

        public override string VisitBinaryExpr(BinaryExpression n)
        {
            Line($"BinaryOp '{n.Op}'");
            _indent++;
            Visit(n.Left);
            Visit(n.Right);
            _indent--;
            return "";
        }

        public override string VisitBooleanLiteral(BooleanLiteral n)
        {
            Line($"Bool({n.Value})");
            return "";
        }

        public override string VisitStringLiteral(StringLiteral n)
        {
            Line($"String(\"{n.Value}\")");
            return "";
        }

        public override string VisitCharLiteral(CharLiteral n)
        {
            Line($"Char('{n.Value}')");
            return "";
        }

        public override string VisitArrayLiteral(ArrayLiteral n)
        {
            Line("ArrayLiteral");
            _indent++;
            foreach (var e in n.Elements)
                Visit(e);
            _indent--;
            return "";
        }

        public override string VisitElementAccess(ElementAccess n)
        {
            Line("ElementAccess");
            _indent++;
            Line("Target:");
            _indent++;
            Visit(n.Target);
            _indent--;
            Line("Index:");
            _indent++;
            Visit(n.Index);
            _indent--;
            _indent--;
            return "";
        }

        public override string VisitCallExpression(CallExpression n)
        {
            Line("CallExpression");
            _indent++;
            Line("Callee:");
            _indent++;
            Visit(n.Callee);
            _indent--;
            Line("Args:");
            _indent++;
            foreach (var a in n.Arguments)
                Visit(a);
            _indent--;
            _indent--;
            return "";
        }

        public override string VisitHashLiteral(HashLiteral n)
        {
            Line("HashLiteral");
            _indent++;
            foreach (var entry in n.Entries)
            {
                Line("Entry");
                _indent++;
                Line("Key:");
                _indent++;
                Visit(entry.Key);
                _indent--;
                Line("Value:");
                _indent++;
                Visit(entry.Value);
                _indent--;
                _indent--;
            }
            _indent--;
            return "";
        }

        public override string VisitFunctionLiteral(FunctionLiteral n)
        {
            Line("FunctionLiteral");
            _indent++;
            Line("Parameters:");
            _indent++;
            foreach (var p in n.Parameters)
                Visit(p);
            _indent--;
            Line($"ReturnType: {n.ReturnType.Name}");
            Line("Body:");
            _indent++;
            Visit(n.Body);
            _indent--;
            _indent--;
            return "";
        }
        
        public override string VisitParameter(ParameterNode n)
        {
            Line($"Param {n.Identifier.Text} : {n.Type.Name}");
            return "";
        }
        public override string VisitIdentifier(IdentifierExpr n)
        {
            Line($"Identifier({n.Name})");
            return "";
        }


    }
}