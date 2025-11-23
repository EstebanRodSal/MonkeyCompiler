using System;
using System.Windows.Forms;
using Antlr4.Runtime;
using Generated;             // MonkeyLexer, MonkeyParser
using MonkeyCompiler;       // MyErrorListener está en este namespace

namespace MonkeyCompiler
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // Código ejemplo inicial (opcional)
            txtSource.Text = @"fn main() : void {
    let age: int = 10 * (20 / 2)
    let name: string = ""Monkey""
    let ok: bool = true

    print(age)
    print(name)
    print(ok)
}";
        }

        private void btnCompileRun_Click(object sender, EventArgs e)
        {
            txtOutput.Clear();
            var sourceCode = txtSource.Text;

            // Creamos un listener nuevo para esta compilación
            var errorListener = new MyErrorListener();

            try
            {
                // 1. Crear input para ANTLR
                var input = new AntlrInputStream(sourceCode);

                // 2. LEXER
                var lexer = new MonkeyLexer(input);
                lexer.RemoveErrorListeners();                // quitar listeners por defecto
                lexer.AddErrorListener(errorListener);       // nuestro listener (SCANNER ERROR)

                // 3. TOKENS
                var tokenStream = new CommonTokenStream(lexer);

                // 4. PARSER
                var parser = new MonkeyParser(tokenStream);
                parser.RemoveErrorListeners();
                parser.AddErrorListener(errorListener);      // mismo listener (PARSER ERROR)

                // 5. Regla inicial de la gramática
                var tree = parser.program();

                // 6. Revisar si el listener acumuló errores
                if (errorListener.HasErrors())
                {
                    txtOutput.Text = "Se encontraron errores:\r\n\r\n";
                    txtOutput.AppendText(errorListener.ToString());
                    return;
                }

                // Si llegamos aquí, no hubo errores léxicos ni sintácticos
                txtOutput.Text = "Compilación exitosa (léxico + sintaxis).\r\n";

                // Opcional: mostrar el árbol de ANTLR
                // txtOutput.AppendText("\r\nParse tree:\r\n");
                // txtOutput.AppendText(tree.ToStringTree(parser));
            }
            catch (Exception ex)
            {
                // Por si algo explota fuera del listener
                txtOutput.Text = "ERROR inesperado:\r\n" + ex.Message;
            }
        }
    }
}
