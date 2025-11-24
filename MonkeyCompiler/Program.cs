using System;
using System.IO;
using Antlr4.Runtime;
using Generated;
using MonkeyCompiler;
using MonkeyCompiler.Checker;
using MonkeyCompiler.Encoder;   

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var sourcePath = "ejemplo.monkey";
            var sourceCode = File.ReadAllText(sourcePath);

            // 1) Crear el input stream
            var input = new AntlrInputStream(sourceCode);

            // 2) Crear el Lexer (Scanner)
            var lexer = new MonkeyLexer(input);

            // 3) Crear el stream de tokens
            var tokens = new CommonTokenStream(lexer);

            // 4) Crear el parser
            var parser = new MonkeyParser(tokens);

            // 5) Listeners de errores léxicos y sintácticos
            var errorListener = new MyErrorListener();
            lexer.RemoveErrorListeners();
            parser.RemoveErrorListeners();
            lexer.AddErrorListener(errorListener);
            parser.AddErrorListener(errorListener);

            // 6) Parsear el programa
            var parseTree = parser.program(); // esto ya es MonkeyParser.ProgramContext

            if (errorListener.HasErrors())
            {
                Console.WriteLine("Error de análisis (lexer/parser):");
                Console.WriteLine(errorListener.ToString());
                return;
            }

            // 7) Type checker
            var typeChecker = new TypeChecker();
            typeChecker.Visit(parseTree);

            if (typeChecker.HasErrors())
            {
                Console.WriteLine("Errores semánticos / de tipos:");
                foreach (var error in typeChecker.GetErrors())
                {
                    Console.WriteLine(error);
                }
                return;
            }

            // 8) Encoder en memoria
            var encoder = new Encoder();

            // El nombre es solo lógico (nombre del módulo) porque no se genera un .exe, antes si lo generabamos entonces para no romper cosas se le dejó eso :)
            var logicalName = "MonkeyProgram";

            encoder.Compile((MonkeyParser.ProgramContext)parseTree, logicalName);

            Console.WriteLine("Compilation success! Running Monkey program...\n");

            // 9) Ejecutar main de Monkey en memoria
            encoder.RunMonkeyMain();

            Console.WriteLine("\nProgram finished.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Runtime error:");
            Console.WriteLine(ex);
        }
    }
}
