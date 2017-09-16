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
    public class InstructionRepository : IInstructionRepository
    {
        protected readonly ILogger Logger;
        protected readonly IDataConnection Connection;

        protected LiteCollection<PersistedBlock> MainChain => Connection.Database.GetCollection<PersistedBlock>("MainChain");
        protected LiteCollection<PersistedInstruction> Instructions => Connection.Database.GetCollection<PersistedInstruction>("Instructions");

        public InstructionRepository(ILoggerFactory loggerFactory, IDataConnection connection)
        {
            Connection = connection;
            Logger = loggerFactory.CreateLogger<InstructionRepository>();
        }

        public Task<bool> HaveInstruction(byte[] instructionId)
        {
            return Task.FromResult(Instructions.Exists(x => x.Entity.InstructionId == instructionId));
        }
    }
}