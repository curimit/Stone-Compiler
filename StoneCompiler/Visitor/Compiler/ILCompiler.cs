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
using System.Collections;

namespace Stone.Compiler
{
    partial class ILCompiler : Visitor
    {
        private TypeBuilder type_builder;
        private MethodBuilder method_builder;
        private ILGenerator IL;

        private ScopeStack scope_stack = new ScopeStack();
        private Root root;

        public override void visit(Root node)
        {
            root = node;

            foreach (var item in node.proxy_block)
            {
                item.accept(this);
            }
            foreach (var item in node.func_block)
            {
                item.accept(this);
            }
            foreach (var item in node.lambda_class_block)
            {
                item.accept(this);
            }

            // bake it 
            node.module_builder.CreateGlobalFunctions();
            node.module_builder.SetUserEntryPoint(method_builder);
            node.assembly_builder.SetEntryPoint(node.entry_method);
            node.assembly_builder.Save("Hello" + ".exe");
        }

        public override void visit(ModuleDef node)
        {
        }

        public override void visit(ClassDef node)
        {
        }

        public override void visit(Proxy node)
        {
            type_builder = node.type_builder;

            foreach (var item in node.message_def)
            {
                item.accept(this);
            }

            type_builder.CreateType();
        }

        public override void visit(DataDef node)
        {
        }

        public override void visit(DataField node)
        {
        }

        public override void visit(LambdaClass node)
        {
            scope_stack.open(node.lambda_expr.scope);
            root.scope.name_space = node.name_space;

            type_builder = node.type_builder;

            // define field
            Dictionary<LocalScope, FieldBuilder> dict = new Dictionary<LocalScope, FieldBuilder>();
            foreach (var ref_scope in node.lambda_expr.ref_scopes)
            {
                dict[ref_scope.scope] = ref_scope.field;
                Push(ref_scope.scope, ref_scope.field);
            }
            foreach (var item in node.lambda_expr.scope.var)
                if (item.Value is ThisVar)
                {
                    ThisVar var = item.Value as ThisVar;
                    var.this_field = dict[var.ref_scope];
                    Debug.Assert(var.ref_scope.closure_scope.closure_var.ContainsKey(var.info.name));
                    var.sub_field = var.ref_scope.closure_scope.closure_var[var.info.name];
                }

            method_builder = node.method_builder;
            IL = method_builder.GetILGenerator();

            // load parameters
            load_parameters(node.lambda_expr.type.params_count() + 1);

            enter_formal_scope(node.lambda_expr.scope);

            if (node.lambda_expr.args != null)
            {
                node.lambda_expr.args.accept(this);
            }

            IL.Emit(OpCodes.Pop);

            node.lambda_expr.stmt_block.accept(this);

            type_builder.CreateType();
            scope_stack.close();
        }

        public override void visit(MessageDeclare node)
        {
        }

        public override void visit(MessageDef node)
        {
            scope_stack.open(node.scope);
            root.scope.name_space = node.name_space;

            method_builder = node.method_builder;
            IL = method_builder.GetILGenerator();

            // load parameters
            load_parameters(node.declare.type.params_count() + 1);

            enter_formal_scope(node.scope);

            if (node.args != null)
            {
                node.args.accept(this);
            }

            IL.Emit(OpCodes.Pop);

            node.stmt_block.accept(this);

            scope_stack.close();
        }

        public override void visit(FuncDeclare node)
        {
        }

        public override void visit(FuncDef node)
        {
            scope_stack.open(node.scope);
            root.scope.name_space = node.name_space;

            method_builder = node.method_builder;
            IL = method_builder.GetILGenerator();

            // load parameters
            load_parameters(node.declare.type.params_count());

            enter_formal_scope(node.scope);

            if (node.args != null)
            {
                node.args.accept(this);
            }

            node.stmt_block.accept(this);

            scope_stack.close();
        }

        public void load_parameters(int ct)
        {
            if (ct >= 1) IL.Emit(OpCodes.Ldarg_0);
            if (ct >= 2) IL.Emit(OpCodes.Ldarg_1);
            if (ct >= 3) IL.Emit(OpCodes.Ldarg_2);
            if (ct >= 4) IL.Emit(OpCodes.Ldarg_3);

            for (int i = 4; i < ct; i++)
            {
                IL.Emit(OpCodes.Ldarg_S, i);
            }
        }

