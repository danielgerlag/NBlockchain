using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LiteDB;
using NBlockchain.Interfaces;

namespace NBlockchain.Services.Database
{
    public class DataConnection : IDataConnection, IDisposable
    {
        public LiteDatabase Database { get; private set; }

        public DataConnection()
        {
            var stream = new MemoryStream();
            Database = new LiteDatabase(stream);
        }

        public DataConnection(string connectionString)
        {
            Database = new LiteDatabase(connectionString);
        }

        public void Dispose()
        {
            Database.Dispose();
        }
    }
}
