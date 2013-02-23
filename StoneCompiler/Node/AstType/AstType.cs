using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stone.Compiler.Node
{
    abstract class AstType : AstNode
    {
        public StoneType type;
    }

    class AstFuncType : AstType
    {
        public AstType Type1, Type2;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class AstCrossType : AstType
    {
        public List<AstType> list = new List<AstType>();

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class AstAtomType : AstType
    {
        public String type_name;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }
}
