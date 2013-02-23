using Stone.Compiler.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace Stone.Compiler
{
    class VarInfo
    {
        public String name;
        public Position pos;
        public StoneType type;
    }

    class VarSymbol
    {
        public VarInfo info = new VarInfo();
    }

    class LocalVar : VarSymbol
    {
        public LocalBuilder local_builder;
    }

    class ThisVar : VarSymbol
    {
        // load arg0, then load field
        // then load sub field by ref_scope
        // so this_var will be like this.<>anonymous_value_1.x
        public FieldBuilder this_field, sub_field;
        public FormalScope ref_scope;
    }

    class ObjectVar : VarSymbol
    {
        // load ref_scope, then load field
        public FormalScope ref_scope;
        public FieldBuilder field;
    }
}
