using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using Newtonsoft.Json.Linq;

namespace NBlockchain.Services
{
    public abstract class BlockbaseTransactionBuilder : IBlockbaseTransactionBuilder
    {
        protected readonly IAddressEncoder AddressEncoder;
        protected readonly ISignatureService SignatureService;
        protected readonly ITransactionBuilder TransactionBuilder;

        protected BlockbaseTransactionBuilder(IAddressEncoder addressEncoder, ISignatureService signatureService, ITransactionBuilder transactionBuilder)
        {
            AddressEncoder = addressEncoder;
            SignatureService = signatureService;
            TransactionBuilder = transactionBuilder;
        }

        public async Task<Transaction> Build(KeyPair builderKeys, ICollection<Transaction> transactions)
        {
            var instructions = BuildInstructions(builderKeys, transactions);
            return await TransactionBuilder.Build(instructions);
        }

        protected abstract ICollection<Instruction> BuildInstructions(KeyPair builderKeys, ICollection<Transaction> transactions);


    }
}
