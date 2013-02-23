using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;

namespace Stone.Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            //ILCompiler.test();
            //return;

            String input = File.ReadAllText("../code/case5.txt");

            ErrorHandle error_handle = new ErrorHandle();

            AstBuilder ast = new AstBuilder();
            var root = ast.visit(input, error_handle);

            if (error_handle.Count() > 0)
            {
                error_handle.print();
                return;
            }

            ScopePreBuilder pre_scope = new ScopePreBuilder(error_handle);
            pre_scope.visit(root);

            if (error_handle.Count() > 0)
            {
                error_handle.print();
                return;
            }

            ScopeBuilder scope = new ScopeBuilder(error_handle);
            scope.visit(root);

            if (error_handle.Count() > 0)
            {
                error_handle.print();
                return;
            }

            TypePreInfer pre_infer = new TypePreInfer(error_handle);
            pre_infer.visit(root);

            if (error_handle.Count() > 0)
            {
                error_handle.print();
                return;
            }

            TypeInfer infer = new TypeInfer(error_handle);
            infer.visit(root);

            if (error_handle.Count() > 0)
            {
                error_handle.print();
                return;
            }

            ILPreBuilderData pre_builder_data = new ILPreBuilderData();
            pre_builder_data.visit(root);

            ILCompiler compiler = new ILCompiler();
            compiler.visit(root);
        }

        public static void visit(CommonTree Tree, int tab = 0)
        {
            for (int i = 0; i < tab; i++) Console.Write("    ");
            if (Tree is CommonErrorNode)
                Console.WriteLine("Error: {0}", Tree);
            else Console.WriteLine(Tree);
            if (Tree.Children == null) return;
            foreach (var subTree in Tree.Children) visit(subTree as CommonTree, tab + 1);
        }
    }
}
