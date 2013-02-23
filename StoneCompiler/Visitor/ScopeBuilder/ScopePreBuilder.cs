using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stone.Compiler.Node;
using System.Diagnostics;

namespace Stone.Compiler
{
    // Deal with namespace, move all data, class, proxy, func to global with full path
    // After this, we will never use module
    // also, create all of scopes
    class ScopePreBuilder : Visitor
    {
        private ErrorHandle error_handle = new ErrorHandle();

        private Root root;

        private Stack<String> name_space_stack = new Stack<String>();

        private String name_space
        {
            get
            {
                Debug.Assert(name_space_stack.Count() > 0);
                return name_space_stack.First();
            }
        }

        public void enter_name_space(String name_space)
        {
            if (name_space_stack.Count() == 0)
                name_space_stack.Push(name_space);
            else name_space_stack.Push(name_space_stack.First() + "." + name_space);
        }

        public void pop_name_space()
        {
            name_space_stack.Pop();
        }

        public String name_in_name_space(String name)
        {
            if (name_space_stack.Count() == 0) return name;
            return name_space_stack.First() + "." + name;
        }

        public String join(IEnumerable<String> list, String name)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var x in list)
            {
                if (sb.Length > 0) sb.Append(".");
                sb.Append(x);
            }
            return sb.ToString() + (sb.Length > 0 ? "." : "") + name;
        }

        private int anonymous_type_count = 0;
        public String get_anonymous_type()
        {
            return name_in_name_space(String.Format("<>Anonymous_Type_{0}", anonymous_type_count++));
        }

        private int anonymous_closure_count = 0;
        public String get_closure_type()
        {
            return name_in_name_space(String.Format("<>Anonymous_Closure_{0}", anonymous_closure_count++));
        }

        public ScopePreBuilder(ErrorHandle error_handle)
        {
            this.error_handle = error_handle;
        }

        public override void visit(Root node)
        {
            root = node;

            root.scope = new GlobalScope();
            foreach (var item in node.module_block)
            {
                item.accept(this);
            }
        }

        public override void visit(ModuleDef node)
        {
            enter_name_space(node.name_space);

            foreach (var item in node.class_block)
            {
                item.accept(this);
                root.scope.try_push_class(item);
            }
            foreach (var item in node.data_block)
            {
                item.accept(this);
                root.scope.try_push_data(item);
            }
            foreach (var item in node.proxy_block)
            {
                item.accept(this);
            }
            foreach (var item in node.func_block)
            {
                item.accept(this);
                root.scope.try_push_func(item);
            }
            foreach (var item in node.module_block)
            {
                item.accept(this);
            }

            pop_name_space();
        }

        public override void visit(ClassDef node)
        {
            node.scope = new ClassScope();
            node.name = name_in_name_space(node.name);

            root.class_block.Add(node);

            foreach (var item in node.list)
            {
                item.accept(this);
                if (!node.scope.try_push_message_declare(item))
                {
                    // Todo
                    //error_handle.push(new DeclConflictError(item.pos, item.name, item.pos));
                    Debug.Assert(false);
                }
            }
        }

        public override void visit(Proxy node)
        {
            node.name_space = name_space;

            String target = node.target_name;
            if (target.LastIndexOf('.') != -1)
            {
                target = target.Substring(target.LastIndexOf('.') + 1);
            }
            String parent = node.parent_name;
            if (parent.LastIndexOf('.') != -1)
            {
                parent = parent.Substring(parent.LastIndexOf('.') + 1);
            }
            node.name = name_in_name_space(String.Format("<>{0}_{1}", target, parent));

            root.proxy_block.Add(node);

            foreach (var item in node.message_def)
            {
                item.accept(this);
            }
        }

        public override void visit(DataDef node)
        {
            node.scope = new DataScope();
            node.name = name_in_name_space(node.name);

            root.data_block.Add(node);

            foreach (var item in node.list)
            {
                item.accept(this);
                if (!node.scope.try_push_field(item))
                {
                    error_handle.push(new DeclConflictError(item.pos, item.name, node.scope.try_find_field(item.name).pos));
                }
            }
        }

        public override void visit(DataField node)
        {
        }

        public override void visit(LambdaClass node)
        {
        }

        public override void visit(MessageDeclare node)
        {
        }

        public override void visit(MessageDef node)
        {
            node.stmt_block.accept(this);
            node.name_space = name_space;
            node.scope = new FormalScope();
            node.scope.closure_scope.name = get_closure_type();
        }

        public override void visit(FuncDeclare node)
        {
        }

        public override void visit(FuncDef node)
        {
            node.declare.accept(this);
            node.scope = new FormalScope();
            node.scope.closure_scope.name = get_closure_type();
            node.name = name_in_name_space(node.name);
            node.name_space = name_space;

            root.func_block.Add(node);

            node.stmt_block.accept(this);
        }

        public override void visit(MatchCross node)
        {
        }

        public override void visit(MatchVar node)
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

        public override void visit(StmtBlock node)
        {
            foreach (var item in node.list)
            {
                item.accept(this);
            }
        }

        public override void visit(StmtAlloc node)
        {
            node.expr.accept(this);
        }

        public override void visit(StmtAssign node)
        {
            node.expr.accept(this);
        }

        public override void visit(StmtCall node)
        {
            foreach (var item in node.args)
            {
                item.accept(this);
            }
        }

        public override void visit(ExprCall node)
        {
            foreach (var item in node.args)
            {
                item.accept(this);
            }
        }

        public override void visit(StmtReturn node)
        {
            if (node.expr != null)
            {
                node.expr.accept(this);
            }
        }

        public override void visit(Const node)
        {
        }

        public override void visit(ExprBin node)
        {
            node.L.accept(this);
            node.R.accept(this);
        }

        public override void visit(ExprMessage node)
        {
            foreach (var item in node.args)
            {
                item.accept(this);
            }
        }

        public override void visit(ExprLambda node)
        {
            node.scope = new FormalScope();
            node.scope.closure_scope.name = get_closure_type();
            node.scope.ref_lambda = node;

            LambdaClass lambda_class = new LambdaClass();
            lambda_class.name = get_anonymous_type();
            lambda_class.lambda_expr = node;
            lambda_class.name_space = name_space;
            root.lambda_class_block.Add(lambda_class);
            node.lambda_class = lambda_class;

            node.stmt_block.accept(this);
        }

        public override void visit(ExprVar node)
        {
        }

        public override void visit(ExprNewData node)
        {
            foreach (var item in node.args)
            {
                item.accept(this);
            }
        }

        public override void visit(ExprAccess node)
        {
            node.expr.accept(this);
        }
    }
}
