using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stone.Compiler.Node
{
    abstract class Stmt : AstNode
    {

    }

    class StmtBlock : AstNode
    {
        public List<Stmt> list = new List<Stmt>();

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class StmtReturn : Stmt
    {
        public Expr expr = null;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class StmtYield : Stmt
    {
        public Expr expr = null;
        public int yield_order;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class StmtAlloc : Stmt
    {
        public String owner;
        public Expr expr;

        public VarSymbol symbol;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class StmtAssign : Stmt
    {
        public String owner;
        public Expr expr;

        public VarSymbol symbol;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class StmtCall : Stmt
    {
        public String owner;
        public List<Expr> args = new List<Expr>();

        public StoneType type;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class StmtIf : Stmt
    {
        public Expr condition;
        public StmtBlock if_true;

        public LocalScope scope;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class StmtWhile : Stmt
    {
        public Expr condition;
        public StmtBlock body;

        public LocalScope scope;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class StmtFor : Stmt
    {
        public String owner;
        public Expr expr;
        public StmtBlock body;

        public VarSymbol symbol;
        public VarSymbol iterator;

        public Position var_pos;

        public LocalScope scope;

        public Type iterator_member_type;
        public Type iterator_enumerator_type;
        public Type iterator_enumerable_type;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }
}
