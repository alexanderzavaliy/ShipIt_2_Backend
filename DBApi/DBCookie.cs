using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBApi
{
    public class DBCookie
    {
        public long ownerId;
        public string value;
        public long expirationDate;
        public DBCookie()
        {
            ownerId = 0;
            value = "";
            expirationDate = 0;
        }
    }
}
