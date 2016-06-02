using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBApi
{
    public class DBCookie
    {
        public string value;
        public long expirationDate;
        public DBCookie()
        {
            value = "";
            expirationDate = DateTime.MinValue.ToUniversalTime().Ticks;
        }
    }
}
