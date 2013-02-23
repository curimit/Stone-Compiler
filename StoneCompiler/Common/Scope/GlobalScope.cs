using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using Stone.Compiler.Node;

namespace Stone.Compiler
{
    class GlobalScope
    {
        public String name_space;

        private Dictionary<String, List<FuncDef>> func = new Dictionary<String, List<FuncDef>>();
        private Dictionary<String, DataDef> data = new Dictionary<String, DataDef>();
        private Dictionary<String, ClassDef> class_list = new Dictionary<String, ClassDef>();

        public Boolean try_push_data(DataDef item)
        {
            String name = item.name;
            if (data.ContainsKey(name)) return false;
            data[name] = item;
            return true;
        }

        public DataDef try_find_data(String name)
        {
            // find in global
            String[] list = name_space.Split('.');
            for (int i = list.Length; i >= 0; i--)
            {
                String tmp = join(list.Take(i), name);
                if (data.ContainsKey(tmp)) return data[tmp];
            }
            return null;
        }

        public Boolean try_push_class(ClassDef item)
        {
            // push to global
            String name = item.name;
            if (class_list.ContainsKey(name)) return false;
            class_list[name] = item;
            return true;
        }

        public ClassDef try_find_class(String name)
        {
            // find in global
            String[] list = name_space.Split('.');
            for (int i = list.Length; i >= 0; i--)
            {
                String tmp = join(list.Take(i), name);
                if (class_list.ContainsKey(tmp)) return class_list[tmp];
            }
            return null;
        }

        // Todo: polymorphic, redefine detective
        public Boolean try_push_func(FuncDef item)
        {
            // push to global
            String name = item.name;
            if (!func.ContainsKey(name))
            {
                func[name] = new List<FuncDef>();
            }
            func[name].Add(item);

            return true;
        }

        public List<FuncDef> try_find_func(String name)
        {
            // find in global
            String[] list = name_space.Split('.');
            for (int i = list.Length; i >= 0; i--)
            {
                String tmp = join(list.Take(i), name);
                if (func.ContainsKey(tmp)) return func[tmp];
            }
            return null;
        }

        public String join(IEnumerable<String> list, String name)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var x in list)
            {
                if (sb.Length > 0) sb.Append(".");
                sb.Append(x);
            }
            return sb.ToString() + (sb.Length > 0 ? "." : "") + name;
        }
    }

}
