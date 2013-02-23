using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stone.Compiler.Node
{
    abstract class AstNode
    {
        public Position pos;

        public abstract void accept(Visitor visitor);
    }
}
