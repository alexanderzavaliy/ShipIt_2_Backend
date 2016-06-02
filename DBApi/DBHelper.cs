using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBApi
{
    public class DBHelper
    {
        public static string AddEscaping(string str)
        {
            str = str.Replace("\"", "\"\"");
            return str;
        }
    }
}
