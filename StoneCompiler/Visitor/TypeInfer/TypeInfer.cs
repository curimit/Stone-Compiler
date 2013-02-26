using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stone.Compiler.Node;
using System.Diagnostics;

namespace Stone.Compiler
{
    // Infer symbol's type in all scopes
    class TypeInfer : Visitor
    {
        private ScopeStack scope_stack = new ScopeStack();
        private ErrorHandle error_handle;
        private Root root;

        public TypeInfer(ErrorHandle error_handle)
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
            foreach (var item in node.message_def)
            {
                item.accept(this);
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
            root.scope.name_space = node.name_space;

            scope_stack.open(node.lambda_expr.scope);

            node.lambda_expr.stmt_block.accept(this);

            scope_stack.close();
        }

        public override void visit(MessageDeclare node)
        {
        }

        public override void visit(MessageDef node)
        {
            root.scope.name_space = node.name_space;

            scope_stack.open(node.scope);

            node.stmt_block.accept(this);

            scope_stack.close();
        }

        public override void visit(FuncDeclare node)
        {
        }

        public override void visit(FuncDef node)
        {
            root.scope.name_space = node.name_space;

            scope_stack.open(node.scope);

            node.stmt_block.accept(this);

            scope_stack.close();
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
            node.symbol.info.type = node.expr.type;
        }

        public override void visit(StmtAssign node)
        {
            node.expr.accept(this);
            // Type Check
            if (node.symbol.info.type.not_match(node.expr.type))
            {
                error_handle.push(new AssignTypeMissMatchError(node.pos, node.symbol.info.type.ToString(), node.expr.type.ToString()));
            }
        }

        public override void visit(StmtCall node)
        {
            foreach (var item in node.args)
            {
                item.accept(this);
            }
            List <FuncDef> func_list = root.scope.try_find_func(node.owner);

            // Maybe system function
            if (func_list == null)
            {
                if (node.owner == "print") return;
            }

            Debug.Assert(func_list.Count == 1);
            FuncDef func = func_list.First();
            Debug.Assert(func.declare.type is FuncType);
            node.type = (func.declare.type as FuncType).return_type;
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

            node.expr.accept(this);

            if (node.expr.type == BaseType.ERROR)
            {
                node.symbol.info.type = BaseType.ERROR;
                return;
            }

            if (!(node.expr.type is ArrayType))
            {
                error_handle.push(new NotEnumerableError(node.expr.pos, node.expr.type));
                node.symbol.info.type = BaseType.ERROR;
                return;
            }

            ArrayType array_type = node.expr.type as ArrayType;
            node.symbol.info.type = array_type.member_type;

            node.body.accept(this);

            scope_stack.close();
        }

        public override void visit(ExprCall node)
        {
            foreach (var item in node.args)
            {
                item.accept(this);
            }
            // try if node.owner is a var but type with function
            VarSymbol var = scope_stack.try_find_var(node.owner, node.pos);
            if (var != null && var.info.type is FuncType)
            {
                node.type = (var.info.type as FuncType).return_type;
            }
            else
            {
                List<FuncDef> func_list = root.scope.try_find_func(node.owner);
                Debug.Assert(func_list.Count == 1);
                FuncDef func = func_list.First();
                Debug.Assert(func.declare.type is FuncType);
                node.type = (func.declare.type as FuncType).return_type;
            }
        }

        public override void visit(StmtReturn node)
        {
            node.expr.accept(this);
        }

        public override void visit(Const node)
        {
            switch (node.const_type)
            {
                case Const.Int:
                    node.type = BaseType.INT;
                    break;

                case Const.Double:
                    node.type = BaseType.DOUBLE;
                    break;

                case Const.String:
                    node.type = BaseType.STRING;
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
                case StoneParser.OP_MINUS:
                    if (node.L.type == node.R.type)
                    {
                        node.type = node.L.type;
                    }
                    else
                    {
                        error_handle.push(new OperatorNotImplementError(node.L.type.ToString(), node.get_op(), node.R.type.ToString()));
                        node.type = BaseType.ERROR;
                        //Debug.Assert(false);
                    }
                    break;

                case StoneParser.OP_MUL:
                case StoneParser.OP_DIV:
                    if (node.L.type == node.R.type && (node.L.type == BaseType.INT || node.R.type == BaseType.DOUBLE))
                    {
                        node.type = node.L.type;
                    }
                    else
                    {
                        error_handle.push(new OperatorNotImplementError(node.L.type.ToString(), node.get_op(), node.R.type.ToString()));
                        node.type = BaseType.ERROR;
                        //Debug.Assert(false);
                    }
                    break;
            }
        }

        public override void visit(ExprMessage node)
        {
            node.owner.accept(this);

            foreach (var item in node.args)
            {
                item.accept(this);
            }

            // find message
            DataDef data = root.scope.try_find_data(node.owner.type.ToString());
            Debug.Assert(data != null);

            List<MessageDef> list = data.scope.try_find_member(node.name);
            Debug.Assert(list != null);
            Debug.Assert(list.Count == 1);
            Debug.Assert(list.First().declare.type is FuncType);
            node.message = list.First();
            node.type = (list.First().declare.type as FuncType).return_type;
        }

        public override void visit(ExprLambda node)
        {
            node.ast_type.accept(this);

            node.type = node.ast_type.type;
        }

        public override void visit(ExprArray node)
        {
            foreach (var item in node.values)
            {
                item.accept(this);
            }

            // types in array must be exactly same
            if (node.values.Exists(x => x.type != node.values.First().type))
            {
                error_handle.push(new ArrayExprTypeMissMatchError(node.pos));
                node.type = BaseType.ERROR;
                return;
            }

            node.type = new ArrayType(node.values.First().type);
        }

        public override void visit(ExprVar node)
        {
            if (!node.is_func)
            {
                VarSymbol var = scope_stack.try_find_var(node.name, node.pos);
                Debug.Assert(var != null);
                node.type = var.info.type;
            }
            else
            {
                List<FuncDef> func_list = root.scope.try_find_func(node.name);
                Debug.Assert(func_list.Count == 1);
                FuncDef func = func_list.First();
                node.type = func.declare.type;
            }
        }

        public override void visit(ExprNewData node)
        {
            DataDef data = root.scope.try_find_data(node.data_name);
            Debug.Assert(data != null);

            node.type = data.type;
        }

        public override void visit(ExprAccess node)
        {
            node.expr.accept(this);
            Debug.Assert(node.expr.type is DataType);
            DataScope scope = (node.expr.type as DataType).data_def.scope;
            DataField field = scope.try_find_field(node.name);
            Debug.Assert(field != null);
            node.type = field.type;
        }
    }
}
