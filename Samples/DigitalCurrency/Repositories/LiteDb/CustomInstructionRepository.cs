using DigitalCurrency.Transactions;
using LiteDB;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;
using NBlockchain.Services.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DigitalCurrency.Repositories.LiteDb
{
    public class CustomInstructionRepository : InstructionRepository, ICustomInstructionRepository
    {
        private readonly IAddressEncoder _addressEncoder;

        public CustomInstructionRepository(ILoggerFactory loggerFactory, IDataConnection dataConnection, IAddressEncoder addressEncoder)
            : base(loggerFactory, dataConnection)
        {
            _addressEncoder = addressEncoder;
        }

        public decimal GetAccountBalance(string address)
        {
            var publicKeyHash = _addressEncoder.ExtractPublicKeyHash(address);

            var totalOut = Instructions
                .Find(Query.EQ("Statistics.PublicKeyHash", publicKeyHash))
                .Select(x => x.Entity)
                .OfType<ValueInstruction>()
                .Sum(x => x.Amount);


            var totalIn = Instructions
                .Find(Query.EQ("Entity.Destination", publicKeyHash))
                .Select(x => x.Entity)
                .OfType<TransferInstruction>()
                .Sum(x => x.Amount);

            return (totalIn - totalOut);
        }
    }
}
