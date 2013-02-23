using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stone.Compiler.Node
{
    class ModuleDef : AstNode
    {
        public String name_space;

        public List<DataDef> data_block = new List<DataDef>();
        public List<ClassDef> class_block = new List<ClassDef>();
        public List<Proxy> proxy_block = new List<Proxy>();
        public List<FuncDef> func_block = new List<FuncDef>();
        public List<ModuleDef> module_block = new List<ModuleDef>();

        public Root root;

        public override void accept(Visitor visitor)
        {
            visitor.visit(this);
        }
    }
}
