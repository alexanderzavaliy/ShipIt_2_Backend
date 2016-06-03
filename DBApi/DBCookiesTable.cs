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
                string sql = string.Format("SELECT * FROM ", COOKIES_TABLE_NAME);
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBCookie cookie = ExtractCookie(reader);
                    cookies.Add(cookie);
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
                string sql = string.Format("SELECT * FROM {0} WHERE value = {1}", COOKIES_TABLE_NAME, "\"" + valueWithEscaping + "\"");
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBCookie cookie = ExtractCookie(reader);
                    cookies.Add(cookie);
                }
            }
            return cookies;
        }

        public int InsertCookie(long ownerId, string value, long expirationDate)
        {
            if (connection != null)
            {
                string valueWithEscaping = DBHelper.AddEscaping(value);
                string sql = string.Format("INSERT INTO {0} (ownerId, value, expirationDate) VALUES ({1},{2},{3})", COOKIES_TABLE_NAME, ownerId, "\"" + valueWithEscaping + "\"", expirationDate);
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
                string sql = string.Format("DELETE FROM {0} WHERE value = {1}", COOKIES_TABLE_NAME, "\"" + valueWithEscaping + "\"");
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                return command.ExecuteNonQuery();
            }
            return -1;
        }

        private void CreateTableIfNotExists()
        {
            string sql = string.Format("create table if not exists {0} (ownerId INTEGER, value TEXT, expirationDate INTEGER)", COOKIES_TABLE_NAME);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private DBCookie ExtractCookie(SQLiteDataReader reader)
        {
            DBCookie cookie = new DBCookie();
            cookie.ownerId = (long)reader["ownerId"];
            cookie.value = (string)reader["value"];
            cookie.expirationDate = (long)reader["expirationDate"];
            return cookie;
        }
    }
}
