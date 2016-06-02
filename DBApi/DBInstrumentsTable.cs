using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace DBApi
{
    public class DBInstrumentsTable
    {
        private SQLiteConnection connection;
        private const string INSTRUMENTS_TABLE_NAME = "Instruments";
        
        public DBInstrumentsTable(SQLiteConnection connection)
        {
            this.connection = connection;
            CreateTableIfNotExists();
        }

        public List<DBInstrument> SelectAllInstruments()
        {
            List<DBInstrument> instruments = new List<DBInstrument>();
            if (connection != null)
            {
                string sql = "SELECT id, shortName, longName FROM " + INSTRUMENTS_TABLE_NAME;
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBInstrument user = new DBInstrument();
                    user.id = (long)reader["id"];
                    user.shortName = (string)reader["shortName"];
                    user.longName = (string)reader["longName"];
                    instruments.Add(user);
                }
            }
            return instruments;
        }

        public int InsertInstrument(string shortName, string longName)
        {
            if (connection != null)
            {
                string shortNameWithEscaping = DBHelper.AddEscaping(shortName);
                string longNameWithEscaping = DBHelper.AddEscaping(longName);
                string sql = "INSERT INTO " + INSTRUMENTS_TABLE_NAME + " (id, shortName, longName) VALUES (" + "null, " + "\"" + shortNameWithEscaping + "\"" + ", " + "\"" + longNameWithEscaping + "\"" + ")";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                return command.ExecuteNonQuery();
            }
            return -1;
        }

        private void CreateTableIfNotExists()
        {
            string sql = "create table if not exists " + INSTRUMENTS_TABLE_NAME + " (id INTEGER PRIMARY KEY, shortName TEXT, longName TEXT)";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}
