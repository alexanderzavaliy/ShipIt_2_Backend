using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBApi
{
    public class DBCookie
    {
        public long ownerId { get; set; }
        public string value { get; set; }
        public long expirationDate { get; set; }
        public DBCookie()
        {
            ownerId = 0;
            value = "";
            expirationDate = 0;
        }
    }
}
