using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace Stone.Compiler.Node
{
    class MessageDeclare : AstNode
    {
        public String name;
        public AstType ast_type;

        public StoneType type;

        public Proxy defined_in;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class MessageDef : AstNode
    {
        public String name;
        public MessageDeclare declare;
        public StmtBlock stmt_block = new StmtBlock();

        public String name_space;

        public Match args;

        public FormalScope scope;

        public Proxy defined_in;

        // IL Info
        public MethodBuilder method_builder;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class FuncDeclare : AstNode
    {
        public String name;
        public AstType ast_type;

        public StoneType type;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }

    class FuncDef : AstNode
    {
        public String name;
        public FuncDeclare declare;
        public StmtBlock stmt_block = new StmtBlock();

        public String name_space;

        public Match args;

        public FormalScope scope;

        // IL Info
        public MethodBuilder method_builder;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }
}
