using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace DBApi
{
    public class DBUsersTable
    {
        private SQLiteConnection connection;
        private const string USERS_TABLE_NAME = "Users";
        public DBUsersTable(SQLiteConnection connection)
        {
            this.connection = connection;
            CreateTableIfNotExists();
        }
        public List<DBUser> SelectAllUsers()
        {
            List<DBUser> users = new List<DBUser>();
            if (connection != null)
            {
                string sql = string.Format("SELECT * FROM {0}", USERS_TABLE_NAME);
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBUser user = ExtractUser(reader);
                    users.Add(user);
                }
            }
            return users;
        }

        public List<DBUser> SelectUser(string login, string password)
        {
            List<DBUser> users = new List<DBUser>();
            if (connection != null)
            {
                string loginWithEscaping = DBHelper.AddEscaping(login);
                string passwordWithEscaping = DBHelper.AddEscaping(password);
                string sql = string.Format("SELECT * FROM {0} WHERE login = {1} AND password = {2}", USERS_TABLE_NAME, "\"" + loginWithEscaping + "\"", "\"" + passwordWithEscaping + "\"");
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBUser user = ExtractUser(reader);
                    users.Add(user);
                }
            }
            return users;
        }

        public List<DBUser> SelectUserById(long id)
        {
            List<DBUser> users = new List<DBUser>();
            if (connection != null)
            {
                string sql = string.Format("SELECT * FROM {0} WHERE id = {1}", USERS_TABLE_NAME,id);
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBUser user = ExtractUser(reader);
                    users.Add(user);
                }
            }
            return users;
        }

        public int InsertUser(string name, string password, string email, string scype)
        {
            if (connection != null)
            {
                string nameWithEscaping = DBHelper.AddEscaping(name);
                string passwordMD5 = DBHelper.GetMD5(password);
                string emailWithEscaping = DBHelper.AddEscaping(email);
                string scypeWithEscaping = DBHelper.AddEscaping(scype);
                string sql = string.Format("INSERT INTO {0} (id, name, password, email, scype) VALUES ({1},{2},{3},{4},{5})", USERS_TABLE_NAME, "null", "\"" + nameWithEscaping + "\"", "\"" + passwordMD5 + "\"", "\"" + emailWithEscaping + "\"", "\"" + scypeWithEscaping + "\"");
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                return command.ExecuteNonQuery();
            }
            return -1;
        }

        private void CreateTableIfNotExists()
        {
            string sql = string.Format("create table if not exists {0} (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, password TEXT, email TEXT, scype TEXT)", USERS_TABLE_NAME);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private DBUser ExtractUser(SQLiteDataReader reader)
        {
            DBUser user = new DBUser();
            user.id = (long)reader["id"];
            user.name = (string)reader["name"];
            user.password = (string)reader["password"];
            user.email = (string)reader["email"];
            user.scype = (string)reader["scype"];
            return user;
        }
    }
}
