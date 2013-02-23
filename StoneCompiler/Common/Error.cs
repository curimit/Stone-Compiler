using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stone.Compiler
{
    abstract class Error
    {
        public Position pos;
    }

    class ErrorHandle
    {
        private List<Error> list = new List<Error>();

        public void push(Error error)
        {
            list.Add(error);
        }

        public int Count()
        {
            return list.Count;
        }

        public void print()
        {
            Console.WriteLine("Total errors {0}", list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                Console.WriteLine("{0}: {1}", i + 1, list[i]);
            }
        }
    }

    class SyntaxError : Error
    {
        public SyntaxError(Position pos)
        {
            this.pos = pos;
        }

        public override string ToString()
        {
            return String.Format("{0}: Syntax Error", pos);
        }
    }

    class DeclConflictError : Error
    {
        private Position pos, earlier;
        private String name;

        public DeclConflictError(Position pos, String name, Position earlier)
        {
            this.pos = pos;
            this.name = name;
            this.earlier = earlier;
        }

        public override string ToString()
        {
            return String.Format("{0}: declaration of '{1}' here, conflicts with earlier declaration at {2}", pos, name, earlier);
        }
    }

    class UndefinedVarError : Error
    {
        private Position pos;
        private String name;

        public UndefinedVarError(Position pos, String name)
        {
            this.pos = pos;
            this.name = name;
        }

        public override string ToString()
        {
            return String.Format("{0}: undefined variable of '{1}'", pos, name);
        }
    }

    class OperatorNotImplementError : Error
    {
        private String type_left;
        private String type_right;
        private String op;

        public OperatorNotImplementError(String type_left, String op, String type_right)
        {
            this.type_left = type_left;
            this.type_right = type_right;
            this.op = op;
        }

        public override string ToString()
        {
            return String.Format("{0}: operator '{1}' not implement {2} {1} {3}", pos, op, type_left, type_right);
        }
    }

    class AssignTypeMissMatchError : Error
    {
        private String type_left;
        private String type_right;

        public AssignTypeMissMatchError(String type_left, String type_right)
        {
            this.type_left = type_left;
            this.type_right = type_right;
        }

        public override string ToString()
        {
            return String.Format("{0}: You can't put {1} into {2}", pos, type_right, type_left);
        }
    }
}
