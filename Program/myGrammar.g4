grammar myGrammar;

// PARSER RULES

program: (function | globalVariable)* EOF;

function:
	(
		returnType ID LPAREN parameterList? RPAREN LBRACE expression* RBRACE
	)
	| mainFunction;

mainFunction:
	(INT | VOID) MAIN LPAREN RPAREN LBRACE expression* RBRACE;

globalVariable: dataType ID ASSIGN constant SEMI;

expression:
	return
	| variableDeclaration SEMI
	| operation SEMI
	| functionCall SEMI
	| controlBlock
	| assignment SEMI;

controlBlock: ifBlock | forBlock | whileBlock | ifElseBlock;

ifElseBlock: ifBlock elseBlock;

ifBlock: IF LPAREN condition RPAREN LBRACE expression* RBRACE;
elseBlock: ELSE LBRACE expression* RBRACE;
forBlock:
	FOR LPAREN forInit? SEMI forCondition? SEMI forUpdate? RPAREN LBRACE expression* RBRACE;
whileBlock:
	WHILE LPAREN condition RPAREN LBRACE expression* RBRACE;

forInit: variableDeclaration | assignment;
forUpdate: increment | decrement | operation;
forCondition: comparison;

variableDeclaration:
	dataType variable (
		ASSIGN (
			constant
			| variable
			| value arithmeticOperator value
			| functionCall
		)
	)?;

assignment:
	variable ASSIGN (
		variable
		| constant
		| functionCall
		| operation
	);
functionCall: ID LPAREN argumentList? RPAREN;

parameterList: parameter (COMMA parameter)*;
parameter: dataType ID;

argumentList: value (COMMA value)*;

increment: INCREMENT variable | variable INCREMENT;
decrement: DECREMENT variable | variable DECREMENT;
return: RETURN (value | operation)? SEMI;

operation:
	value (arithmeticOperator value)+
	| compoundAssignment
	| logicalOperation
	| value (arithmeticOperator value)+ SEMI;

logicalOperation: condition;

// condition: condition AND condition | condition OR condition | LPAREN condition RPAREN | NOT
// condition | comparison | variable;

condition: orCondition;
orCondition: andCondition (OR andCondition)*;
andCondition: notCondition (AND notCondition)*;
notCondition: NOT notCondition | atom;
atom: LPAREN (condition) RPAREN | comparison | variable;
// comparison: comparisonOperator (variable | NUM);
comparison: value comparisonOperator value;

compoundAssignment: variable compoundOperator value;

compoundOperator: PLUSEQ | MINUSEQ | DIVEQ | MULTEQ | MODEQ;
arithmeticOperator: PLUS | MINUS | MULT | DIV | MOD;

value: variable | NUM | STRING_LITERAL;
variable: ID;
returnType: INT | FLOAT | DOUBLE | STRING | VOID;
dataType: INT | FLOAT | DOUBLE | STRING;
constant: NUM | STRING_LITERAL;
comparisonOperator: LT | LE | EQ | NEQ | GE | GT;
logicalConditionalOperator: AND | OR;

// -----------------------------------------------------------------

// LEXER RULES

INT: 'int';
FLOAT: 'float';
DOUBLE: 'double';
STRING: 'string';
VOID: 'void';
IF: 'if';
ELSE: 'else';
FOR: 'for';
WHILE: 'while';
RETURN: 'return';
MAIN: 'main';

PLUS: '+';
MINUS: '-';
MULT: '*';
DIV: '/';
MOD: '%';
LT: '<';
LE: '<=';
GT: '>';
GE: '>=';
EQ: '==';
NEQ: '!=';
AND: '&&';
OR: '||';
NOT: '!';
ASSIGN: '=';
PLUSEQ: '+=';
MINUSEQ: '-=';
MULTEQ: '*=';
DIVEQ: '/=';
MODEQ: '%=';
INCREMENT: '++';
DECREMENT: '--';

LPAREN: '(';
RPAREN: ')';
LBRACE: '{';
RBRACE: '}';
COMMA: ',';
SEMI: ';';

ID: [a-zA-Z_][a-zA-Z_0-9]*;

NUM: [0-9]+ ('.' [0-9]+)?;
STRING_LITERAL: '"' .*? '"';

LINE_COMMENT: '//' ~[\r\n]* -> skip;
BLOCK_COMMENT: '/*' .*? '*/' -> skip;
WHITESPACE: [ \t\r\n]+ -> skip;

ERROR: .;
NON_TERMINATED_STRING: '"' (~["\r\n])* -> type(ERROR);
NON_TERMINATED_BLOCK_COMMENT: '/*' (.)*? -> type(ERROR);