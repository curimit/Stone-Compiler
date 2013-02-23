using Stone.Compiler.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stone.Compiler
{
    class DataScope
    {
        private Dictionary<String, List<MessageDef>> members = new Dictionary<String, List<MessageDef>>();
        private Dictionary<String, DataField> fields = new Dictionary<String, DataField>();

        public Boolean try_push_member(MessageDef symbol)
        {
            if (!members.ContainsKey(symbol.name)) members[symbol.name] = new List<MessageDef>();
            members[symbol.name].Add(symbol);
            return true;
        }

        public List<MessageDef> try_find_member(String name)
        {
            if (members.ContainsKey(name)) return members[name];
            return null;
        }

        public DataField try_find_field(String name)
        {
            if (fields.ContainsKey(name)) return fields[name];
            return null;
        }

        public Boolean try_push_field(DataField item)
        {
            if (fields.ContainsKey(item.name)) return false;
            fields[item.name] = item;
            return true;
        }
    }
}
