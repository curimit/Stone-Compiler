grammar Stone;

options {
    language=CSharp3;
    TokenLabelType=CommonToken;
    output=AST;
    ASTLabelType=CommonTree;
}

tokens {
	Root;
	INDENT;
	DEDENT;

	Module_Def;

	Data_Def;
	Data_Body;
	Data_Def_Item;

	Class_Def;
	Class_Def_Body;

	Proxy_Def;
	Proxy_Def_Body;

	Message_Declare;
	Message_Args;
	Message_Def;

	Type_Cross;
	Type_Func;
	Type_Atom;

	Func_Declare;
	Func_Args;
	Func_Def;

	Match_Cross;
	Match_Var;

	Stmt_Block;
	Stmt_Return;
	Stmt_Alloc;
	Stmt_Assign;
	Stmt_Call;

	Stmt_If;
	Stmt_While;
	Stmt_For;

	Expr_Message;
	Message_Item;

	Expr_Call;
	
	Expr_New_Data;

	Expr_Array;

	Expr_Lambda;
	Lambda_Args;
	
	OP_PLUS  = '+';
	OP_MINUS = '-';
	OP_MUL   = '*';
	OP_DIV   = '/';
	
	OP_EQU = '==';
	OP_NEQ = '!=';
	OP_LSS = '<';
	OP_LEQ = '<=';
	OP_GTR = '>';
	OP_GEQ = '>=';

	Expr_Access = '.';
}

@parser :: namespace { Stone.Compiler }
@lexer  :: namespace { Stone.Compiler }

@lexer::init {
	CurrentIndent = 0;
}
@lexer::members {
	int CurrentIndent = 0;

	Queue<IToken> tokens = new Queue<IToken>();

    public override void Emit(IToken token) 
    {
        state.token = token;
        tokens.Enqueue(token);
    }
    public override IToken NextToken()
    {
        base.NextToken();
        if ( tokens.Count ==0 )
		{
			IToken token = new CommonToken(EOF, "EOF");
            token.StartIndex = CharIndex;
            token.StopIndex = CharIndex;
            return token;
		}
        return tokens.Dequeue();
    }
}

public parse
	: module_def+ NEWLINE* EOF -> ^(Root module_def+)
	;

module_def
	: NEWLINE* 'module' IDENT NEWLINE (INDENT module_inner NEWLINE* DEDENT) -> ^(Module_Def IDENT module_inner)
	;

module_inner
	: block+ -> block+
	| INDENT module_inner NEWLINE* DEDENT -> module_inner
	;

block
	: data_def
	| class_def
	| proxy_def
	| func_def
	| module_def
	;

// data
data_def
	: NEWLINE* 'data' IDENT NEWLINE (INDENT data_def_inner NEWLINE* DEDENT) -> ^(Data_Def IDENT data_def_inner)
	;

data_def_inner
	: data_def_item+ -> ^(Data_Body data_def_item+)
	| INDENT data_def_inner NEWLINE* DEDENT -> data_def_inner
	;

data_def_item
	: NEWLINE* IDENT '::' type -> ^(Data_Def_Item IDENT type)
	;

// class
class_def
	: NEWLINE* 'class' IDENT NEWLINE (INDENT class_def_inner NEWLINE* DEDENT) -> ^(Class_Def IDENT class_def_inner)
	;

class_def_inner
	: message_declare+ -> ^(Class_Def_Body message_declare+)
	| INDENT class_def_inner NEWLINE* DEDENT -> class_def_inner
	;

// proxy
proxy_def
	: NEWLINE* 'proxy' IDENT ':' IDENT NEWLINE (INDENT proxy_def_inner NEWLINE* DEDENT) -> ^(Proxy_Def IDENT IDENT proxy_def_inner)
	;

proxy_def_inner
	: message_def+ -> ^(Proxy_Def_Body message_def+)
	| INDENT proxy_def_inner NEWLINE* DEDENT -> proxy_def_inner
	;

// message
message_declare
	: NEWLINE* IDENT '::' type NEWLINE -> ^(Message_Declare IDENT type)
	;

message_def
	:  message_declare NEWLINE* IDENT message_def_args NEWLINE stmt_block -> ^(Message_Def IDENT message_declare message_def_args stmt_block)
	;

message_def_args
	: match? -> ^(Message_Args match?)
	;

// func
func_declare
	: NEWLINE* IDENT '::' type NEWLINE -> ^(Func_Declare IDENT type)
	;

func_def
	:  func_declare NEWLINE* IDENT func_def_args NEWLINE stmt_block -> ^(Func_Def IDENT func_declare func_def_args stmt_block)
	;

func_def_args
	: match? -> ^(Func_Args match?)
	;

// match
match
	: match_cross
	;

match_cross
options{
	backtrack=true;
	memoize=true;
}
	: match_var (',' match_var)+ -> ^(Match_Cross match_var+)
	| match_var
	;

match_var
	: IDENT -> ^(Match_Var IDENT)
	| '(' match ')' -> match
	;

// type
type
	: type_func
	;

type_func
options{
	backtrack=true;
	memoize=true;
}
	: type_cross '->' type_cross -> ^(Type_Func type_cross type_cross)
	| type_cross
	;

type_cross
options{
	backtrack=true;
	memoize=true;
}
	: type_atom ('*' type_atom)+ -> ^(Type_Cross type_atom+)
	| type_atom
	;

