using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stone.Compiler
{
    class Position
    {
        public int start_index, stop_index;

        // position by coordinate
        public int start_line, start_column;
        public int stop_line, stop_column;

        public Position(String code, int start_index, int stop_index)
        {
            this.start_index = start_index;
            this.stop_index = stop_index;

            GetPosition(code, start_index, out start_line, out start_column);
            GetPosition(code, stop_index, out stop_line, out stop_column);
        }

        public override string ToString()
        {
            return String.Format("({0},{1}) to ({2},{3})", start_line, start_column, stop_line, stop_column);
        }

        private void GetPosition(String code, int index, out int line, out int column)
        {
            line = 1; column = 1;
            for (int i = 0; i < index; i++)
            {
                if (code[i] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }
        }
    }
}
