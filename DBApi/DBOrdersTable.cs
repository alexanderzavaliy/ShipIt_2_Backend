using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace DBApi
{
    public class DBOrdersTable
    {
        private SQLiteConnection connection;
        private const string ORDERS_TABLE_NAME = "Orders";

        public DBOrdersTable(SQLiteConnection connection)
        {
            this.connection = connection;
            CreateTableIfNotExists();
        }

        public List<DBOrder> SelectAllOrders()
        {
            List<DBOrder> orders = new List<DBOrder>();
            if (connection != null)
            {
                string sql = string.Format("SELECT * FROM {0}", ORDERS_TABLE_NAME);
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBOrder order = ExtractDBOrder(reader);
                    orders.Add(order);
                }
            }
            return orders;
        }

        public List<DBOrder> SelectLastOrderForOwner(long ownerId)
        {
            List<DBOrder> orders = new List<DBOrder>();
            if (connection != null)
            {
                string sql = string.Format("SELECT * FROM {0} WHERE id = {1} AND id = (SELECT MAX(id) FROM {0} WHERE ownerId = {1})", ORDERS_TABLE_NAME, ownerId);
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBOrder order = ExtractDBOrder(reader);
                    orders.Add(order);
                }
            }
            return orders;
        }

        public List<DBOrder> SelectOrdersForOwner(long ownerId)
        {
            List<DBOrder> orders = new List<DBOrder>();
            if (connection != null)
            {
                string sql = string.Format("SELECT * FROM {0} WHERE ownerId = {1}", ORDERS_TABLE_NAME, ownerId);
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBOrder order = ExtractDBOrder(reader);
                    orders.Add(order);
                }
            }
            return orders;
        }

        public int InsertOrder(long ownerId, long instrumentId, long createdDate, long endDate, string type, double price, long amount, string status, long executorId, long executionDate)
        {
            if (connection != null)
            {
                string typeWithEscaping = DBHelper.AddEscaping(type);
                string statusWithEscaping = DBHelper.AddEscaping(status);
                string sql = string.Format("INSERT INTO {0} (id, ownerId, instrumentId, createdDate, endDate, type, price, amount, status, executorId, executionDate) VALUES ({1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11})", ORDERS_TABLE_NAME, "null", ownerId, instrumentId, createdDate, endDate, "\"" + typeWithEscaping + "\"", price, amount, "\"" + statusWithEscaping + "\"", executorId, executionDate);
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                return command.ExecuteNonQuery();
            }
            return -1;
        }

        private void CreateTableIfNotExists()
        {
            string sql = string.Format("create table if not exists {0} (id INTEGER PRIMARY KEY AUTOINCREMENT, ownerId INTEGER, instrumentId INTEGER, createdDate INTEGER, endDate INTEGER, type TEXT, price REAL, amount INTEGER, status TEXT, executorId INTEGER, executionDate INTEGER)", ORDERS_TABLE_NAME);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private DBOrder ExtractDBOrder(SQLiteDataReader reader)
        {
            DBOrder order = new DBOrder();
            order.id = (long)reader["id"];
            order.ownerId = (long)reader["ownerId"];
            order.instrumentId = (long)reader["instrumentId"];
            order.createdDate = (long)reader["createdDate"];
            order.endDate = (long)reader["endDate"];
            order.type = (string)reader["type"];
            order.price = (double)reader["price"];
            order.amount = (long)reader["amount"];
            order.status = (string)reader["status"];
            order.executorId = (long)reader["executorId"];
            order.executionDate = (long)reader["executionDate"];
            return order;
        }
    }
}