type_atom
	: IDENT -> ^(Type_Atom IDENT)
	| '(' type ')' -> type
	;

// stmt
stmt_block
	: stmt+ -> ^(Stmt_Block stmt+)
	| INDENT stmt_block NEWLINE* DEDENT -> stmt_block
	;

stmt
	: stmt_return
	| stmt_alloc
	| stmt_assign
	| stmt_call
	| stmt_if
	| stmt_while
	| stmt_for
	;

// special stmt
stmt_return
	: 'return' expr NEWLINE -> ^(Stmt_Return expr)
	;
	
stmt_alloc
	: '|' IDENT '|' '=' expr NEWLINE -> ^(Stmt_Alloc IDENT expr)
	;

stmt_assign
	: IDENT '=' expr NEWLINE -> ^(Stmt_Assign IDENT expr)
	;

stmt_call
	: IDENT '(' args_list? ')' NEWLINE -> ^(Stmt_Call IDENT args_list?)
	;

stmt_if
	: 'if' expr NEWLINE (INDENT stmt_block NEWLINE* DEDENT) -> ^(Stmt_If expr stmt_block)
	;

stmt_while
	: 'while' expr NEWLINE (INDENT stmt_block NEWLINE* DEDENT) -> ^(Stmt_While expr stmt_block)
	;

stmt_for
	: 'for' '|' IDENT '|' 'in' expr NEWLINE (INDENT stmt_block NEWLINE* DEDENT) -> ^(Stmt_For IDENT expr stmt_block)
	;

// expr
expr
	: logic_expr
	| lambda_expr
	| array_expr
	;

lambda_expr
	: '\\' lambda_args '=>' type NEWLINE (INDENT stmt_block NEWLINE* DEDENT) -> ^(Expr_Lambda lambda_args type stmt_block)
	;

lambda_args
	: match? -> ^(Lambda_Args match?)
	;

array_expr
	: '[' array_list ']' -> ^(Expr_Array array_list)
	;

array_list
	: (expr (',' expr)*)? -> expr*
	;

logic_expr
	: message_expr ((OP_EQU | OP_NEQ | OP_LSS | OP_LEQ | OP_GTR | OP_GEQ)^ message_expr)*
	;

message_expr
options{
	backtrack=true;
	memoize=true;
}
	: add_expr message_item+ -> ^(Expr_Message add_expr message_item+)
	| add_expr
	;

message_item
	: IDENT '(' args_list? ')' -> ^(Message_Item IDENT args_list?)
	;

args_list
	: expr (',' expr)* -> expr*
	;

add_expr
	: mul_expr ((OP_PLUS | OP_MINUS)^ mul_expr)*
	;

mul_expr
	: call_expr ((OP_MUL | OP_DIV)^ call_expr)*
	;

call_expr
	: IDENT '(' args_list? ')' -> ^(Expr_Call IDENT args_list?)
	| access_expr
	;

access_expr
	: atom_expr (Expr_Access^ IDENT)*
	;

atom_expr
	: IDENT
	| INT
	| DOUBLE
	| NORMAL_STRING
	| 'new' IDENT '(' args_list ')' -> ^(Expr_New_Data IDENT args_list)
	| '(' expr ')' -> expr
	;

// Lexer Rules
NEWLINE
: '\n'+ ( ' ' | '\t' )*
{
       int indent = Text.Length;
	   while (indent != 0 && Text[Text.Length - indent] != '\t' && Text[Text.Length - indent] != ' ') indent--;
	   IToken token_newline = new CommonToken(NEWLINE, "NEWLINE");
	   token_newline.StartIndex = CharIndex;
	   token_newline.StopIndex = CharIndex;
	   Emit(token_newline);
       if (indent > CurrentIndent)
       {
	          for (int i = 0; i < indent - CurrentIndent; i++)
			  {
				IToken token = new CommonToken(INDENT, "INDENT -- " + indent);
				token.StartIndex = CharIndex;
				token.StopIndex = CharIndex;
                Emit(token);
			  }
       }
       else if (indent < CurrentIndent)
       {
              for (int i=0; i < CurrentIndent - indent; i++)
              {
				IToken token = new CommonToken(DEDENT, "DEDENT -- " + CurrentIndent);
				token.StartIndex = CharIndex;
				token.StopIndex = CharIndex;
                Emit(token);
              }
       }
       else
       {
       }
       CurrentIndent = indent;
}
;

IDENT: (('a'..'z' | 'A'..'Z')+ ':')* ('a'..'z' | 'A'..'Z')+ ('0'..'9')*;

// string
NORMAL_STRING
	: '"' (~'"')* '"'
	| '\'' (~'\'')* '\''
	;

// integer
INT   : ('0'..'9')+ ;

// double
DOUBLE
    : ('0'..'9')+ '.' ('0'..'9')* EXPONENT?
    | '.' ('0'..'9')+ EXPONENT?
    | ('0'..'9')+ EXPONENT     
    ;
	
// fragment: expoent
fragment
EXPONENT :
    ('e'|'E') ('+'|'-')? ('0'..'9')+
    ;

INDENT: 'nothing_will_match_this_indent' ;
DEDENT: 'nothing_will_match_this_dedent' ;

WS: ' ' { Skip(); } ;