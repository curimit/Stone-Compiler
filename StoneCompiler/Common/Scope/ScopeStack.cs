using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Stone.Compiler.Node;

namespace Stone.Compiler
{
    class ScopeStack
    {
        public Stack<FormalScope> stack = new Stack<FormalScope>();

        public Boolean try_push_var(VarSymbol var)
        {
            if (stack.First().var.ContainsKey(var.info.name)) return false;
            stack.First().var[var.info.name] = var;
            return true;
        }

        public VarSymbol try_find_var(String name, Position pos)
        {
            foreach (var scope in stack)
                if (scope.var.ContainsKey(name) && scope.var[name].info.pos.stop_index < pos.start_index)
                    return scope.var[name];
            return null;
        }

        public void open(FormalScope scope)
        {
            stack.Push(scope);
        }

        public void close()
        {
            stack.Pop();
        }
    }
}