        public void enter_formal_scope(LocalScope scope)
        {
            // define var in scope
            foreach (var var in scope.var.Values)
            {
                if (var is LocalVar)
                {
                    LocalVar local_var = var as LocalVar;
                    local_var.local_builder = IL.DeclareLocal(local_var.info.type.get_type());
                    local_var.local_builder.SetLocalSymInfo(local_var.info.name);
                }
            }

            if (!scope.closure_scope.has_closure_value) return;

            scope.closure_scope.anonymous_target = IL.DeclareLocal(scope.closure_scope.anonymous_type);

            IL.Emit(OpCodes.Newobj, scope.closure_scope.anonymous_type.GetConstructor(new Type[]{}));

            IL.Emit(OpCodes.Stloc, scope.closure_scope.anonymous_target);

            Push(scope, scope.closure_scope.anonymous_target);
        }

        public override void visit(MatchCross node)
        {
            Stack<Match> stack = new Stack<Match>();
            foreach (var item in node.list) stack.Push(item);
            foreach (var item in stack)
            {
                item.accept(this);
            }
        }

        public override void visit(MatchVar node)
        {
            // load a value from stack
            if (node.symbol.local_builder == null)
            {
                node.symbol.local_builder = IL.DeclareLocal(node.symbol.info.type.get_type());
                node.symbol.local_builder.SetLocalSymInfo(node.symbol.info.name);
            }

            IL.Emit(OpCodes.Stloc, node.symbol.local_builder);
        }

        public override void visit(AstFuncType node)
        {
        }

        public override void visit(AstCrossType node)
        {
        }

        public override void visit(AstAtomType node)
        {
        }

        public override void visit(StmtBlock node)
        {
            foreach (var item in node.list)
            {
                item.accept(this);
            }
        }

        public override void visit(StmtAlloc node)
        {
            foreach (var var in scope_stack.stack.First().var.Values)
            {
                if (var.info.name == node.symbol.info.name)
                {
                    node.symbol = var;
                }
            }

            if (node.symbol is LocalVar)
            {
                node.expr.accept(this);
                IL.Emit(OpCodes.Stloc, (node.symbol as LocalVar).local_builder);
            }
            else if (node.symbol is ThisVar)
            {
                IL.Emit(OpCodes.Ldarg_0);
                IL.Emit(OpCodes.Ldfld, (node.symbol as ThisVar).this_field);
                node.expr.accept(this);
                IL.Emit(OpCodes.Stfld, (node.symbol as ThisVar).sub_field);
            }
            else if (node.symbol is ObjectVar)
            {
                IL.Emit(OpCodes.Ldloc, (node.symbol as ObjectVar).ref_scope.closure_scope.anonymous_target);
                node.expr.accept(this);
                IL.Emit(OpCodes.Stfld, (node.symbol as ObjectVar).field);
            }
        }

        public override void visit(StmtAssign node)
        {
            VarSymbol var = scope_stack.try_find_var(node.owner, node.pos);
            Debug.Assert(var != null);

            if (var is LocalVar)
            {
                node.expr.accept(this);
                IL.Emit(OpCodes.Stloc, (var as LocalVar).local_builder);
            }
            else if (var is ThisVar)
            {
                IL.Emit(OpCodes.Ldarg_0);
                IL.Emit(OpCodes.Ldfld, (var as ThisVar).this_field);
                node.expr.accept(this);
                IL.Emit(OpCodes.Stfld, (var as ThisVar).sub_field);
            }
            else if (var is ObjectVar)
            {
                IL.Emit(OpCodes.Ldloc, (var as ObjectVar).ref_scope.closure_scope.anonymous_target);
                node.expr.accept(this);
                IL.Emit(OpCodes.Stfld, (var as ObjectVar).field);
            }
        }

