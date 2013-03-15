using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stone.Compiler.Node
{
    abstract class Match : AstNode
    {
        public StoneType type;
    }

    class MatchCross : Match
    {
        public List<Match> list = new List<Match>();

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class MatchAssignVar : Match
    {
        public String name;

        public VarSymbol symbol;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class MatchAllocVar : Match
    {
        public String name;

        public VarSymbol symbol;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }
}
