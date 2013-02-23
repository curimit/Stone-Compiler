using Stone.Compiler.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stone.Compiler
{
    class ClassScope
    {
        private Dictionary<String, List<MessageDeclare>> message_declare = new Dictionary<String, List<MessageDeclare>>();

        public Boolean try_push_message_declare(MessageDeclare item)
        {
            if (!message_declare.ContainsKey(item.name)) message_declare[item.name] = new List<MessageDeclare>();
            message_declare[item.name].Add(item);
            return true;
        }

        public List<MessageDeclare> try_find_message_declare(String name)
        {
            if (message_declare.ContainsKey(name)) return message_declare[name];
            return null;
        }
    }
}
