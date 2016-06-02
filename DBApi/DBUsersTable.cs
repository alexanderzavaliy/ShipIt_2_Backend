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
                string sql = "SELECT id, login, password FROM " + USERS_TABLE_NAME;
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBUser user = new DBUser();
                    user.id = (long)reader["id"];
                    user.login = (string)reader["login"];
                    user.password = (string)reader["password"];
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
                string sql = "SELECT id, login, password FROM " + USERS_TABLE_NAME + " WHERE login = " + "\"" + loginWithEscaping + "\"" + " AND password = " + "\"" + passwordWithEscaping + "\"";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBUser user = new DBUser();
                    user.id = (long)reader["id"];
                    user.login = (string)reader["login"];
                    user.password = (string)reader["password"];
                    users.Add(user);
                }
            }
            return users;
        }

        public int InsertUser(string login, string password)
        {
            if (connection != null)
            {
                string loginWithEscaping = DBHelper.AddEscaping(login);
                string passwordWithEscaping = DBHelper.AddEscaping(password);
                string sql = "INSERT INTO " + USERS_TABLE_NAME + " (id, login, password) VALUES (" + "null, " + "\"" + loginWithEscaping + "\"" + ", " + "\"" + passwordWithEscaping + "\"" + ")";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                return command.ExecuteNonQuery();
            }
            return -1;
        }

        private void CreateTableIfNotExists()
        {
            string sql = "create table if not exists " + USERS_TABLE_NAME + " (id INTEGER PRIMARY KEY AUTOINCREMENT, login TEXT, password TEXT)";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}
