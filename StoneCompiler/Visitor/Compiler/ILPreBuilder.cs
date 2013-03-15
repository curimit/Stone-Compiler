using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stone.Compiler.Node;
using System.Reflection.Emit;
using System.Reflection;
using System.Diagnostics.SymbolStore;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Collections;

namespace Stone.Compiler
{
    class ILPreBuilderData : Visitor
    {
        private ModuleBuilder module_builder;
        private TypeBuilder type_builder;

        private ScopeStack scope_stack = new ScopeStack();
        private Root root;

        public override void visit(Root node)
        {
            root = node;

            node.assembly_name = new AssemblyName("Hello");
            node.assembly_builder = Thread.GetDomain().DefineDynamicAssembly(node.assembly_name, AssemblyBuilderAccess.Save, "..\\bin");
            node.module_builder = node.assembly_builder.DefineDynamicModule("Hello" + ".exe", true); // <-- pass 'true' to track debug info.

            module_builder = node.module_builder;

            foreach (var item in node.data_block)
            {
                item.accept(this);
            }
            foreach (var item in node.class_block)
            {
                item.accept(this);
            }
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
        }

        public override void visit(ModuleDef node)
        {
        }

        public override void visit(ClassDef node)
        {
        }

        public override void visit(Proxy node)
        {
            node.type_builder = module_builder.DefineType(node.target_name, TypeAttributes.Public | TypeAttributes.Class);
            type_builder = node.type_builder;

            foreach (var item in node.message_def)
            {
                item.accept(this);
            }
        }

        public override void visit(DataDef node)
        {
            node.type_builder = module_builder.DefineType(node.name, TypeAttributes.Public | TypeAttributes.Class);
            type_builder = node.type_builder;

            foreach (var item in node.list)
            {
                item.accept(this);
            }

            type_builder.CreateType();
        }

        public override void visit(DataField node)
        {
            node.field_builder = type_builder.DefineField(node.name, node.type.get_type(), FieldAttributes.Public);
        }

        private Tuple<Type[], Type> to_func_type(StoneType _type)
        {
            Type[] clr_params_type;
            Type clr_return_type;

            Debug.Assert(_type is FuncType);
            FuncType type = _type as FuncType;
            if (type.args_type.ToString() == "Void")
            {
                clr_params_type = null;
            }
            else
            {
                clr_params_type = type.args_type.get_types();
            }
            clr_return_type = type.return_type.get_type();
            return new Tuple<Type[], Type>(clr_params_type, clr_return_type);
        }

        private Tuple<Type[], Type> to_static_message_type(StoneType _type, Type target_type)
        {
            List<Type> clr_params_type = new List<Type>();
            Type clr_return_type;

            Debug.Assert(_type is FuncType);
            FuncType type = _type as FuncType;
            
            clr_params_type.Add(target_type);
            if (type.args_type.ToString() != "Void")
            {
                foreach (var tmp in type.args_type.get_types())
                {
                    clr_params_type.Add(tmp);
                }
            }
            clr_return_type = type.return_type.get_type();


            return new Tuple<Type[], Type>(clr_params_type.ToArray(), clr_return_type);
        }

        public override void visit(LambdaClass node)
        {
            node.type_builder = module_builder.DefineType(node.name, TypeAttributes.Public | TypeAttributes.Class);
            type_builder = node.type_builder;

            var type = to_func_type(node.lambda_expr.type);
            node.method_builder = type_builder.DefineMethod("main", MethodAttributes.Public, type.Item2, type.Item1);

            foreach (var ref_scope in node.lambda_expr.ref_scopes)
            {
                ref_scope.field = type_builder.DefineField(ref_scope.scope.closure_scope.anonymous_type.ToString(), ref_scope.scope.closure_scope.anonymous_type, FieldAttributes.Public);
            }

            enter(node.lambda_expr.scope);
        }

        public override void visit(MessageDeclare node)
        {
        }

        public override void visit(MessageDef node)
        {
            var type = to_static_message_type(node.declare.type, node.defined_in.type_builder);
            node.method_builder = type_builder.DefineMethod(node.name, MethodAttributes.Public | MethodAttributes.Static, type.Item2, type.Item1);

            enter(node.scope);
        }

        public override void visit(FuncDeclare node)
        {
        }

