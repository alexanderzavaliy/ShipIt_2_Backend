using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBApi
{
    public class DBOrder
    {
        public long id { get; set; }
        public long ownerId { get; set; }
        public long instrumentId { get; set; }
        public long createdDate { get; set; }
        public long endDate { get; set; }
        public string type { get; set; }
        public double price { get; set; }
        public long amount { get; set; }
        public string status { get; set; }
        public long executorId { get; set; }
        public long executionDate { get; set; }

        public DBOrder()
        {
            id = 0;
            ownerId = 0;
            instrumentId = 0;
            createdDate = 0;
            endDate = 0;
            type = "";
            price = 0;
            amount = 0;
            status = "";
            executorId = 0;
            executionDate = 0;
        }

        public class Status
        {
            public const string NEW = "New";
            public const string LOCKED = "Locked";
            public const string PENDING = "Pending";
            public const string EXECUTED = "Executed";
        }
    }
}
