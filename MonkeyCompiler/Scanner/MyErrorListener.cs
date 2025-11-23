using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Antlr4.Runtime;
using Generated;

namespace MonkeyCompiler
{
    public class MyErrorListener : IAntlrErrorListener<IToken>, IAntlrErrorListener<int>
    {
        public List<string> ErrorMsgs { get; private set; }

        public MyErrorListener()
        {
            this.ErrorMsgs = new List<string>();
        }

        // Para el Parser (usa IToken)
        public void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            IToken offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            ErrorMsgs.Add($"PARSER ERROR - line {line}:{charPositionInLine} {msg}");
        }

        // Para el Lexer (usa int)
        public void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            int offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            ErrorMsgs.Add($"SCANNER ERROR - line {line}:{charPositionInLine} {msg}");
        }

        public bool HasErrors()
        {
            return this.ErrorMsgs.Count > 0;
        }

        public override string ToString()
        {
            if (!HasErrors())
                return "0 errors";

            StringBuilder builder = new StringBuilder();
            foreach (string s in ErrorMsgs)
            {
                builder.AppendLine(s);
            }
            return builder.ToString();
        }
    }
}