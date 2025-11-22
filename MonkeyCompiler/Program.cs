using System.Text;

using System;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using MonkeyCompiler.Ast;
using MonkeyCompiler.Scanner;   // LexerErrorListener
using MonkeyCompiler;           // ParserErrorListener


class Program
{
    static void Main(string[] args)
    {
        var sourcePath = "ejemplo.monkey";
        var sourceCode = File.ReadAllText(sourcePath);

        // 1. Scanner ANTLR
        var input  = new AntlrInputStream(sourceCode);
        var lexer  = new MonkeyLexer(input);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(new LexerErrorListener());

        // 2. Tokens para el parser
        var tokenStream = new CommonTokenStream(lexer);

        // 3. Parser ANTLR (top-down)
        var parser = new MonkeyParser(tokenStream);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new ParserErrorListener());

        try
        {
            // 4. Parse tree (árbol de análisis sintáctico de ANTLR)
            var parseTree = parser.program();   // regla inicial

            // 5. Convertir parse tree -> AST
            var builder = new ParseTreeToAstVisitor();
            var programAst = (ProgramNode)builder.Visit(parseTree);

            Console.WriteLine("Parseo correcto.\n");

            // 6. Imprimir AST (texto, por ahora)
            var printer = new AstPrintVisitor();
            var astText = printer.Print(programAst);
            Console.WriteLine("AST:");
            Console.WriteLine(astText);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}