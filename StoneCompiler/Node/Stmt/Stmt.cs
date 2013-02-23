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

    class StmtAlloc : Stmt
    {
        public String owner;
        public Expr expr;

        public LocalVar symbol;

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
}
