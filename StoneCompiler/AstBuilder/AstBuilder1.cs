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
        public MessageDeclare visit_message_declare(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            MessageDeclare message_declare = new MessageDeclare();
            message_declare.pos = get_pos(T);
            message_declare.name = T.GetChild(0).Text;
            message_declare.ast_type = visit_type(T.GetChild(1) as CommonTree);
            if (message_declare.ast_type == null)
            {
                issue_error(T);
                return null;
            }
            return message_declare;
        }

        // issue error
        public MessageDef visit_message_def(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            MessageDef message_def = new MessageDef();
            message_def.pos = get_pos(T);

            message_def.name = T.GetChild(0).Text;
            message_def.declare = visit_message_declare(T.GetChild(1) as CommonTree);
            if (message_def.declare == null) return null;

            if (T.GetChild(2).ChildCount == 1)
            {
                message_def.args = visit_match((T.GetChild(2) as CommonTree).GetChild(0) as CommonTree);

                if (message_def.args == null)
                {
                    issue_error(T);
                    return null;
                }
            }
            else
            {
                Debug.Assert(T.GetChild(2).ChildCount == 0);
                message_def.args = null;
            }

            message_def.stmt_block = visit_stmt_block(T.GetChild(3) as CommonTree);
            if (message_def.stmt_block == null) return null;

            return message_def;
        }

        // can issue error
        public FuncDeclare visit_func_declare(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            FuncDeclare func_declare = new FuncDeclare();
            func_declare.pos = get_pos(T);
            func_declare.name = T.GetChild(0).Text;
            func_declare.ast_type = visit_type(T.GetChild(1) as CommonTree);
            if (func_declare.ast_type == null)
            {
                issue_error(T);
                return null;
            }
            return func_declare;
        }

        public AstType visit_type(CommonTree T)
        {
            if (T is CommonErrorNode) return null;

            switch (T.Type)
            {
                case StoneParser.Type_Func:
                    return visit_type_func(T);

                case StoneParser.Type_Cross:
                    return visit_type_cross(T);

                case StoneParser.Type_Atom:
                    return visit_type_atom(T);

                case StoneParser.Type_Enum:
                    return visit_type_enum(T);

                default:
                    Debug.Assert(false);
                    break;
            }
            return null;
        }

        public AstType visit_type_func(CommonTree T)
        {
            if (T is CommonErrorNode) return null;

            return new AstFuncType
            {
                Type1 = visit_type(T.GetChild(0) as CommonTree),
                Type2 = visit_type(T.GetChild(1) as CommonTree),
                pos = get_pos(T)
            };
        }

        public AstType visit_type_cross(CommonTree T)
        {
            if (T is CommonErrorNode) return null;

            var cross = new AstCrossType();
            cross.pos = get_pos(T);
            foreach (CommonTree item in T.Children)
            {
                cross.list.Add(visit_type(item));
            }
            return cross;
        }

        public AstType visit_type_atom(CommonTree T)
        {
            if (T is CommonErrorNode) return null;

            return new AstAtomType { type_name = T.GetChild(0).Text, pos = get_pos(T) };
        }

        public AstType visit_type_enum(CommonTree T)
        {
            if (T is CommonErrorNode) return null;

            AstEnumType type = new AstEnumType();
            type.member_type = visit_type(T.Children[0] as CommonTree);
            if (type.member_type == null) return null;

            return type;
        }

        // issue error
        public FuncDef visit_func_def(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            FuncDef func_def = new FuncDef();
            func_def.pos = get_pos(T);

            func_def.name = T.GetChild(0).Text;
            func_def.declare = visit_func_declare(T.GetChild(1) as CommonTree);
            if (func_def.declare == null) return null;

            if (T.GetChild(2).ChildCount == 1)
            {
                func_def.args = visit_match((T.GetChild(2) as CommonTree).GetChild(0) as CommonTree);

                if (func_def.args == null)
                {
                    issue_error(T);
                    return null;
                }
            }
            else
            {
                Debug.Assert(T.GetChild(2).ChildCount == 0);
                func_def.args = null;
            }

            func_def.stmt_block = visit_stmt_block(T.GetChild(3) as CommonTree);
            if (func_def.stmt_block == null) return null;
            
            return func_def;
        }

        public Match visit_match(CommonTree T)
        {
            if (T is CommonErrorNode) return null;

            switch (T.Type)
            {
                case StoneParser.Match_Cross:
                    return visit_match_cross(T);

                case StoneParser.Match_Assign_Var:
                    return visit_match_assign_var(T);

                case StoneParser.Match_Alloc_Var:
                    return visit_match_alloc_var(T);
            }
            Debug.Assert(false);
            return null;
        }

        public Match visit_match_cross(CommonTree T)
        {
            if (T is CommonErrorNode) return null;

            MatchCross cross = new MatchCross();
            cross.pos = get_pos(T);
            foreach (CommonTree item in T.Children)
            {
                cross.list.Add(visit_match(item));
            }
            return cross;
        }

        public Match visit_match_assign_var(CommonTree T)
        {
            if (T is CommonErrorNode) return null;

            return new MatchAssignVar { name = T.GetChild(0).Text, pos = get_pos(T) };
        }

        public Match visit_match_alloc_var(CommonTree T)
        {
            if (T is CommonErrorNode) return null;

            return new MatchAllocVar { name = T.GetChild(0).Text, pos = get_pos(T) };
        }

        // issue error
        public StmtBlock visit_stmt_block(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }
            
            StmtBlock block = new StmtBlock();
            block.pos = get_pos(T);

            foreach (CommonTree item in T.Children)
            {
                if (item is CommonErrorNode)
                {
                    issue_error(item);
                    continue;
                }

                Stmt stmt;
                switch (item.Type)
                {
                    case StoneParser.Stmt_Return:
                        stmt = visit_stmt_return(item);
                        if (stmt != null) block.list.Add(stmt);
                        break;

                    case StoneParser.Stmt_Alloc:
                        stmt = visit_stmt_alloc(item);
                        if (stmt != null) block.list.Add(stmt);
                        break;

                    case StoneParser.Stmt_Assign:
                        stmt = visit_stmt_assign(item);
                        if (stmt != null) block.list.Add(stmt);
                        break;

                    case StoneParser.Stmt_Call:
                        stmt = visit_stmt_call(item);
                        if (stmt != null) block.list.Add(stmt);
                        break;

                    case StoneParser.Stmt_If:
                        stmt = visit_stmt_if(item);
                        if (stmt != null) block.list.Add(stmt);
                        break;

                    case StoneParser.Stmt_While:
                        stmt = visit_stmt_while(item);
                        if (stmt != null) block.list.Add(stmt);
                        break;

                    case StoneParser.Stmt_For:
                        stmt = visit_stmt_for(item);
                        if (stmt != null) block.list.Add(stmt);
                        break;

                    case StoneParser.Stmt_Yield:
                        stmt = visit_stmt_yield(item);
                        if (stmt != null) block.list.Add(stmt);
                        break;

                    default:
                        Debug.Assert(false);
                        break;
                }
            }

            return block;
        }

        // issue error
        public DataField visit_data_item(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            AstType ast_type = visit_type(T.GetChild(1) as CommonTree);

            return new DataField { name = T.GetChild(0).Text, ast_type = ast_type, pos = get_pos(T) };
        }

    }
}