        public override void visit(FuncDef node)
        {
            var type = to_func_type(node.declare.type);

            if (!node.scope.has_yield)
            {
                node.method_builder = module_builder.DefineGlobalMethod(node.name, MethodAttributes.Public | MethodAttributes.Static, type.Item2, type.Item1);
            }
            else
            {
                node.method_builder = module_builder.DefineGlobalMethod(node.name, MethodAttributes.Public | MethodAttributes.Static, type.Item2, type.Item1);
                Type enumerator = typeof(IEnumerator<>).MakeGenericType(type.Item2.GetGenericArguments());

                node.scope.yield_scope.type_builder = module_builder.DefineType(node.scope.yield_scope.name, TypeAttributes.Public, null, Type.EmptyTypes);
                node.scope.yield_scope.type_builder.AddInterfaceImplementation(type.Item2);
                node.scope.yield_scope.type_builder.AddInterfaceImplementation(enumerator);
                node.scope.yield_scope.type_builder.AddInterfaceImplementation(typeof(IEnumerable));
                node.scope.yield_scope.type_builder.AddInterfaceImplementation(typeof(IEnumerator));
                node.scope.yield_scope.type_builder.AddInterfaceImplementation(typeof(IDisposable));

                // declear interface function
                node.scope.yield_scope.move_next = node.scope.yield_scope.type_builder.DefineMethod("MoveNext", MethodAttributes.Public | MethodAttributes.Virtual, typeof(Boolean), Type.EmptyTypes);

                node.scope.yield_scope.get_enumerator = node.scope.yield_scope.type_builder.DefineMethod("GetEnumerator", MethodAttributes.Public | MethodAttributes.Virtual, enumerator, Type.EmptyTypes);

                node.scope.yield_scope.get_enumerator2 = node.scope.yield_scope.type_builder.DefineMethod("GetEnumerator", MethodAttributes.Public | MethodAttributes.Virtual, typeof(IEnumerator), Type.EmptyTypes);

                node.scope.yield_scope.get_current = node.scope.yield_scope.type_builder.DefineMethod("get_Current", MethodAttributes.Public | MethodAttributes.Virtual, type.Item2.GetGenericArguments()[0], Type.EmptyTypes);

                node.scope.yield_scope.get_current2 = node.scope.yield_scope.type_builder.DefineMethod("get_Current", MethodAttributes.Public | MethodAttributes.Virtual, typeof(object), Type.EmptyTypes);

                node.scope.yield_scope.dispose = node.scope.yield_scope.type_builder.DefineMethod("Dispose", MethodAttributes.Public | MethodAttributes.Virtual, null, Type.EmptyTypes);

                node.scope.yield_scope.reset = node.scope.yield_scope.type_builder.DefineMethod("Reset", MethodAttributes.Public | MethodAttributes.Virtual, null, Type.EmptyTypes);

                node.scope.yield_scope.state_field = node.scope.yield_scope.type_builder.DefineField("<>yield_state", typeof(Int32), FieldAttributes.Public);
                node.scope.yield_scope.current_field = node.scope.yield_scope.type_builder.DefineField("<>yield_current", type.Item2.GetGenericArguments()[0], FieldAttributes.Private);
            }

            if (node.name.EndsWith(".main") || node.name == "main") root.entry_method = node.method_builder;

            node.stmt_block.accept(this);

            enter(node.scope);
        }

        // FormalScope
        public void enter(FormalScope scope)
        {
            if (scope.has_yield)
            {
                foreach (var this_var in scope.yield_scope.this_var)
                {
                    if (this_var.ref_scope == null)
                    {
                        this_var.this_field = scope.yield_scope.type_builder.DefineField(this_var.info.name, this_var.info.type.get_type(), FieldAttributes.Public);
                    }
                    else
                    {
                        this_var.this_field = scope.yield_scope.type_builder.DefineField(this_var.info.name, this_var.ref_scope.anonymous_type, FieldAttributes.Public);
                    }
                }
            }

            enter(scope.local_scope);
        }

        public void enter(LocalScope scope)
        {
            if (!scope.closure_scope.has_closure_value) return;

            scope.closure_scope.anonymous_type = module_builder.DefineType(scope.closure_scope.name, TypeAttributes.Public);
            foreach (var var in scope.closure_scope.closure_var)
            {
                var.ref_scope = scope;
                var.field = scope.closure_scope.anonymous_type.DefineField(var.info.name, var.info.type.get_type(), FieldAttributes.Public);
            }

            scope.closure_scope.anonymous_type.CreateType();
        }

        public override void visit(MatchCross node)
        {
        }

        public override void visit(MatchAssignVar node)
        {
        }

        public override void visit(MatchAllocVar node)
        {
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

        public override void visit(AstEnumType node)
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
        }

        public override void visit(StmtAssign node)
        {
        }

        public override void visit(StmtCall node)
        {
        }

        public override void visit(ExprCall node)
        {
        }

        public override void visit(ExprLambda node)
        {
        }

        public override void visit(ExprArray node)
        {
        }

        public override void visit(StmtReturn node)
        {
        }

        public override void visit(StmtYield node)
        {
        }

        public override void visit(StmtIf node)
        {
            enter(node.scope);
        }

        public override void visit(StmtWhile node)
        {
            enter(node.scope);
        }

        public override void visit(StmtFor node)
        {
            enter(node.scope);
            node.body.accept(this);
        }

        public override void visit(Const node)
        {
        }

        public override void visit(ExprBin node)
        {
        }

        public override void visit(ExprMessage node)
        {
        }

        public override void visit(ExprVar node)
        {
        }

        public override void visit(ExprNewData node)
        {
        }

        public override void visit(ExprAccess node)
        {
        }

    }
}
