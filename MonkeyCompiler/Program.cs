using System.Text;

using System;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Generated;
using MonkeyCompiler;
using MonkeyCompiler.Checker;


class Program
{
    static void Main(string[] args)
    {
    
        try
        {
            var sourcePath = "ejemplo.monkey";
            var sourceCode = File.ReadAllText(sourcePath);

            //Crear el input stream
            var input = new AntlrInputStream(sourceCode);
        
            //Crear el Lexer (Scanner)
            MonkeyLexer lexer = new MonkeyLexer(input);
        
            //Crear el stream de tokens
            CommonTokenStream tokens = new CommonTokenStream(lexer);
        
            //Crear el parser
            MonkeyParser parser = new MonkeyParser(tokens);
        
        
            var errorListener = new MyErrorListener();

            lexer.RemoveErrorListeners();
            parser.RemoveErrorListeners();

            lexer.AddErrorListener(errorListener);
            parser.AddErrorListener(errorListener);

            var parseTree = parser.program();
            

            if (errorListener.HasErrors())
            {
                Console.WriteLine("Error:");
                Console.WriteLine(errorListener.ToString());
            }
            else
            {
                var typeChecker = new TypeChecker();
                typeChecker.Visit(parseTree);
                
                if (typeChecker.HasErrors())
                {
                    foreach (var error in typeChecker.GetErrors())
                    {
                        Console.WriteLine(error);
                    }
                }
                
                Console.WriteLine("Compilation success!.\n");
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}