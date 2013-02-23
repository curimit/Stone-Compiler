using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stone.Compiler.Node
{
    class ClassDef : AstNode
    {
        public String name;
        public List<MessageDeclare> list = new List<MessageDeclare>();

        public ClassScope scope;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }
}
