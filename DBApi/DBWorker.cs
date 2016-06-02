using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Data.SQLite;
using System.Globalization;

namespace DBApi
{
    public class DBWorker
    {
        private string dbFilePath;
        private SQLiteConnection connection;
        public DBBidsTable Bids;
        public DBUsersTable Users;
        public DBCookiesTable Cookies;

        public DBWorker(string dbFilePath)
        {
            this.dbFilePath = dbFilePath;
        }

        public void Open()
        {
            if (!File.Exists(dbFilePath))
            {
                SQLiteConnection.CreateFile(dbFilePath);
            }
            connection = new SQLiteConnection("Data Source=" + dbFilePath + ";" + "Version=3;");
            connection.Open();

            Bids = new DBBidsTable(connection);
            Users = new DBUsersTable(connection);
            Cookies = new DBCookiesTable(connection);
        }

        public void Close()
        {
            if (connection != null)
            {
                connection.Close();
            }
        }
    }
}