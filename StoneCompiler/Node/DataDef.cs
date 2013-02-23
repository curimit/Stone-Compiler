using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace Stone.Compiler.Node
{
    class DataDef : AstNode
    {
        public String name;
        public List<DataField> list = new List<DataField>();
        public StoneType type;

        public DataScope scope;

        // IL Info
        public TypeBuilder type_builder;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class DataField : AstNode
    {
        public String name;
        public StoneType type;

        public AstType ast_type;

        // IL Info
        public FieldBuilder field_builder;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }
}
