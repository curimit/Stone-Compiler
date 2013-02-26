using Stone.Compiler.Node;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace Stone.Compiler
{
    abstract class StoneType
    {
        public static Boolean operator==(StoneType A, StoneType B)
        {
            return A.ToString() == B.ToString();
        }

        public static Boolean operator!=(StoneType A, StoneType B)
        {
            return A.ToString() != B.ToString();
        }

        public Boolean not_match(StoneType B)
        {
            if (this is BaseType && this.ToString() == "Error") return false;
            if (B is BaseType && B.ToString() == "Error") return false;
            return this.ToString() != B.ToString();
        }

        public int params_count()
        {
            Debug.Assert(this is FuncType);
            FuncType type = this as FuncType;
            if (type.args_type.ToString() == "Void") return 0;
            return type.args_type.get_types().Count();
        }

        public Type get_type()
        {
            if (this is BaseType)
            {
                String type_name = (this as BaseType).ToString();
                if (type_name == "Int") return typeof(int);
                if (type_name == "String") return typeof(string);
                if (type_name == "Double") return typeof(double);
                if (type_name == "Void") return typeof(void);
            }
            if (this is DataType)
            {
                Debug.Assert((this as DataType).data_def.type_builder != null);
                return (this as DataType).data_def.type_builder;
            }
            if (this is FuncType)
            {
                FuncType func = this as FuncType;

                Debug.Assert(func.return_type != BaseType.VOID);

                List<Type> list = new List<Type>();
                foreach (var item in func.args_type.get_types()) list.Add(item);
                list.Add(func.return_type.get_type());

                Type func_type = null;
                switch (list.Count)
                {
                    case 1:
                        func_type = typeof(Func<>);
                        break;

                    case 2:
                        func_type = typeof(Func<,>);
                        break;

                    case 3:
                        func_type = typeof(Func<,,>);
                        break;

                    default:
                        Debug.Assert(false);
                        break;
                }


                func_type = func_type.MakeGenericType(list.ToArray());

                return func_type;
            }
            if (this is ArrayType)
            {
                Type member_type = (this as ArrayType).member_type.get_type();
                return member_type.MakeArrayType();
            }
            Debug.Assert(false);
            return null;
        }

        public Type[] get_types()
        {
            if (this is CrossType)
            {
                CrossType cross_type = this as CrossType;
                List<Type> list = new List<Type>();
                foreach (var x in cross_type.list) list.Add(x.get_type());
                return list.ToArray();
            }
            return new Type[] { get_type() };
        }
    }

    class BaseType : StoneType
    {
        public static BaseType INT = new BaseType("Int");
        public static BaseType STRING = new BaseType("String");
        public static BaseType DOUBLE = new BaseType("Double");
        public static BaseType VOID = new BaseType("Void");

        public static BaseType ERROR = new BaseType("Error");

        public static BaseType from_string(String type_name)
        {
            if (type_name == "Int") return INT;
            if (type_name == "String") return STRING;
            if (type_name == "Double") return DOUBLE;
            if (type_name == "Void") return VOID;
            Debug.Assert(false);
            return null;
        }

        private String type_name;

        private BaseType(String type_name)
        {
            this.type_name = type_name;
        }

        public override string ToString()
        {
            return type_name;
        }
    }

    class DataType : StoneType
    {
        public String name;
        public DataDef data_def;

        public DataType(String name, DataDef data_def)
        {
            this.name = name;
            this.data_def = data_def;
        }

        public override string ToString()
        {
            return name;
        }
    }

    class ClassType : StoneType
    {
        public String name;

        public ClassType(String name)
        {
            this.name = name;
        }

        public override string ToString()
        {
            return "Class: " + name;
        }
    }

    class FuncType : StoneType
    {
        public StoneType args_type;
        public StoneType return_type;

        public FuncType(StoneType args_type, StoneType return_type)
        {
            this.args_type = args_type;
            this.return_type = return_type;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            sb.Append(args_type.ToString());
            sb.Append(" -> ");
            sb.Append(return_type.ToString());
            sb.Append(")");
            return sb.ToString();
        }
    }

    class CrossType : StoneType
    {
        public List<StoneType> list = new List<StoneType>();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in list)
            {
                if (sb.Length > 0) sb.Append(" × ");
                sb.Append(item.ToString());
            }
            return sb.ToString();
        }
    }

    class ArrayType : StoneType
    {
        public StoneType member_type;

        public ArrayType(StoneType member_type)
        {
            this.member_type = member_type;
        }

        public override string ToString()
        {
            return String.Format("[{0}]", member_type.ToString());
        }
    }
}
