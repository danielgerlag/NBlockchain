using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using System.Linq;

namespace NBlockchain.Services.Database
{
    public abstract class TransactionRepository
    {
        protected readonly ILogger Logger;
        protected readonly IDataConnection Connection;

        protected LiteCollection<PersistedBlock> Blocks => Connection.Database.GetCollection<PersistedBlock>("Blocks");

        protected TransactionRepository(ILoggerFactory loggerFactory, IDataConnection connection)
        {
            Connection = connection;
            Logger = loggerFactory.CreateLogger<DefaultBlockRepository>();
        }
    }
}