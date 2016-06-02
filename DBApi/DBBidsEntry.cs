using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBApi
{
    public class DBBidsEntry
    {
        public long id;
        public string currency;
        public string operationType;
        public long amount;
        public double price;
        public string poster;
        public long timestamp;

        public DBBidsEntry()
        {
            id = 0;
            currency = "";
            operationType = "";
            amount = 0;
            price = 0;
            poster = "";
            timestamp = DateTime.MinValue.Ticks;
        }
    }
}
