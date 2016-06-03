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
                string sql = string.Format("SELECT * FROM {0}", INSTRUMENTS_TABLE_NAME);
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DBInstrument instrument = ExtractInstrument(reader);
                    instruments.Add(instrument);
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
                string sql = string.Format("INSERT INTO {0} (id, shortName, longName) VALUES ({1},{2},{3})", INSTRUMENTS_TABLE_NAME, "null", "\"" + shortNameWithEscaping + "\"", "\"" + longNameWithEscaping + "\"");
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                return command.ExecuteNonQuery();
            }
            return -1;
        }

        private void CreateTableIfNotExists()
        {
            string sql = string.Format("create table if not exists {0} (id INTEGER PRIMARY KEY, shortName TEXT, longName TEXT)", INSTRUMENTS_TABLE_NAME);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private DBInstrument ExtractInstrument(SQLiteDataReader reader)
        {
            DBInstrument instrument = new DBInstrument();
            instrument.id = (long)reader["id"];
            instrument.shortName = (string)reader["shortName"];
            instrument.longName = (string)reader["longName"];
            return instrument;
        }
    }
}
