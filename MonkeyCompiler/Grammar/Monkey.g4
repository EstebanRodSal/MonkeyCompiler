grammar Monkey;

// =====================================================
// PARSER RULES
// =====================================================

program
    : (functionDeclaration | statement)* mainFunction EOF
    ;

mainFunction
    : FN MAIN LPAREN RPAREN COLON VOID_TYPE blockStatement
    ;

functionDeclaration
    : FN identifier LPAREN functionParameters? RPAREN COLON type blockStatement
    ;

functionParameters
    : parameter (COMMA parameter)*
    ;

parameter
    : identifier COLON type
    ;

type
    : INT_TYPE
    | STRING_TYPE
    | BOOL_TYPE
    | CHAR_TYPE
    | VOID_TYPE
    | arrayType
    | hashType
    | functionType
    ;

arrayType
    : ARRAY LT type GT
    ;

hashType
    : HASH LT type COMMA type GT
    ;

functionType
    : FN LPAREN functionParameterTypes? RPAREN COLON type
    ;

functionParameterTypes
    : type (COMMA type)*
    ;

// ---------------- STATEMENTS ----------------

statement
    : 
      letStatement                          #LetStmt
    | returnStatement                       #ReturnStmt               
    | expressionStatement                   #ExpressionStmt
    | ifStatement                           #IfStmt
    | blockStatement                        #BlockStmt
    | printStatement                        #PrintStmt
    ;

letStatement
    : LET identifier COLON type ASSIGN expression            #LetVarStmt
    | LET CONST identifier COLON type ASSIGN expression      #ConstVarStmt
    ;


returnStatement
    : RETURN expression    #ReturnWithValue
    | RETURN               #ReturnWithoutValue
    ;

expressionStatement
    : expression
    ;

ifStatement
    : IF expression blockStatement                      #IfOnly
    | IF expression blockStatement ELSE blockStatement  #IfElse
    ;

blockStatement
    : LBRACE statement* RBRACE
    ;

printStatement
    : PRINT LPAREN expression RPAREN
    ;

// ---------------- EXPRESSIONS ----------------

expression
    // equivalente a: additionExpression { relOp additionExpression }
    : additionExpression (relationalOp additionExpression)*
    ;

relationalOp
    : LT
    | GT
    | LE
    | GE
    | EQ
    | NEQ
    ;

additionExpression
    : multiplicationExpression ((PLUS | MINUS) multiplicationExpression)*
    ;

multiplicationExpression
    : elementExpression ((STAR | SLASH) elementExpression)*
    ;

elementExpression
    : primitiveExpression (elementAccess | callExpression)?
    ;

elementAccess
    : LBRACK expression RBRACK
    ;

callExpression
    : LPAREN expressionList? RPAREN
    ;

// ---------------- PRIMITIVES ----------------

primitiveExpression
    : integerLiteral                        #IntLiteralExpr
    | stringLiteral                         #StringLiteralExpr
    | charLiteral                           #CharLiteralExpr
    | identifier                            #IdentifierExpr
    | TRUE                                  #TrueLiteralExpr
    | FALSE                                 #FalseLiteralExpr
    | LPAREN expression RPAREN              #GroupedExpr
    | arrayLiteral                          #ArrayLiteralExpr
    | functionLiteral                       #FunctionLiteralExpr
    | hashLiteral                           #HashLiteralExpr
    ;

arrayLiteral
    : LBRACK expressionList? RBRACK
    ;

functionLiteral
    : FN LPAREN functionParameters? RPAREN COLON type blockStatement
    ;

hashLiteral
    : LBRACE hashContent (COMMA hashContent)* RBRACE
    ;

hashContent
    : expression COLON expression
    ;

expressionList
    : expression (COMMA expression)*
    ;

// ---------------- SMALL NON-TERM RULES ----------------

integerLiteral
    : INTEGER_LITERAL
    ;

stringLiteral
    : STRING_LITERAL
    ;

charLiteral
    : CHAR_LITERAL
    ;

identifier
    : IDENTIFIER
    ;

// =====================================================
// LEXER RULES
// =====================================================

// Palabras reservadas / tipos
FN          : 'fn';
MAIN        : 'main';
LET         : 'let';
CONST       : 'const';
RETURN      : 'return';
IF          : 'if';
ELSE        : 'else';
PRINT       : 'print';

INT_TYPE    : 'int';
STRING_TYPE : 'string';
BOOL_TYPE   : 'bool';
CHAR_TYPE   : 'char';
VOID_TYPE   : 'void';

ARRAY       : 'array';
HASH        : 'hash';

TRUE        : 'true';
FALSE       : 'false';

// Operadores
PLUS        : '+';
MINUS       : '-';
STAR        : '*';
SLASH       : '/';

ASSIGN      : '=';
EQ          : '==';
NEQ         : '!=';
LT          : '<';
GT          : '>';
LE          : '<=';
GE          : '>=';

// Símbolos
LPAREN      : '(';
RPAREN      : ')';
LBRACE      : '{';
RBRACE      : '}';
LBRACK      : '[';
RBRACK      : ']';
COMMA       : ',';
COLON       : ':';
SEMICOLON   : ';';

// Literales
INTEGER_LITERAL
    : [0-9]+
    ;

CHAR_LITERAL
    : '\'' ( EscapeSequence | ~['\\\r\n] ) '\''
    ;

STRING_LITERAL
    : '"' ( EscapeSequence | ~["\\\r\n] )* '"'
    ;

// Identificadores
IDENTIFIER
    : [a-zA-Z_] [a-zA-Z_0-9]*
    ;

// Escapes: \n, \t, \r, \", \', \\
fragment EscapeSequence
    : '\\' [btnrf"'\\]
    ;

// Comentarios y espacios en blanco
LINE_COMMENT
    : '//' ~[\r\n]* -> skip
    ;

// *** Comentarios /* ... */ (no anidados).  Para anidados ver nota abajo ***
BLOCK_COMMENT
    : '/*' .*? '*/' -> skip
    ;

WS
    : [ \t\r\n]+ -> skip
    ;
