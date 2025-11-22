using System;
using System.IO;
using Antlr4.Runtime;

namespace MonkeyCompiler
{
    public class ParserErrorListener : BaseErrorListener
    {
        public override void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            IToken offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            throw new Exception(
                $"Error sintáctico en línea {line}, columna {charPositionInLine}: {msg}");
        }
    }
}
