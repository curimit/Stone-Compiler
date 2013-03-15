using Stone.Compiler.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace Stone.Compiler
{
    class Tuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public Tuple(T1 Item1, T2 Item2)
        {
            this.Item1 = Item1;
            this.Item2 = Item2;
        }
    }

    class ClosureScope
    {
        // closure_var: All the variables that not on stack, and get the field ref to it
        public HashSet<ObjectVar> closure_var = new HashSet<ObjectVar>();

        // hashset to prevent from add same variable twice
        public HashSet<String> var_pushed = new HashSet<string>();

        public String name;
        public TypeBuilder anonymous_type;

        // either local_var or this_var
        public VarSymbol anonymous_target;

        public Boolean has_closure_value
        {
            get { return closure_var.Count() > 0; }
        }
    }

    class LocalScope
    {
        public Dictionary<String, VarSymbol> var = new Dictionary<String, VarSymbol>();

        public ClosureScope closure_scope = new ClosureScope();

        public ExprLambda ref_lambda = null;
    }
}
