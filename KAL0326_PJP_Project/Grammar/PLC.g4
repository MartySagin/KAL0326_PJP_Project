grammar PLC;

// ------------------- Lexer Rules -------------------
INT       : [0-9]+ ;
FLOAT     : [0-9]+ '.' [0-9]+ ;
BOOL      : 'true' | 'false' ;
STRING    : '"' .*? '"' ;

TYPE      : 'int' | 'float' | 'bool' | 'string' ;

IF        : 'if' ;
ELSE      : 'else' ;
WHILE     : 'while' ;
READ      : 'read' ;
WRITE     : 'write' ;

COMMENT   : '//' ~[\r\n]* -> skip ;
WS        : [ \t\r\n]+ -> skip ;

// Operators and symbols
ASSIGN    : '=' ;
PLUS      : '+' ;
MINUS     : '-' ;
MUL       : '*' ;
DIV       : '/' ;
MOD       : '%' ;
DOT       : '.' ;

AND       : '&&' ;
OR        : '||' ;
NOT       : '!' ;

EQ        : '==' ;
NEQ       : '!=' ;
LT        : '<' ;
GT        : '>' ;

SEMI      : ';' ;
COMMA     : ',' ;
LPAREN    : '(' ;
RPAREN    : ')' ;
LBRACE    : '{' ;
RBRACE    : '}' ;

ID        : [a-zA-Z][a-zA-Z0-9]* ;

// ------------------- Parser Rules -------------------
program     : statement* ;

statement
    : declaration
    | expressionStatement
    | readStatement
    | writeStatement
    | block
    | ifStatement
    | whileStatement
    | SEMI
    ;

declaration
    : TYPE ID (COMMA ID)* SEMI ;

expressionStatement
    : expression SEMI ;

readStatement
    : READ ID (COMMA ID)* SEMI ;

writeStatement
    : WRITE expression (COMMA expression)* SEMI ;

block
    : LBRACE statement* RBRACE ;

ifStatement
    : IF LPAREN expression RPAREN statement (ELSE statement)? ;

whileStatement
    : WHILE LPAREN expression RPAREN statement ;

expression
    : assignment ;

assignment
    : logic_or (ASSIGN assignment)?
    ;

logic_or
    : logic_and (OR logic_and)*
    ;

logic_and
    : equality (AND equality)*
    ;

equality
    : comparison ((EQ | NEQ) comparison)*
    ;

comparison
    : addition ((LT | GT) addition)*
    ;

addition
    : multiplication ((PLUS | MINUS | DOT) multiplication)*
    ;

multiplication
    : unary ((MUL | DIV | MOD) unary)*
    ;

unary
    : (NOT | MINUS) unary
    | primary
    ;

primary
    : INT
    | FLOAT
    | BOOL
    | STRING
    | ID
    | LPAREN expression RPAREN
    ;