        public override void visit(StmtCall node)
        {
            foreach (var item in node.args)
            {
                item.accept(this);
            }
            // Call
            if (node.owner == "print")
            {
                Debug.Assert(node.args.Count == 1);
                if (node.args.First().type == BaseType.INT)
                {
                    IL.Emit(OpCodes.Call, typeof(System.Console).GetMethod("WriteLine", new System.Type[] { typeof(int) }));
                }
                else if (node.args.First().type == BaseType.STRING)
                {
                    IL.Emit(OpCodes.Call, typeof(System.Console).GetMethod("WriteLine", new System.Type[] { typeof(string) }));
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            else
            {
                List<FuncDef> func_list = root.scope.try_find_func(node.owner);
                Debug.Assert(func_list.Count == 1);
                FuncDef func = func_list.First();

                IL.Emit(OpCodes.Call, func.method_builder);
            }

        }

        public override void visit(ExprCall node)
        {
            // Call
            if (node.owner == "print")
            {
                foreach (var item in node.args)
                {
                    item.accept(this);
                }
                Debug.Assert(false);
            }
            else
            {
                // try if node.owner is a var but type with function
                VarSymbol var = scope_stack.try_find_var(node.owner, node.pos);
                if (var != null && var.info.type is FuncType)
                {
                    LoadVarValue(var);
                    foreach (var item in node.args)
                    {
                        item.accept(this);
                    }
                    IL.Emit(OpCodes.Callvirt, var.info.type.get_type().GetMethod("Invoke"));
                }
                else
                {
                    foreach (var item in node.args)
                    {
                        item.accept(this);
                    }
                    List<FuncDef> symbol = root.scope.try_find_func(node.owner);
                    Debug.Assert(symbol.Count == 1);
                    FuncDef func = symbol.First();

                    IL.Emit(OpCodes.Call, func.method_builder);
                }
            }
        }

        public override void visit(StmtReturn node)
        {
            if (node.expr != null)
            {
                node.expr.accept(this);
            }
            IL.Emit(OpCodes.Ret);
        }

        public override void visit(StmtIf node)
        {
            scope_stack.open(node.scope);

            Label exit = IL.DefineLabel();
            node.condition.accept(this);
            IL.Emit(OpCodes.Brfalse, exit);

            enter_formal_scope(node.scope);

            node.if_true.accept(this);
            IL.MarkLabel(exit);

            scope_stack.close();
        }

        public override void visit(StmtWhile node)
        {
            scope_stack.open(node.scope);

            Label start = IL.DefineLabel();
            Label check = IL.DefineLabel();
            
            IL.Emit(OpCodes.Br, check);

            IL.MarkLabel(start);
            enter_formal_scope(node.scope);
            node.body.accept(this);

            IL.MarkLabel(check);
            node.condition.accept(this);
            IL.Emit(OpCodes.Brtrue, start);

            scope_stack.close();
        }

        public override void visit(StmtFor node)
        {
            scope_stack.open(node.scope);

            Label start = IL.DefineLabel();
            Label check = IL.DefineLabel();

            node.expr.accept(this);

            Type array_type = node.expr.type.get_type();
            Type member_type = (node.expr.type as ArrayType).member_type.get_type();

            Type enumerator_type = typeof(IEnumerator<>);
            enumerator_type = enumerator_type.MakeGenericType(member_type);

            Type enumerable_type = typeof(IEnumerable<>);
            enumerable_type = enumerable_type.MakeGenericType(member_type);

            LocalBuilder iterator = IL.DeclareLocal(enumerator_type);

            IL.Emit(OpCodes.Castclass, enumerable_type);
            IL.Emit(OpCodes.Callvirt, enumerable_type.GetMethod("GetEnumerator"));
            IL.Emit(OpCodes.Stloc, iterator);

            IL.Emit(OpCodes.Br, check);

            IL.MarkLabel(start);

            enter_formal_scope(node.scope);

            // set to ref_var
            foreach (var var in scope_stack.stack.First().var.Values)
            {
                if (var.info.name == node.symbol.info.name)
                {
                    node.symbol = var;
                }
            }

            if (node.symbol is LocalVar)
            {
                IL.Emit(OpCodes.Ldloc, iterator);
                IL.Emit(OpCodes.Callvirt, enumerator_type.GetMethod("get_Current"));

                IL.Emit(OpCodes.Stloc, (node.symbol as LocalVar).local_builder);
            }
            else if (node.symbol is ThisVar)
            {
                IL.Emit(OpCodes.Ldarg_0);
                IL.Emit(OpCodes.Ldfld, (node.symbol as ThisVar).this_field);

                IL.Emit(OpCodes.Ldloc, iterator);
                IL.Emit(OpCodes.Callvirt, enumerator_type.GetMethod("get_Current"));

                IL.Emit(OpCodes.Stfld, (node.symbol as ThisVar).sub_field);
            }
            else if (node.symbol is ObjectVar)
            {
                IL.Emit(OpCodes.Ldloc, (node.symbol as ObjectVar).ref_scope.closure_scope.anonymous_target);

                IL.Emit(OpCodes.Ldloc, iterator);
                IL.Emit(OpCodes.Callvirt, enumerator_type.GetMethod("get_Current"));

                IL.Emit(OpCodes.Stfld, (node.symbol as ObjectVar).field);
            }

            node.body.accept(this);

            IL.MarkLabel(check);

            IL.Emit(OpCodes.Ldloc, iterator);
            IL.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetMethod("MoveNext"));

            IL.Emit(OpCodes.Brtrue, start);

            scope_stack.close();
        }

        public override void visit(ExprLambda node)
        {
            var constructer = node.lambda_class.type_builder.DefineDefaultConstructor(MethodAttributes.Public);
            IL.Emit(OpCodes.Newobj, constructer);
            foreach (var up_scope in node.ref_scopes)
            {
                IL.Emit(OpCodes.Dup);
                LoadScopeVar(up_scope.scope);
                IL.Emit(OpCodes.Stfld, up_scope.field);
            }
            IL.Emit(OpCodes.Dup);
            IL.Emit(OpCodes.Ldvirtftn, node.lambda_class.method_builder);
            IL.Emit(OpCodes.Newobj, node.type.get_type().GetConstructor(new Type[] { typeof(object), typeof(IntPtr) }));
        }

        public override void visit(ExprArray node)
        {
            IL.Emit(OpCodes.Ldc_I4, node.values.Count);
            IL.Emit(OpCodes.Newarr, node.values.First().type.get_type());
            int i = 0;
            foreach (var item in node.values)
            {
                IL.Emit(OpCodes.Dup);
                IL.Emit(OpCodes.Ldc_I4, i++);

                item.accept(this);

                IL.Emit(OpCodes.Stelem_I4);
            }
        }

        public override void visit(Const node)
        {
            switch (node.const_type)
            {
                case Const.Int:
                    IL.Emit(OpCodes.Ldc_I4, node.Value_Int);
                    break;

                case Const.String:
                    IL.Emit(OpCodes.Ldstr, node.Value_String);
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }
        }

        public override void visit(ExprBin node)
        {
            node.L.accept(this);
            node.R.accept(this);
            switch (node.Op)
            {
                case StoneParser.OP_PLUS:
                    IL.Emit(OpCodes.Add);
                    break;
                case StoneParser.OP_MINUS:
                    IL.Emit(OpCodes.Sub);
                    break;
                case StoneParser.OP_MUL:
                    IL.Emit(OpCodes.Mul);
                    break;
                case StoneParser.OP_DIV:
                    IL.Emit(OpCodes.Div);
                    break;


                case StoneParser.OP_EQU:
                    IL.Emit(OpCodes.Ceq);
                    break;
                case StoneParser.OP_NEQ:
                    IL.Emit(OpCodes.Ceq);
                    IL.Emit(OpCodes.Ldc_I4, 0);
                    IL.Emit(OpCodes.Ceq);
                    break;

                case StoneParser.OP_LSS:
                    IL.Emit(OpCodes.Clt);
                    break;

                case StoneParser.OP_LEQ:
                    IL.Emit(OpCodes.Cgt);
                    IL.Emit(OpCodes.Ldc_I4, 0);
                    IL.Emit(OpCodes.Ceq);
                    break;

                case StoneParser.OP_GTR:
                    IL.Emit(OpCodes.Cgt);
                    break;

                case StoneParser.OP_GEQ:
                    IL.Emit(OpCodes.Clt);
                    IL.Emit(OpCodes.Ldc_I4, 0);
                    IL.Emit(OpCodes.Ceq);
                    break;


                default:
                    throw new NotImplementedException();
            }
        }

        public override void visit(ExprMessage node)
        {
            node.owner.accept(this);
            foreach (var item in node.args)
            {
                item.accept(this);
            }
            IL.Emit(OpCodes.Call, node.message.method_builder);
        }

        public override void visit(ExprVar node)
        {
            if (!node.is_func)
            {
                VarSymbol var = scope_stack.try_find_var(node.name, node.pos);
                Debug.Assert(var != null);
                LoadVarValue(var);
            }
            else
            {
                List<FuncDef> list = root.scope.try_find_func(node.name);
                Debug.Assert(list.Count == 1);
                FuncDef func_def = list.First();

                IL.Emit(OpCodes.Ldnull);
                IL.Emit(OpCodes.Ldftn, func_def.method_builder);
                IL.Emit(OpCodes.Newobj, func_def.declare.type.get_type().GetConstructor(new Type[] { typeof(object), typeof(IntPtr) }));
            }
        }

        public override void visit(ExprNewData node)
        {
            IL.Emit(OpCodes.Newobj, node.type.get_type());
            var fields = node.type.get_type().GetFields();
            for (int i = 0; i < node.args.Count; i++)
            {
                IL.Emit(OpCodes.Dup);
                node.args[i].accept(this);
                IL.Emit(OpCodes.Stfld, fields[i]);
            }
        }

        public override void visit(ExprAccess node)
        {
            Debug.Assert(node.expr.type is DataType);
            DataScope scope = (node.expr.type as DataType).data_def.scope;
            DataField field = scope.try_find_field(node.name);
            Debug.Assert(field != null);
            node.field_builder = field.field_builder;

            node.expr.accept(this);

            IL.Emit(OpCodes.Ldfld, node.field_builder);
        }
    }
}
