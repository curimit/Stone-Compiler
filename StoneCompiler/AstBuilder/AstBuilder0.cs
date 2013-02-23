using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Stone.Compiler.Node;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using System.Diagnostics;

namespace Stone.Compiler
{
    partial class AstBuilder
    {
        private String code;
        private CommonTokenStream tokens;
        private ErrorHandle error_handle;

        private Position get_pos(CommonTree T)
        {
            if (!(T is CommonErrorNode))
            {
                CommonToken start = tokens.Get(T.TokenStartIndex) as CommonToken;
                CommonToken stop = tokens.Get(T.TokenStopIndex) as CommonToken;
                Position pos = new Position(code, start.StartIndex, stop.StopIndex);
                return pos;
            }
            else
            {
                CommonErrorNode TT = T as CommonErrorNode;
                Position pos = new Position(code, TT.start.StartIndex, TT.stop.StopIndex);
                return pos;
            }
        }

        private void issue_error(CommonTree T)
        {
            error_handle.push(new SyntaxError(get_pos(T)));
        }

        public Root visit(String input, ErrorHandle error_handle)
        {
            if (!input.EndsWith("\n")) input = input + "\n";
            ANTLRStringStream Input = new ANTLRStringStream(input);
            StoneLexer Lexer = new StoneLexer(Input);
            CommonTokenStream Tokens = new CommonTokenStream(Lexer);

            StoneParser Parser = new StoneParser(Tokens);
            var ParseReturn = Parser.parse();
            CommonTree Tree = (CommonTree)ParseReturn.Tree;

            //Program.visit(Tree);
            //for (int i = 0; i < Tokens.Count; i++)
            //{
            //    Console.WriteLine(Tokens.Get(i).ToString());
            //}

            this.code = input;
            this.tokens = Tokens;
            this.error_handle = error_handle;

            return visit_root(Tree);
        }

        // issue_error
        public Root visit_root(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            Root root = new Root();
            root.pos = get_pos(T);
            foreach (CommonTree item in T.Children)
            {
                if (item is CommonErrorNode)
                {
                    issue_error(item);
                    continue;
                }

                ModuleDef module_def = visit_module_def(item);
                if (module_def != null)
                {
                    root.module_block.Add(module_def);
                    module_def.root = root;
                }
            }
            return root;
        }

        // issue error
        public ModuleDef visit_module_def(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            ModuleDef module = new ModuleDef();
            module.pos = get_pos(T);
            module.name_space = T.GetChild(0).Text;
            foreach (CommonTree item in T.Children.Skip(1))
            {
                if (item is CommonErrorNode)
                {
                    issue_error(item);
                    continue;
                }

                switch (item.Type)
                {
                    case StoneParser.Class_Def:
                        {
                            ClassDef tmp = visit_class(item);
                            if (tmp != null) module.class_block.Add(tmp);
                            break;
                        }

                    case StoneParser.Data_Def:
                        {
                            DataDef tmp = visit_data(item);
                            if (tmp != null) module.data_block.Add(tmp);
                            break;
                        }

                    case StoneParser.Proxy_Def:
                        {
                            Proxy tmp = visit_proxy(item);
                            if (tmp != null) module.proxy_block.Add(tmp);
                            break;
                        }

                    case StoneParser.Func_Def:
                        {
                            FuncDef tmp = visit_func_def(item);
                            if (tmp != null) module.func_block.Add(tmp);
                            break;
                        }

                    case StoneParser.Module_Def:
                        {
                            ModuleDef module_def = visit_module_def(item);
                            if (module_def != null)
                            {
                                module.module_block.Add(module_def);
                                module_def.root = module.root;
                            }
                            break;
                        }

                    default:
                        Debug.Assert(false);
                        break;
                }
            }
            return module;
        }

        // issue error
        public ClassDef visit_class(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            ClassDef class_def = new ClassDef();
            class_def.pos = get_pos(T);
            class_def.name = T.GetChild(0).Text;
            foreach (CommonTree item in ((CommonTree)T.GetChild(1)).Children)
            {
                MessageDeclare tmp = visit_message_declare(item);
                if (tmp != null) class_def.list.Add(tmp);
            }
            return class_def;
        }

        public DataDef visit_data(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            DataDef data = new DataDef();
            data.pos = get_pos(T);
            data.name = T.GetChild(0).Text;

            // Error in Data Define
            if (T.GetChild(1) is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            foreach (CommonTree item in ((CommonTree)T.GetChild(1)).Children)
            {
                DataField tmp = visit_data_item(item);
                if (tmp != null) data.list.Add(tmp);
            }

            return data;
        }

        public Proxy visit_proxy(CommonTree T)
        {
            if (T is CommonErrorNode)
            {
                issue_error(T);
                return null;
            }

            Proxy proxy = new Proxy();
            proxy.pos = get_pos(T);
            proxy.target_name = T.GetChild(0).Text;
            proxy.parent_name = T.GetChild(1).Text;
            foreach (CommonTree item in ((CommonTree)T.GetChild(2)).Children)
            {
                if (item is CommonErrorNode)
                {
                    issue_error(item);
                    continue;
                }

                switch (item.Type)
                {
                    case StoneParser.Message_Def:
                        {
                            MessageDef tmp = visit_message_def(item);
                            if (tmp != null)
                            {
                                proxy.message_def.Add(tmp);
                                tmp.declare.defined_in = proxy;
                            }
                            break;
                        }

                    default:
                        Debug.Assert(false);
                        break;
                }
            }
            return proxy;
        }
    }
}
