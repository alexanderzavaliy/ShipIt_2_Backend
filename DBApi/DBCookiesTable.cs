using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace DBApi
{
    public class DBCookiesTable
    {
        private SQLiteConnection connection;
        private string COOKIES_TABLE_NAME = "Cookies";
        public DBCookiesTable(SQLiteConnection connection)
        {
            this.connection = connection;
            CreateTableIfNotExists();
        }

        public List<DBCookie> SelectAllCookies()
        {
            List<DBCookie> cookies = new List<DBCookie>();
            if (connection != null)
            {
                string sql = "SELECT value, expirationDate FROM " + COOKIES_TABLE_NAME;
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBCookie user = new DBCookie();
                    user.value = (string)reader["value"];
                    user.expirationDate = (long)reader["expirationDate"];
                    cookies.Add(user);
                }
            }
            return cookies;
        }

        public List<DBCookie> SelectCookie(string value)
        {
            List<DBCookie> cookies = new List<DBCookie>();
            if (connection != null)
            {
                string valueWithEscaping = DBHelper.AddEscaping(value);
                string sql = "SELECT value, expirationDate FROM " + COOKIES_TABLE_NAME + " WHERE value = " + "\"" + valueWithEscaping + "\"";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBCookie user = new DBCookie();
                    user.value = (string)reader["value"];
                    user.expirationDate = (long)reader["expirationDate"];
                    cookies.Add(user);
                }
            }
            return cookies;
        }

        public int InsertCookie(string value, long expirationDate)
        {
            if (connection != null)
            {
                string valueWithEscaping = DBHelper.AddEscaping(value);
                string sql = "INSERT INTO " + COOKIES_TABLE_NAME + " (value, expirationDate) VALUES (" + "\"" + valueWithEscaping + "\"" + ", " + expirationDate + ")";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                return command.ExecuteNonQuery();
            }
            return -1;
        }

        public int DeleteCookie(string value)
        {
            if (connection != null)
            {
                string valueWithEscaping = DBHelper.AddEscaping(value);
                string sql = "DELETE FROM " + COOKIES_TABLE_NAME + " WHERE value = " + "\"" + value + "\"";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                return command.ExecuteNonQuery();
            }
            return -1;
        }

        private void CreateTableIfNotExists()
        {
            string sql = "create table if not exists " + COOKIES_TABLE_NAME + " (value TEXT, expirationDate INTEGER)";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}
