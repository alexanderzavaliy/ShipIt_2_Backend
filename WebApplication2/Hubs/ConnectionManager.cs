using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication2.Hubs
{
    public class ConnectionManager
    {
        private Dictionary<long, HashSet<string>> connections;

        public ConnectionManager()
        { 
            connections = new Dictionary<long, HashSet<string>>();
        }

        public void Add(long key, string connectionId)
        {
            lock (connections)
            {
                HashSet<string> connectionIds;
                if (!connections.TryGetValue(key, out connectionIds))
                {
                    connectionIds = new HashSet<string>();
                    connections.Add(key, connectionIds);
                }

                lock (connectionIds)
                {
                    connectionIds.Add(connectionId);
                }
            }
        }

        public IList<string> GetConnectionIds(long key)
        {
            HashSet<string> connectionIds;
            if (connections.TryGetValue(key, out connectionIds))
            {
                return connectionIds.ToList<string>();
            }
            return new List<string>();
        }

        public IList<string> GetConnectionIdsExcept(long key)
        {
            Dictionary<long, HashSet<string>> result = new Dictionary<long, HashSet<string>>();
            HashSet<string> connectionIdsExceptKey = new HashSet<string>();
            foreach (KeyValuePair<long, HashSet<string>> connection in connections)
            {
                if (connection.Key != key)
                {
                    connectionIdsExceptKey.UnionWith(connection.Value);
                }
            }
            return connectionIdsExceptKey.ToList<string>();
        }

        public void Remove(long key, string connectionId)
        {
            lock (connections)
            {
                HashSet<string> connectionIds;
                if (!connections.TryGetValue(key, out connectionIds))
                {
                    return;
                }

                lock (connectionIds)
                {
                    connectionIds.Remove(connectionId);

                    if (connections.Count == 0)
                    {
                        connections.Remove(key);
                    }
                }
            }
        }
    }
}