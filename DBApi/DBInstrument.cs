using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBApi
{
    public class DBInstrument
    {
        public long id;
        public string shortName;
        public string longName;

        public DBInstrument()
        {
            id = 0;
            shortName = "";
            longName = "";
        }
    }
}
