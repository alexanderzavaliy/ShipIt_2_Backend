using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Globalization;

namespace DBApi
{
    public class DBBidsTable
    {
        private SQLiteConnection connection;
        private string BIDS_TABLE_NAME = "Bids";
        public DBBidsTable(SQLiteConnection connection)
        {
            this.connection = connection;
            CreateTableIfNotExists();
        }

        public List<DBBidsEntry> SelectAllBids()
        {
            List<DBBidsEntry> bids = new List<DBBidsEntry>();
            if (connection != null)
            {
                string sql = "SELECT id, operationType, currency, amount, price, poster, timestamp FROM " + BIDS_TABLE_NAME;
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBBidsEntry entry = new DBBidsEntry();
                    entry.id = (long)reader["id"];
                    entry.currency = (string)reader["currency"];
                    entry.operationType = (string)reader["operationType"];
                    entry.amount = (long)reader["amount"];
                    entry.price = (double)reader["price"];
                    entry.poster = (string)reader["poster"];
                    entry.timestamp = (long)reader["timestamp"];
                    bids.Add(entry);
                }
            }
            return bids;
        }

        public List<DBBidsEntry> SelectBidById(long bidId)
        {
            List<DBBidsEntry> bids = new List<DBBidsEntry>();
            if (connection != null)
            {
                string sql = "SELECT id, operationType, currency, amount, price, poster, timestamp FROM " + BIDS_TABLE_NAME + " WHERE id = " + bidId;
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBBidsEntry entry = new DBBidsEntry();
                    entry.id = (long)reader["id"];
                    entry.currency = (string)reader["currency"];
                    entry.operationType = (string)reader["operationType"];
                    entry.amount = (long)reader["amount"];
                    entry.price = (double)reader["price"];
                    entry.poster = (string)reader["poster"];
                    entry.timestamp = (long)reader["timestamp"];
                    bids.Add(entry);
                }
            }
            return bids;
        }
        public List<DBBidsEntry> SelectLastBidForPoster(string poster)
        {
            List<DBBidsEntry> bids = new List<DBBidsEntry>();
            if (connection != null)
            {
                //string sql = "SELECT id, operationType, currency, count, price, poster, timestamp FROM " + tableName + " WHERE poster = " + "\"" + poster + "\"";
                string sql = "SELECT * FROM " + BIDS_TABLE_NAME + " WHERE poster = " + "\"" + poster + "\"" + " AND " + " id = (SELECT MAX(id) FROM " + BIDS_TABLE_NAME + " WHERE poster = " + "\"" + poster + "\"" + ")";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBBidsEntry entry = new DBBidsEntry();
                    entry.id = (long)reader["id"];
                    entry.currency = (string)reader["currency"];
                    entry.operationType = (string)reader["operationType"];
                    entry.amount = (long)reader["amount"];
                    entry.price = (double)reader["price"];
                    entry.poster = (string)reader["poster"];
                    entry.timestamp = (long)reader["timestamp"];
                    bids.Add(entry);
                }
            }
            return bids;
        }

        public List<DBBidsEntry> SelectBidsForPoster(string poster)
        {
            List<DBBidsEntry> bids = new List<DBBidsEntry>();
            if (connection != null)
            {
                string sql = "SELECT * FROM " + BIDS_TABLE_NAME + " WHERE poster = " + "\"" + poster + "\"";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBBidsEntry entry = new DBBidsEntry();
                    entry.id = (long)reader["id"];
                    entry.currency = (string)reader["currency"];
                    entry.operationType = (string)reader["operationType"];
                    entry.amount = (long)reader["amount"];
                    entry.price = (double)reader["price"];
                    entry.poster = (string)reader["poster"];
                    entry.timestamp = (long)reader["timestamp"];
                    bids.Add(entry);
                }
            }
            return bids;
        }

        public int InsertBid(string operationType, string currency, int amount, double price, string poster, long timestamp)
        {
            if (connection != null)
            {
                string posterWithEscaping = DBHelper.AddEscaping(poster);
                NumberFormatInfo nfi = new NumberFormatInfo();
                nfi.NumberDecimalSeparator = ".";
                string sql = "INSERT INTO " + BIDS_TABLE_NAME + " (id, operationType, currency, amount, price, poster, timestamp) VALUES (" + "null, " + "\"" + operationType + "\"" + ", " + "\"" + currency + "\"" + ", " + amount + ", " + price.ToString(nfi) + ", " + "\"" + posterWithEscaping + "\"" + ", " + timestamp + ")";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                return command.ExecuteNonQuery();
            }
            return -1;
        }

        public int DeleteBid(long id)
        {
            if (connection != null)
            {
                string sql = "DELETE FROM " + BIDS_TABLE_NAME + " WHERE id = " + id;
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                return command.ExecuteNonQuery();
            }
            return -1;
        }

        public int DeleteAllBids()
        {
            if (connection != null)
            {
                string sql = "DELETE FROM " + BIDS_TABLE_NAME;
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                return command.ExecuteNonQuery();
            }
            return -1;
        }

        private void CreateTableIfNotExists()
        {
            string sql = "create table if not exists " + BIDS_TABLE_NAME + " (id INTEGER PRIMARY KEY AUTOINCREMENT, operationType TEXT, currency TEXT, amount INTEGER, price REAL, poster TEXT, timestamp INTEGER)";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}
