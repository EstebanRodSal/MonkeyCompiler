using System;
using System.IO;
using System.Windows.Forms;
using Antlr4.Runtime;
using Generated;                 // MonkeyLexer, MonkeyParser
using MonkeyCompiler;           // MyErrorListener
using MonkeyCompiler.Checker;   // TypeChecker
using MonkeyCompiler.Encoder;   // Encoder

namespace MonkeyCompiler
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // Código ejemplo inicial
            txtSource.Text = @"fn add(a: int, b: int) : int {
                return a + b
            }
            //Codigo de ejemplo
            fn factorial(n: int) : int {
                if (n == 0) {
                    return 1
                } else {
                    let previous: int = n - 1
                    let result: int = factorial(previous)
                    return n * result
                }
            }

            fn sumArray(values: array<int>) : int {
                let first: int = values[0]
                let second: int = values[1]
                let third: int = values[2]
                let sum: int = first + second + third
                return sum
            }

            fn buildFullName(firstName: string, lastName: string) : string {
                let space: string = "" ""
                let withSpace: string = firstName + space
                let fullName: string = withSpace + lastName
                return fullName
            }

            fn isAdult(age: int) : bool {
                if (age >= 18) {
                    return true
                } else {
                    return false
                }
            }

            fn max3(a: int, b: int, c: int) : int {
                let maxValue: int = a

                if (b > maxValue) {
                    let newMax: int = b
                    return max3Internal(newMax, c)
                } else {
                    return max3Internal(maxValue, c)
                }
            }

            fn max3Internal(currentMax: int, other: int) : int {
                if (other > currentMax) {
                    return other
                } else {
                    return currentMax
                }
            }

            fn main() : void {
                let x: int = add(3, 4)
                let fact5: int = factorial(5)

                let numbers: array<int> = [1, 2, 3]
                let total: int = sumArray(numbers)

                let fullName: string = buildFullName(""Juan"", ""López"")

                let age: int = 20
                let adult: bool = isAdult(age)

                let biggest: int = max3(10, 5, 8)

                print(x)
                print(fact5)
                print(total)
                print(fullName)
                print(adult)
                print(biggest)
            }";
                    }

        private void btnCompileRun_Click(object sender, EventArgs e)
        {
            txtOutput.Clear();
            var sourceCode = txtSource.Text;

            // Listener de errores nuevo para esta compilación
            var errorListener = new MyErrorListener();

            try
            {
                // 1) Crear input para ANTLR
                var input = new AntlrInputStream(sourceCode);

                // 2) LEXER
                var lexer = new MonkeyLexer(input);
                lexer.RemoveErrorListeners();
                lexer.AddErrorListener(errorListener);   // errores léxicos

                // 3) TOKENS
                var tokenStream = new CommonTokenStream(lexer);

                // 4) PARSER
                var parser = new MonkeyParser(tokenStream);
                parser.RemoveErrorListeners();
                parser.AddErrorListener(errorListener);  // errores sintácticos

                // 5) Regla inicial de la gramática
                var parseTree = parser.program();        // MonkeyParser.ProgramContext

                // 6) Revisar errores léxicos / sintácticos
                if (errorListener.HasErrors())
                {
                    txtOutput.Text = "Error de análisis (lexer/parser):\r\n\r\n";
                    txtOutput.AppendText(errorListener.ToString());
                    return;
                }

                txtOutput.Text = "Análisis léxico y sintáctico exitoso.\r\n";

                // 7) Type checker
                var typeChecker = new TypeChecker();
                typeChecker.Visit(parseTree);

                if (typeChecker.HasErrors())
                {
                    txtOutput.AppendText("\r\nErrores semánticos / de tipos:\r\n\r\n");
                    foreach (var err in typeChecker.GetErrors())
                    {
                        txtOutput.AppendText(err + "\r\n");
                    }
                    return;
                }

                txtOutput.AppendText("\r\nChequeo de tipos exitoso.\r\n");

                // 8) Encoder en memoria
                var encoder = new Encoder.Encoder();

                // Nombre lógico del "módulo" (no se genera .exe)
                var logicalName = "MonkeyProgram";

                encoder.Compile((MonkeyParser.ProgramContext)parseTree, logicalName);

                txtOutput.AppendText("\r\nCompilation success! Running Monkey program...\r\n\r\n");

                // 9) Ejecutar main de Monkey en memoria y capturar la salida de Console
                var originalOut = Console.Out;
                var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                try
                {
                    encoder.RunMonkeyMain();
                }
                finally
                {
                    // Restaurar la salida original de la consola
                    Console.SetOut(originalOut);
                }

                // Mostrar la salida del programa en el txtOutput
                var programOutput = stringWriter.ToString();
                if (!string.IsNullOrEmpty(programOutput))
                {
                    txtOutput.AppendText(programOutput);
                }

                txtOutput.AppendText("\r\nProgram finished.\r\n");
            }
            catch (Exception ex)
            {
                // Por si algo explota fuera del listener / encoder
                txtOutput.Text = "Runtime error:\r\n" + ex;
            }
        }
    }
}
