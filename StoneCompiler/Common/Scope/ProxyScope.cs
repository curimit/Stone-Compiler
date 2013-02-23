using Stone.Compiler.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stone.Compiler
{
    class ProxyScope
    {
        private Dictionary<String, List<MessageDef>> message_def = new Dictionary<String, List<MessageDef>>();

        public Boolean try_push_message_def(MessageDef item)
        {
            if (!message_def.ContainsKey(item.name)) message_def[item.name] = new List<MessageDef>();
            message_def[item.name].Add(item);
            return true;
        }

        public List<MessageDef> try_find_message_def(String name)
        {
            if (message_def.ContainsKey(name)) return message_def[name];
            return null;
        }
    }
}
