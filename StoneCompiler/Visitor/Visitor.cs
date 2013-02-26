using Stone.Compiler.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stone.Compiler
{
    abstract class Visitor
    {
        public abstract void visit(Root node);

        public abstract void visit(ModuleDef node);

        public abstract void visit(ClassDef node);

        public abstract void visit(DataDef node);
        public abstract void visit(DataField node);

        public abstract void visit(Proxy node);

        public abstract void visit(LambdaClass node);

        public abstract void visit(MessageDeclare node);
        public abstract void visit(MessageDef node);

        public abstract void visit(FuncDeclare node);
        public abstract void visit(FuncDef node);

        public abstract void visit(AstFuncType node);
        public abstract void visit(AstCrossType node);
        public abstract void visit(AstAtomType node);

        public abstract void visit(MatchCross node);
        public abstract void visit(MatchVar var);

        public abstract void visit(StmtBlock node);
        public abstract void visit(StmtCall node);
        public abstract void visit(StmtAlloc node);
        public abstract void visit(StmtAssign node);
        public abstract void visit(StmtReturn node);

        public abstract void visit(StmtIf node);
        public abstract void visit(StmtWhile node);
        public abstract void visit(StmtFor node);

        public abstract void visit(Const node);
        public abstract void visit(ExprBin node);
        public abstract void visit(ExprVar node);
        public abstract void visit(ExprMessage node);
        public abstract void visit(ExprAccess node);
        public abstract void visit(ExprNewData node);
        public abstract void visit(ExprCall node);
        public abstract void visit(ExprArray node);

        public abstract void visit(ExprLambda node);
    }
}
