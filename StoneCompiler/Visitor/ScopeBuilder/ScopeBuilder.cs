using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stone.Compiler.Node;
using System.Diagnostics;

namespace Stone.Compiler
{
    class ScopeBuilder : Visitor
    {
        public ScopeStack scope_stack = new ScopeStack();
        public ErrorHandle error_handle = new ErrorHandle();

        private Root root;
        private Stack<ExprLambda> lambda_stack = new Stack<ExprLambda>();

        public ScopeBuilder(ErrorHandle error_handle)
        {
            this.error_handle = error_handle;
        }

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
        }

        public override void visit(ModuleDef node)
        {
        }

        public override void visit(ClassDef node)
        {
        }

        public override void visit(Proxy node)
        {
            root.scope.name_space = node.name_space;

            node.target = root.scope.try_find_data(node.target_name);
            node.base_class = root.scope.try_find_class(node.parent_name);

            Debug.Assert(node.target != null);
            Debug.Assert(node.base_class != null);

            foreach (var item in node.message_def)
            {
                item.defined_in = node;
                item.accept(this);
                if (!node.target.scope.try_push_member(item))
                {
                    //error_handle.push(new DeclConflictError(item.pos, item.name, item.pos));
                    Debug.Assert(false);
                }
            }
        }

        public override void visit(DataDef node)
        {
        }

        public override void visit(DataField node)
        {
        }

        public override void visit(LambdaClass node)
        {
            /*scope_stack.open(node.lambda_expr.scope);
            scope_stack.enter_name_space(node.name_space);

            scope_stack.pop_name_space();
            scope_stack.close();*/
        }

        public override void visit(MessageDeclare node)
        {
        }

        public override void visit(MessageDef node)
        {
            node.declare.accept(this);

            scope_stack.open(node.scope);
            root.scope.name_space = node.name_space;

            if (node.args != null)
            {
                node.args.accept(this);
            }

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

            if (node.args != null)
            {
                node.args.accept(this);
            }

            node.stmt_block.accept(this);

            scope_stack.close();
        }

        public override void visit(MatchCross node)
        {
            foreach (var item in node.list)
            {
                item.accept(this);
            }
        }

        public override void visit(MatchVar node)
        {
            node.symbol = new LocalVar();
            node.symbol.info.name = node.name;
            node.symbol.info.pos = node.pos;
            if (!scope_stack.try_push_var(node.symbol))
            {
                Position earlier = scope_stack.try_find_var(node.symbol.info.name, node.pos).info.pos;
                error_handle.push(new DeclConflictError(node.pos, node.symbol.info.name, earlier));
            }
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

            node.symbol = new LocalVar();
            node.symbol.info.name = node.owner;
            node.symbol.info.pos = node.pos;

            if (!scope_stack.try_push_var(node.symbol))
            {
                Position earlier = scope_stack.try_find_var(node.symbol.info.name, node.pos).info.pos;
                error_handle.push(new DeclConflictError(node.pos, node.symbol.info.name, earlier));
            }
        }

        public override void visit(StmtAssign node)
        {
            node.expr.accept(this);

            node.symbol = scope_stack.try_find_var(node.owner, node.pos);

            if (node.symbol == null)
            {
                error_handle.push(new UndefinedVarError(node.pos, node.owner));
            }
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
            node.expr.accept(this);
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
            node.owner.accept(this);
            foreach (var item in node.args)
            {
                item.accept(this);
            }
        }

        public override void visit(ExprLambda node)
        {
            scope_stack.open(node.scope);
            lambda_stack.Push(node);

            node.args.accept(this);
            node.stmt_block.accept(this);

            lambda_stack.Pop();
            scope_stack.close();
        }

        public override void visit(ExprVar node)
        {
            if (root.scope.try_find_func(node.name) == null)
            {
                node.is_func = false;
                if (scope_stack.try_find_var(node.name, node.pos) == null)
                {
                    error_handle.push(new UndefinedVarError(node.pos, node.name));
                }
                else
                {
                    if (lambda_stack.Count() > 0)
                    {
                        closure_up_value(node);
                    }
                }
            }
            else
            {
                node.is_func = true;
                if (root.scope.try_find_func(node.name) == null)
                {
                    error_handle.push(new UndefinedVarError(node.pos, node.name));
                }
            }
        }

        public void closure_up_value(ExprVar node)
        {
            Boolean ref_out = false;
            FormalScope formal_scope = null;
            Stack<ExprLambda> expr_lambda_list = new Stack<ExprLambda>();
            VarSymbol var_symbol = null;
            foreach (var scope in scope_stack.stack)
            {
                if (scope.var.ContainsKey(node.name) && !(scope.var[node.name] is ThisVar))
                {
                    if (!ref_out) return;
                    formal_scope = scope;
                    var_symbol = scope.var[node.name];
                    break;
                }
                if (scope.ref_lambda != null)
                {
                    ref_out = true;
                    expr_lambda_list.Push(scope.ref_lambda);
                }
            }
            Debug.Assert(formal_scope != null);
            foreach (var expr_lambda in expr_lambda_list)
            {
                // up_value for lambda_expr
                if (!expr_lambda.lambda_class.up_var.Contains(node.name))
                {
                    expr_lambda.lambda_class.up_var.Add(node.name);
                    Debug.Assert(!expr_lambda.scope.var.ContainsKey(node.name));

                    ThisVar var = new ThisVar();
                    var.info = var_symbol.info;
                    var.ref_scope = formal_scope;
                    expr_lambda.scope.var[node.name] = var;
                }
                // closure_value in this scope
                if (!formal_scope.closure_scope.closure_var.ContainsKey(node.name))
                {
                    formal_scope.closure_scope.closure_var[node.name] = null;
                    ObjectVar var = new ObjectVar();
                    var.info = var_symbol.info;
                    formal_scope.var[node.name] = var;
                }
                expr_lambda.ref_scopes.Add(new ExprLambda.RefScope { scope = formal_scope });
            }
        }

        public override void visit(ExprNewData node)
        {
        }

        public override void visit(ExprAccess node)
        {
        }
    }
}
