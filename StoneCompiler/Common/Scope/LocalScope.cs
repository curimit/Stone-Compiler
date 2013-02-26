using Stone.Compiler.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace Stone.Compiler
{
    class ClosureScope
    {
        // closure_var: All the variables that not on stack, and get the field ref to it
        public Dictionary<String, FieldBuilder> closure_var = new Dictionary<String, FieldBuilder>();

        public String name;
        public TypeBuilder anonymous_type;
        public LocalBuilder anonymous_target;

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
