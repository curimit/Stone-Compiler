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

	Expr_Message;
	Message_Item;

	Expr_Call;

	Expr_New_Data;

	Expr_Lambda;
	Lambda_Args;
	
	OP_PLUS  = '+';
	OP_MINUS = '-';
	OP_MUL   = '*';
	OP_DIV   = '/';

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
	: NEWLINE* 'module' UIDENT NEWLINE (INDENT module_inner NEWLINE* DEDENT) -> ^(Module_Def UIDENT module_inner)
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
	: NEWLINE* 'data' UIDENT NEWLINE (INDENT data_def_inner NEWLINE* DEDENT) -> ^(Data_Def UIDENT data_def_inner)
	;

data_def_inner
	: data_def_item+ -> ^(Data_Body data_def_item+)
	| INDENT data_def_inner NEWLINE* DEDENT -> data_def_inner
	;

data_def_item
	: NEWLINE* LIDENT '::' type -> ^(Data_Def_Item LIDENT type)
	;

// class
class_def
	: NEWLINE* 'class' UIDENT NEWLINE (INDENT class_def_inner NEWLINE* DEDENT) -> ^(Class_Def UIDENT class_def_inner)
	;

class_def_inner
	: message_declare+ -> ^(Class_Def_Body message_declare+)
	| INDENT class_def_inner NEWLINE* DEDENT -> class_def_inner
	;

// proxy
proxy_def
	: NEWLINE* 'proxy' UIDENT ':' UIDENT NEWLINE (INDENT proxy_def_inner NEWLINE* DEDENT) -> ^(Proxy_Def UIDENT UIDENT proxy_def_inner)
	;

proxy_def_inner
	: message_def+ -> ^(Proxy_Def_Body message_def+)
	| INDENT proxy_def_inner NEWLINE* DEDENT -> proxy_def_inner
	;

// message
message_declare
	: NEWLINE* LIDENT '::' type NEWLINE -> ^(Message_Declare LIDENT type)
	;

message_def
	:  message_declare NEWLINE* LIDENT message_def_args NEWLINE stmt_block -> ^(Message_Def LIDENT message_declare message_def_args stmt_block)
	;

message_def_args
	: match? -> ^(Message_Args match?)
	;

// func
func_declare
	: NEWLINE* LIDENT '::' type NEWLINE -> ^(Func_Declare LIDENT type)
	;

func_def
	:  func_declare NEWLINE* LIDENT func_def_args NEWLINE stmt_block -> ^(Func_Def LIDENT func_declare func_def_args stmt_block)
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
	: LIDENT -> ^(Match_Var LIDENT)
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
	: type_atom ('¡Á' type_atom)+ -> ^(Type_Cross type_atom+)
	| type_atom
	;

type_atom
	: UIDENT -> ^(Type_Atom UIDENT)
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
	;

// special stmt
stmt_return
	: 'return' expr NEWLINE -> ^(Stmt_Return expr)
	;
	
stmt_alloc
	: '|' LIDENT '|' '=' expr NEWLINE -> ^(Stmt_Alloc LIDENT expr)
	;

stmt_assign
	: LIDENT '=' expr NEWLINE -> ^(Stmt_Assign LIDENT expr)
	;

stmt_call
	: LIDENT '(' args_list? ')' NEWLINE -> ^(Stmt_Call LIDENT args_list?)
	;

stmt_if
	: 'if' expr NEWLINE (INDENT stmt_block NEWLINE* DEDENT) -> ^(Stmt_If expr stmt_block)
	;

// expr
expr
	: message_expr
	| lambda_expr
	;

lambda_expr
	: '¦Ë' lambda_args '=>' type NEWLINE (INDENT stmt_block NEWLINE* DEDENT) -> ^(Expr_Lambda lambda_args type stmt_block)
	;

lambda_args
	: match? -> ^(Lambda_Args match)
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
	: LIDENT ('(' args_list ')' )? -> ^(Message_Item LIDENT args_list?)
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
	: LIDENT '(' args_list? ')' -> ^(Expr_Call LIDENT args_list?)
	| access_expr
	;

access_expr
	: atom_expr (Expr_Access^ LIDENT)*
	;

atom_expr
	: LIDENT
	| INT
	| DOUBLE
	| NORMAL_STRING
	| UIDENT '(' args_list ')' -> ^(Expr_New_Data UIDENT args_list)
	| '(' expr ')' -> expr
	;

// Lexer Rules
NEWLINE
: '\n'+ ( ' ' | '\t' )*
{
       int indent = Text.Length;
	   while (indent != 0 && Text[Text.Length - indent] == '\n') indent--;
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

LIDENT: (('A'..'Z') ('a'..'z' | 'A'..'Z')* '.')* ('a'..'z') ('a'..'z' | 'A'..'Z')* ('0'..'9')*;
UIDENT: (('A'..'Z') ('a'..'z' | 'A'..'Z')* '.')* ('A'..'Z') ('a'..'z' | 'A'..'Z')* ('0'..'9')*;

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