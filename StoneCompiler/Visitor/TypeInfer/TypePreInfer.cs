using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stone.Compiler.Node;
using System.Diagnostics;

namespace Stone.Compiler
{
    // Infer function level type
    // data, function, message
    class TypePreInfer : Visitor
    {
        private ErrorHandle error_handle;
        private Root root;

        public TypePreInfer(ErrorHandle error_handle)
        {
            this.error_handle = error_handle;
        }

        public override void visit(Root node)
        {
            root = node;

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
            foreach (var item in node.list)
            {
                item.accept(this);
            }
        }

        public override void visit(Proxy node)
        {
            foreach (var item in node.message_def)
            {
                item.accept(this);
            }
        }

        public override void visit(DataDef node)
        {
            node.type = new DataType(node.name, node);

            foreach (var item in node.list)
            {
                item.accept(this);
            }
        }

        public override void visit(DataField node)
        {
            node.ast_type.accept(this);
            node.type = node.ast_type.type;
            node.type = node.ast_type.type;
        }

        public override void visit(LambdaClass node)
        {
            root.scope.name_space = node.name_space;

            node.lambda_expr.ast_type.accept(this);
            node.lambda_expr.type = node.lambda_expr.ast_type.type;

            Debug.Assert(node.lambda_expr.type is FuncType);

            FuncType type = node.lambda_expr.type as FuncType;

            if (node.lambda_expr.args != null)
            {
                infer_match(node.lambda_expr.args, type.args_type);
            }
        }

        public override void visit(MessageDeclare node)
        {
            node.ast_type.accept(this);
            node.type = node.ast_type.type;
        }

        public override void visit(MessageDef node)
        {
            root.scope.name_space = node.name_space;

            node.declare.accept(this);

            Debug.Assert(node.declare.type is FuncType);

            FuncType type = node.declare.type as FuncType;

            if (node.args != null)
            {
                infer_match(node.args, type.args_type);
            }
        }

        public override void visit(FuncDeclare node)
        {
            node.ast_type.accept(this);
            node.type = node.ast_type.type;
        }

        public override void visit(FuncDef node)
        {
            root.scope.name_space = node.name_space;

            node.declare.accept(this);

            Debug.Assert(node.declare.type is FuncType);

            FuncType type = node.declare.type as FuncType;

            if (node.args != null)
            {
                infer_match(node.args, type.args_type);
            }
        }

        private void infer_match(Match match, StoneType type)
        {
            if (type == BaseType.VOID)
            {
                Debug.Assert(match == null);
                return;
            }
            if (match is MatchCross)
            {
                infer_match(match as MatchCross, type as CrossType);
            }
            else if (match is MatchAssignVar)
            {
                infer_match(match as MatchAssignVar, type);
            }
            else if (match is MatchAllocVar)
            {
                infer_match(match as MatchAllocVar, type);
            }
        }

        private void infer_match(MatchCross cross, StoneType _type)
        {
            Debug.Assert(_type is CrossType);
            CrossType type = _type as CrossType;

            cross.type = type;
            for (int i = 0; i < cross.list.Count; i++)
            {
                infer_match(cross.list[i], type.list[i]);
            }
        }

        private void infer_match(MatchAssignVar var, StoneType type)
        {
            var.symbol.info.type = type;
            var.type = type;
        }

        private void infer_match(MatchAllocVar var, StoneType type)
        {
            var.symbol.info.type = type;
            var.type = type;
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
            node.Type1.accept(this);
            node.Type2.accept(this);
            node.type = new FuncType(node.Type1.type, node.Type2.type);
        }

        public override void visit(AstCrossType node)
        {
            var cross = new CrossType();
            foreach (var item in node.list)
            {
                item.accept(this);
                cross.list.Add(item.type);
            }
            node.type = cross;
        }

        public override void visit(AstAtomType node)
        {
            // if node is a atom type
            switch (node.type_name)
            {
                case "Int":
                    node.type = BaseType.INT;
                    return;

                case "Double":
                    node.type = BaseType.DOUBLE;
                    return;

                case "String":
                    node.type = BaseType.STRING;
                    return;

                case "Void":
                    node.type = BaseType.VOID;
                    return;
            }

            DataDef data = root.scope.try_find_data(node.type_name);
            Debug.Assert(data != null);
            node.type = data.type;
        }

        public override void visit(AstEnumType node)
        {
            node.member_type.accept(this);
            node.type = new EnumType(node.member_type.type);
        }

        public override void visit(StmtBlock node)
        {
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

        public override void visit(StmtReturn node)
        {
        }

        public override void visit(StmtYield node)
        {
        }

        public override void visit(StmtIf node)
        {
        }

        public override void visit(StmtWhile node)
        {
        }

        public override void visit(StmtFor node)
        {
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

        public override void visit(ExprLambda node)
        {
        }

        public override void visit(ExprArray node)
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
