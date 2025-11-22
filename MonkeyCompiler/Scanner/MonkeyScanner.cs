using System;
using System.Collections.Generic;
using Antlr4.Runtime;

namespace MonkeyCompiler.Scanner
{
    public enum TokenType
    {
        Identifier,
        IntegerLiteral,
        StringLiteral,
        CharLiteral,
        BoolLiteral,
        // Palabras reservadas
        Let, Const, Fn, Main, If, Else, Return, Print,
        IntType, StringType, BoolType, CharType, VoidType,
        Array, Hash,
        // Operadores
        Plus, Minus, Star, Slash,
        Assign, Eq, Neq, Lt, Gt, Le, Ge,
        // Símbolos
        LParen, RParen, LBrace, RBrace, LBracket, RBracket,
        Comma, Colon, Semicolon,
        EOF
    }

    public class Token
    {
        public TokenType Type { get; }
        public string Lexeme { get; }
        public int Line { get; }
        public int Column { get; }

        public Token(TokenType type, string lexeme, int line, int column)
        {
            Type = type;
            Lexeme = lexeme;
            Line = line;
            Column = column;
        }

        public override string ToString()
            => $"{Type} \"{Lexeme}\" ({Line}:{Column})";
    }

    public static class MonkeyScanner
    {
        public static IEnumerable<Token> Scan(string source)
        {
            var input = new AntlrInputStream(source);
            var lexer = new MonkeyLexer(input);

            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new LexerErrorListener()); // ya lo tienes

            IToken t;
            while ((t = lexer.NextToken()).Type != TokenConstants.EOF)
            {
                yield return ConvertToken(t);
            }

            yield return new Token(TokenType.EOF, "<EOF>", 0, 0);
        }

        private static Token ConvertToken(IToken t)
        {
            var type = MapTokenType(t.Type);
            return new Token(type, t.Text, t.Line, t.Column);
        }

        private static TokenType MapTokenType(int antlrType)
{
    switch (antlrType)
    {
        case MonkeyLexer.IDENTIFIER:      return TokenType.Identifier;
        case MonkeyLexer.INTEGER_LITERAL: return TokenType.IntegerLiteral;
        case MonkeyLexer.STRING_LITERAL:  return TokenType.StringLiteral;
        case MonkeyLexer.CHAR_LITERAL:    return TokenType.CharLiteral;
        case MonkeyLexer.TRUE:
        case MonkeyLexer.FALSE:           return TokenType.BoolLiteral;

        case MonkeyLexer.LET:             return TokenType.Let;
        case MonkeyLexer.CONST:           return TokenType.Const;
        case MonkeyLexer.FN:              return TokenType.Fn;
        case MonkeyLexer.MAIN:            return TokenType.Main;   // <-- NUEVO
        case MonkeyLexer.IF:              return TokenType.If;
        case MonkeyLexer.ELSE:            return TokenType.Else;
        case MonkeyLexer.RETURN:          return TokenType.Return;
        case MonkeyLexer.PRINT:           return TokenType.Print;

        case MonkeyLexer.INT_TYPE:        return TokenType.IntType;
        case MonkeyLexer.STRING_TYPE:     return TokenType.StringType;
        case MonkeyLexer.BOOL_TYPE:       return TokenType.BoolType;
        case MonkeyLexer.CHAR_TYPE:       return TokenType.CharType;
        case MonkeyLexer.VOID_TYPE:       return TokenType.VoidType;

        case MonkeyLexer.ARRAY:           return TokenType.Array;
        case MonkeyLexer.HASH:            return TokenType.Hash;

        case MonkeyLexer.PLUS:            return TokenType.Plus;
        case MonkeyLexer.MINUS:           return TokenType.Minus;
        case MonkeyLexer.STAR:            return TokenType.Star;
        case MonkeyLexer.SLASH:           return TokenType.Slash;

        case MonkeyLexer.ASSIGN:          return TokenType.Assign;
        case MonkeyLexer.EQ:              return TokenType.Eq;
        case MonkeyLexer.NEQ:             return TokenType.Neq;
        case MonkeyLexer.LT:              return TokenType.Lt;
        case MonkeyLexer.GT:              return TokenType.Gt;
        case MonkeyLexer.LE:              return TokenType.Le;
        case MonkeyLexer.GE:              return TokenType.Ge;

        case MonkeyLexer.LPAREN:          return TokenType.LParen;
        case MonkeyLexer.RPAREN:          return TokenType.RParen;
        case MonkeyLexer.LBRACE:          return TokenType.LBrace;
        case MonkeyLexer.RBRACE:          return TokenType.RBrace;
        case MonkeyLexer.LBRACK:          return TokenType.LBracket;
        case MonkeyLexer.RBRACK:          return TokenType.RBracket;
        case MonkeyLexer.COMMA:           return TokenType.Comma;
        case MonkeyLexer.COLON:           return TokenType.Colon;
        case MonkeyLexer.SEMICOLON:       return TokenType.Semicolon;

        default:
            throw new Exception($"Token ANTLR desconocido: tipo {antlrType}");
    }
}

    }
}
