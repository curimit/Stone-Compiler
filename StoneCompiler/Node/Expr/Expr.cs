using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace Stone.Compiler.Node
{
    abstract class Expr : AstNode
    {
        public StoneType type;
    }

    class ExprBin : Expr
    {
        public Expr L, R;
        public int Op;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }

        public String get_op()
        {
            switch (Op)
            {
                case StoneParser.OP_PLUS: return "+";
                case StoneParser.OP_MINUS: return "-";
                case StoneParser.OP_MUL: return "*";
                case StoneParser.OP_DIV: return "/";
                case StoneParser.OP_EQU: return "==";
                case StoneParser.OP_NEQ: return "!=";
                case StoneParser.OP_LSS: return "<";
                case StoneParser.OP_LEQ: return "<=";
                case StoneParser.OP_GTR: return ">";
                case StoneParser.OP_GEQ: return ">=";
            }
            return "???";
        }
    }

    class ExprMessage : Expr
    {
        public Expr owner;
        public String name;
        public List<Expr> args = new List<Expr>();

        public MessageDef message;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class ExprLambda : Expr
    {
        public StmtBlock stmt_block = new StmtBlock();
        public Match args;

        public AstType ast_type;

        public LambdaClass lambda_class;

        public LocalScope scope;

        public HashSet<HeapVar> heap_vars = new HashSet<HeapVar>();

        public HashSet<RefScope> ref_scopes = new HashSet<RefScope>();

        public class RefScope
        {
            public LocalScope scope;
            public FieldBuilder field;

            public override int GetHashCode()
            {
                return scope.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return (obj is RefScope) && ((obj as RefScope).scope == scope);
            }
        }

        public int params_count
        {
            get
            {
                return type.get_types().Count();
            }
        }

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class Const : Expr
    {
        public const int Int = 0;
        public const int Double = 1;
        public const int String = 2;

        public int const_type;

        public int Value_Int;
        public double Value_Double;
        public String Value_String;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class ExprVar : Expr
    {
        public String name;

        public Boolean is_func;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class ExprNewData : Expr
    {
        public String data_name;
        public List<Expr> args = new List<Expr>();

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class ExprArray : Expr
    {
        public List<Expr> values = new List<Expr>();

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class ExprAccess : Expr
    {
        public Expr expr;
        public String name;

        // IL filed builder
        public FieldBuilder field_builder;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class ExprCall : Expr
    {
        public String owner;
        public List<Expr> args = new List<Expr>();

        // IL filed builder
        public FieldBuilder field_builder;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }
}
