using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Stone.Compiler.Node;
using System.Reflection.Emit;
using System.Reflection;
using System.Diagnostics.SymbolStore;
using System.Diagnostics;
using System.IO;

namespace Stone.Compiler
{
    partial class ILCompiler : Visitor
    {
        public void LoadVarValue(VarSymbol var)
        {
            if (var is LocalVar)
            {
                IL.Emit(OpCodes.Ldloc, (var as LocalVar).local_builder);
            }
            else if (var is HeapVar)
            {
                IL.Emit(OpCodes.Ldarg_0);
                IL.Emit(OpCodes.Ldfld, (var as HeapVar).this_field);
                IL.Emit(OpCodes.Ldfld, (var as HeapVar).closure_field);
            }
            else if (var is ObjectVar)
            {
                LoadVarValue((var as ObjectVar).ref_scope.closure_scope.anonymous_target);
                IL.Emit(OpCodes.Ldfld, (var as ObjectVar).field);
            }
            else if (var is ThisVar)
            {
                IL.Emit(OpCodes.Ldarg_0);
                IL.Emit(OpCodes.Ldfld, (var as ThisVar).this_field);
            }
            else
            {
                Debug.Assert(false, "VarSymbol's type cannot recognize");
            }
        }
    }
}
