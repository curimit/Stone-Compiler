using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace Stone.Compiler.Node
{
    class Proxy : AstNode
    {
        public String name;
        public String target_name;
        public String parent_name;
        public List<MessageDef> message_def = new List<MessageDef>();

        public ProxyScope scope;

        public DataDef target;
        public ClassDef base_class;

        public String name_space;

        // IL Info
        public TypeBuilder type_builder;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }
}
