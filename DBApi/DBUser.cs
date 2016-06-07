using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBApi
{
    public class DBUser
    {
        public long id { get; set; }
        public string name { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public string scype { get; set; }

        public DBUser()
        {
            id = 0;
            name = "";
            password = "";
            email = "";
            scype = "";
        }
    }
}
