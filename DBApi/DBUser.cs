using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBApi
{
    public class DBUser
    {
        public long id;
        public string name;
        public string password;
        public string email;
        public string scype;

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
