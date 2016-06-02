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
        public string login;
        public string password;
        public DBUser()
        {
            id = 0;
            login = "";
            password = "";
        }
    }
}
