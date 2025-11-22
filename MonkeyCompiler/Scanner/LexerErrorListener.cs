using System;
using System.IO;          // por TextWriter
using Antlr4.Runtime;

namespace MonkeyCompiler.Scanner
{
    // Listener de errores LÉXICOS (para MonkeyLexer)
    public class LexerErrorListener : IAntlrErrorListener<int>
    {
        public void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            int offendingSymbol,          // <-- nota: int, no IToken
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            throw new Exception(
                $"Error léxico en línea {line}, columna {charPositionInLine}: {msg}");
        }
    }
}