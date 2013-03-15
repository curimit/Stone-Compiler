using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Stone.Compiler.Node;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using System.Diagnostics;

namespace Stone.Compiler
{
    partial class AstBuilder
    {
        // can issue error
        public StmtReturn visit_stmt_return(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            StmtReturn stmt_return = new StmtReturn();
            stmt_return.pos = get_pos(T);

            if (T.ChildCount == 1)
            {
                stmt_return.expr = visit_expr(T.GetChild(0) as CommonTree);
                if (stmt_return.expr == null)
                {
                    issue_error(T);
                    return null;
                }
            }
            else stmt_return.expr = null;

            return stmt_return;
        }

        // can issue error
        public StmtYield visit_stmt_yield(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            StmtYield stmt_yield = new StmtYield();
            stmt_yield.pos = get_pos(T);

            stmt_yield.expr = visit_expr(T.GetChild(0) as CommonTree);
            if (stmt_yield.expr == null)
            {
                issue_error(T);
                return null;
            }

            return stmt_yield;
        }

        // can issue error
        public StmtIf visit_stmt_if(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            StmtIf stmt_if = new StmtIf();
            stmt_if.pos = get_pos(T);

            stmt_if.condition = visit_expr(T.GetChild(0) as CommonTree);
            if (stmt_if.condition == null) return null;

            stmt_if.if_true = visit_stmt_block(T.GetChild(1) as CommonTree);
            if (stmt_if.if_true == null) return null;

            return stmt_if;
        }
        
        // can issue error
        public StmtWhile visit_stmt_while(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            StmtWhile stmt_while = new StmtWhile();
            stmt_while.pos = get_pos(T);

            stmt_while.condition = visit_expr(T.GetChild(0) as CommonTree);
            if (stmt_while.condition == null) return null;

            stmt_while.body = visit_stmt_block(T.GetChild(1) as CommonTree);
            if (stmt_while.body == null) return null;

            return stmt_while;
        }

        // can issue error
        public StmtFor visit_stmt_for(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            StmtFor stmt_for = new StmtFor();
            stmt_for.pos = get_pos(T);
            stmt_for.var_pos = get_pos(T.Children[0] as CommonTree);

            stmt_for.owner = T.GetChild(0).Text;

            stmt_for.expr = visit_expr(T.Children[1] as CommonTree);
            if (stmt_for.expr == null) return null;

            stmt_for.body = visit_stmt_block(T.Children[2] as CommonTree);
            if (stmt_for.body == null) return null;

            return stmt_for;
        }

        // can issue error
        public StmtAlloc visit_stmt_alloc(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            StmtAlloc stmt_alloc = new StmtAlloc();
            stmt_alloc.pos = get_pos(T);

            stmt_alloc.owner = T.GetChild(0).Text;
            stmt_alloc.expr = visit_expr(T.GetChild(1) as CommonTree);
            if (stmt_alloc.expr == null)
            {
                issue_error(T);
                return null;
            }
            return stmt_alloc;
        }

        // can issue error
        public StmtAssign visit_stmt_assign(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            StmtAssign stmt_assign = new StmtAssign();
            stmt_assign.pos = get_pos(T);

            stmt_assign.owner = T.GetChild(0).Text;
            stmt_assign.expr = visit_expr(T.GetChild(1) as CommonTree);
            if (stmt_assign.expr == null)
            {
                issue_error(T);
                return null;
            }
            return stmt_assign;
        }

        // can issue error
        public StmtCall visit_stmt_call(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            StmtCall stmt_call = new StmtCall();
            stmt_call.pos = get_pos(T);

            stmt_call.owner = T.GetChild(0).Text;
            foreach (CommonTree item in T.Children.Skip(1))
            {
                Expr expr = visit_expr(item);
                if (expr == null)
                {
                    issue_error(T);
                    return null;
                }
                stmt_call.args.Add(expr);
            }
            return stmt_call;
        }
    }
}
