using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBApi
{
    public class DBOrder
    {
        public long id;
        public long ownerId;
        public long instrumentId;
        public long createdDate;
        public long endDate;
        public string type;
        public double price;
        public long amount;
        public string status;
        public long executorId;
        public long executionDate;

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
    }
}
