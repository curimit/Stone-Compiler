using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Stone.Compiler.Node
{
    class Root : AstNode
    {
        public GlobalScope scope;

        public List<ModuleDef> module_block = new List<ModuleDef>();
        public List<LambdaClass> lambda_class_block = new List<LambdaClass>();

        // Todo
        public List<DataDef> data_block = new List<DataDef>();
        public List<ClassDef> class_block = new List<ClassDef>();
        public List<Proxy> proxy_block = new List<Proxy>();
        public List<FuncDef> func_block = new List<FuncDef>();

        // IL Info
        public AssemblyName assembly_name;
        public AssemblyBuilder assembly_builder;
        public ModuleBuilder module_builder;

        public MethodBuilder entry_method;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }
}
