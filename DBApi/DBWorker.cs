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
        public DBUsersTable Users { get; set; }
        public DBCookiesTable Cookies { get; set; }
        public DBInstrumentsTable Instruments { get; set; }
        public DBOrdersTable Orders { get; set; }

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
            connection.Update += new SQLiteUpdateEventHandler(OnDBUpdate);

            Users = new DBUsersTable(connection);
            Cookies = new DBCookiesTable(connection);
            Instruments = new DBInstrumentsTable(connection);
            Orders = new DBOrdersTable(connection);
        }

        public void Close()
        {
            if (connection != null)
            {
                connection.Close();
            }
        }

        private void OnDBUpdate(object sender, UpdateEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("On DBUpdate() called");   
        }
    }
}