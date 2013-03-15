using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace Stone.Compiler
{
    class YieldScope
    {
        // this_var: All the variables that not on stack, and get the field ref to it
        // this.field
        public HashSet<ThisVar> this_var = new HashSet<ThisVar>();

        public String name;
        public TypeBuilder type_builder;

        public MethodBuilder move_next;
        public MethodBuilder get_enumerator;
        public MethodBuilder get_enumerator2;
        public MethodBuilder get_current;
        public MethodBuilder get_current2;
        public MethodBuilder dispose;
        public MethodBuilder reset;

        public FieldBuilder state_field;
        public FieldBuilder current_field;
    }

    class FormalScope
    {
        public LocalScope local_scope = new LocalScope();

        public YieldScope yield_scope = new YieldScope();

        public Boolean has_yield = false;
        public int yield_count = 0;
    }
}
