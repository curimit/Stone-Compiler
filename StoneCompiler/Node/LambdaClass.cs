using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace Stone.Compiler.Node
{
    class LambdaClass : AstNode
    {
        public String name;
        public ExprLambda lambda_expr;

        public String name_space;

        public HashSet<String> up_var = new HashSet<String>();

        // generated anonymous class
        public TypeBuilder type_builder;
        // mainly lambda function
        public MethodBuilder method_builder;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }
}
