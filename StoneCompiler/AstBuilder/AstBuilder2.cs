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

        public Expr visit_expr(CommonTree T)
        {
            if (T is CommonErrorNode) return null;

            switch (T.Type)
            {
                case StoneParser.OP_PLUS:
                case StoneParser.OP_MINUS:
                case StoneParser.OP_MUL:
                case StoneParser.OP_DIV:
                    return visit_expr_bin(T);

                case StoneParser.Expr_Message:
                    return visit_expr_message(T);

                case StoneParser.Expr_Access:
                    return visit_expr_access(T);

                case StoneParser.Expr_Call:
                    return visit_expr_call(T);

                case StoneParser.Expr_New_Data:
                    return visit_expr_new_data(T);

                case StoneParser.LIDENT:
                    return visit_expr_var(T);
                    
                case StoneParser.INT:
                    return visit_const_int(T);
                case StoneParser.DOUBLE:
                    return visit_const_double(T);
                case StoneParser.NORMAL_STRING:
                    return visit_const_string(T);

                case StoneParser.Expr_Lambda:
                    return visit_expr_lambda(T);

                default:
                    Debug.Assert(false);
                    return null;
            }
        }

        #region const && var
        public Const visit_const_int(CommonTree T)
        {
            if (T is CommonErrorNode) return null;
            return new Const { const_type = Const.Int, Value_Int = int.Parse(T.Text), pos = get_pos(T) };
        }

        public Const visit_const_double(CommonTree T)
        {
            if (T is CommonErrorNode) return null;
            return new Const { const_type = Const.Double, Value_Double = double.Parse(T.Text), pos = get_pos(T) };
        }

        public Const visit_const_string(CommonTree T)
        {
            if (T is CommonErrorNode) return null;
            return new Const { const_type = Const.String, Value_String = T.Text.Substring(1, T.Text.Length - 2), pos = get_pos(T) };
        }

        public ExprVar visit_expr_var(CommonTree T)
        {
            if (T is CommonErrorNode) return null;
            return new ExprVar { name = T.Text, pos = get_pos(T) };
        }
        #endregion

        public ExprLambda visit_expr_lambda(CommonTree T)
        {
            if (T is CommonErrorNode) return null;

            ExprLambda lambda = new ExprLambda();
            lambda.pos = get_pos(T);

            if (T.GetChild(0).ChildCount == 1)
            {
                lambda.args = visit_match((T.GetChild(0) as CommonTree).GetChild(0) as CommonTree);
            }
            else
            {
                lambda.args = null;
                Debug.Assert(T.GetChild(0).ChildCount == 0);
            }

            lambda.ast_type = visit_type(T.GetChild(1) as CommonTree);

            lambda.stmt_block = visit_stmt_block(T.GetChild(2) as CommonTree);

            return lambda;
        }
        
        public ExprMessage visit_expr_message(CommonTree T)
        {
            if (T is CommonErrorNode) return null;

            Expr expr = visit_expr(T.GetChild(0) as CommonTree);
            expr.pos = get_pos(T);

            for (int i = 1; i < T.ChildCount; i++)
            {
                CommonTree node = T.Children[i] as CommonTree;
                ExprMessage tmp = new ExprMessage();
                tmp.pos = get_pos(node); // interesting

                tmp.owner = expr;
                tmp.name = node.GetChild(0).Text;
                Boolean success = true;
                foreach (CommonTree item in node.Children.Skip(1))
                {
                    Expr tmp_expr = visit_expr(item);
                    if (tmp_expr == null)
                    {
                        success = false;
                        break;
                    }
                    tmp.args.Add(tmp_expr);
                }
                if (!success) return null;
                expr = tmp;
                if (i == T.ChildCount - 1) return tmp;
            }
            Debug.Assert(false);
            return null;
        }

        public ExprNewData visit_expr_new_data(CommonTree T)
        {
            if (T is CommonErrorNode) return null;

            ExprNewData expr_data = new ExprNewData();
            expr_data.data_name = T.GetChild(0).Text;
            expr_data.pos = get_pos(T);

            Boolean success = true;
            foreach (CommonTree item in T.Children.Skip(1))
            {
                Expr tmp_expr = visit_expr(item);
                if (tmp_expr == null)
                {
                    success = false;
                    break;
                }
                expr_data.args.Add(tmp_expr);
            }
            if (!success) return null;
            return expr_data;
        }

        public ExprAccess visit_expr_access(CommonTree T)
        {
            if (T is CommonErrorNode) return null;

            ExprAccess expr = new ExprAccess();
            expr.pos = get_pos(T);

            expr.expr = visit_expr(T.GetChild(0) as CommonTree);
            if (expr.expr == null) return null;
            expr.name = T.GetChild(1).Text;

            return expr;
        }

        public ExprCall visit_expr_call(CommonTree T)
        {
            if (T is CommonErrorNode) return null;

            ExprCall expr = new ExprCall();
            expr.owner = T.GetChild(0).Text;
            expr.pos = get_pos(T);

            if (expr.owner == null) return null;
            foreach (CommonTree item in T.Children.Skip(1))
            {
                Expr tmp = visit_expr(item);
                if (tmp == null) return null; 
                expr.args.Add(tmp);
            }

            return expr;
        }

        public ExprBin visit_expr_bin(CommonTree T)
        {
            if (T is CommonErrorNode) return null;

            ExprBin bin = new ExprBin();
            bin.pos = get_pos(T);

            bin.Op = T.Type;
            bin.L = visit_expr(T.GetChild(0) as CommonTree);
            bin.R = visit_expr(T.GetChild(1) as CommonTree);
            return bin;
        }
    }
}
