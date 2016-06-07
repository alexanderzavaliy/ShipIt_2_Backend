using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBApi
{
    public class DBInstrument
    {
        public long id { get; set; }
        public string shortName { get; set; }
        public string longName { get; set; }

        public DBInstrument()
        {
            id = 0;
            shortName = "";
            longName = "";
        }
    }
}
