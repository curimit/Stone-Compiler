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

        private FormalScope current_formal_scope;

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
        }

        public override void visit(MessageDeclare node)
        {
        }

        public override void visit(MessageDef node)
        {
            current_formal_scope = node.scope;

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
            current_formal_scope = node.scope;

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

        public override void visit(MatchAssignVar node)
        {
            node.symbol = scope_stack.try_find_var(node.name, node.pos);

            if (lambda_stack.Count() > 0)
            {
                closure_up_value(node.name);
            }

            if (node.symbol == null)
            {
                error_handle.push(new UndefinedVarError(node.pos, node.name));
            }
        }

        public override void visit(MatchAllocVar node)
        {
            if (!current_formal_scope.has_yield || lambda_stack.Count() > 0)
            {
                node.symbol = new LocalVar();
                node.symbol.info.name = node.name;
                node.symbol.info.pos = node.pos;
            }
            else
            {
                node.symbol = new ThisVar();
                node.symbol.info.name = node.name;
                node.symbol.info.pos = node.pos;
                current_formal_scope.yield_scope.this_var.Add((ThisVar)node.symbol);
            }

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

            if (lambda_stack.Count() > 0)
            {
                closure_up_value(node.owner);
            }

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
            if (node.expr != null)
            {
                node.expr.accept(this);
            }
        }

        public override void visit(StmtYield node)
        {
            node.expr.accept(this);
        }

        public override void visit(StmtIf node)
        {
            scope_stack.open(node.scope);

            node.condition.accept(this);
            node.if_true.accept(this);

            scope_stack.close();
        }

        public override void visit(StmtWhile node)
        {
            scope_stack.open(node.scope);

            node.condition.accept(this);
            node.body.accept(this);

            scope_stack.close();
        }

        public override void visit(StmtFor node)
        {
            scope_stack.open(node.scope);

            if (!current_formal_scope.has_yield)
            {
                node.symbol = new LocalVar();
                node.symbol.info.name = node.owner;
                node.symbol.info.pos = node.var_pos;

                node.iterator = new LocalVar();
            }
            else
            {
                node.symbol = new ThisVar();
                node.symbol.info.name = node.owner;
                node.symbol.info.pos = node.var_pos;
                current_formal_scope.yield_scope.this_var.Add((ThisVar)node.symbol);

                node.iterator = new ThisVar();
                node.iterator.info.name = "<>for_iterator_" + node.owner;
                current_formal_scope.yield_scope.this_var.Add((ThisVar)node.iterator);
            }

            if (!scope_stack.try_push_var(node.symbol))
            {
                Position earlier = scope_stack.try_find_var(node.symbol.info.name, node.pos).info.pos;
                error_handle.push(new DeclConflictError(node.pos, node.symbol.info.name, earlier));
            }

            node.expr.accept(this);
            node.body.accept(this);

            scope_stack.close();
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

            if (node.args != null)
            {
                node.args.accept(this);
            }
            node.stmt_block.accept(this);

            lambda_stack.Pop();
            scope_stack.close();
        }

        public override void visit(ExprArray node)
        {
            foreach (var item in node.values)
            {
                item.accept(this);
            }
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
                        closure_up_value(node.name);
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

        public void closure_up_value(String var_name)
        {
            Boolean ref_out = false;
            LocalScope local_scope = null;
            Stack<ExprLambda> expr_lambda_list = new Stack<ExprLambda>();
            VarSymbol var_symbol = null;
            foreach (var scope in scope_stack.stack)
            {
                if (scope.var.ContainsKey(var_name) && !(scope.var[var_name] is HeapVar))
                {
                    if (!ref_out) return;
                    local_scope = scope;
                    var_symbol = scope.var[var_name];
                    break;
                }
                if (scope.ref_lambda != null)
                {
                    ref_out = true;
                    expr_lambda_list.Push(scope.ref_lambda);
                }
            }
            Debug.Assert(local_scope != null);
            // in yield
            if (current_formal_scope.has_yield)
            {
                ThisVar this_var = new ThisVar();
                this_var.info.name = local_scope.closure_scope.name;
                this_var.ref_scope = local_scope.closure_scope;
                local_scope.closure_scope.anonymous_target = this_var;
                current_formal_scope.yield_scope.this_var.Add(this_var);
            }
            foreach (var expr_lambda in expr_lambda_list)
            {
                ObjectVar obj_var = null;
                // closure_value in this scope
                if (!local_scope.closure_scope.var_pushed.Contains(var_name))
                {
                    obj_var = new ObjectVar();
                    obj_var.info = var_symbol.info;
                    local_scope.var[var_name] = obj_var;

                    local_scope.closure_scope.var_pushed.Add(var_name);
                    local_scope.closure_scope.closure_var.Add(obj_var);
                }
                // up_value for lambda_expr
                if (!expr_lambda.lambda_class.up_var.Contains(var_name))
                {
                    expr_lambda.lambda_class.up_var.Add(var_name);
                    Debug.Assert(!expr_lambda.scope.var.ContainsKey(var_name));

                    HeapVar var = new HeapVar();
                    var.info = var_symbol.info;
                    var.ref_scope = local_scope;
                    var.ref_obj_var = obj_var;
                    expr_lambda.scope.var[var_name] = var;
                    expr_lambda.heap_vars.Add(var);
                }
                expr_lambda.ref_scopes.Add(new ExprLambda.RefScope { scope = local_scope });
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
