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
            else if (var is ThisVar)
            {
                IL.Emit(OpCodes.Ldarg_0);
                IL.Emit(OpCodes.Ldfld, (var as ThisVar).this_field);
                IL.Emit(OpCodes.Ldfld, (var as ThisVar).sub_field);
            }
            else if (var is ObjectVar)
            {
                IL.Emit(OpCodes.Ldloc, (var as ObjectVar).ref_scope.closure_scope.anonymous_target);
                IL.Emit(OpCodes.Ldfld, (var as ObjectVar).field);
            }
            else
            {
                Debug.Assert(false, "VarSymbol's type cannot recognize");
            }
        }
        
        enum ScopeVarType
        {
            LocalVar, ThisVar
        }

        class ScopeVar
        {
            public ScopeVarType type;

            public LocalBuilder local_builder;

            public FieldBuilder field_builder;
        }

        Dictionary<FormalScope, ScopeVar> scope_var = new Dictionary<FormalScope, ScopeVar>();

        public void Push(FormalScope scope, LocalBuilder local_builder)
        {
            scope_var[scope] = new ScopeVar { type = ScopeVarType.LocalVar, local_builder = local_builder };
        }

        public void Push(FormalScope scope, FieldBuilder field_builder)
        {
            scope_var[scope] = new ScopeVar { type = ScopeVarType.ThisVar, field_builder = field_builder };
        }

        public void LoadScopeVar(FormalScope scope)
        {
            ScopeVar var = scope_var[scope];
            if (var.type == ScopeVarType.LocalVar)
            {
                IL.Emit(OpCodes.Ldloc, var.local_builder);
            }
            else
            {
                IL.Emit(OpCodes.Ldarg_0);
                IL.Emit(OpCodes.Ldfld, var.field_builder);
            }
        }
    }
}
