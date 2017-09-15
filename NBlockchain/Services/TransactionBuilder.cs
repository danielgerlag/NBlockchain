using System;
using System.Collections.Generic;
using System.Text;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NBlockchain.Services
{
    public class TransactionBuilder : ITransactionBuilder
    {
        private readonly ITransactionKeyResolver _keyResolver;

        public TransactionBuilder(ITransactionKeyResolver keyResolver)
        {
            _keyResolver = keyResolver;
        }

        public async Task<Transaction> Build(ICollection<Instruction> instructions)
        {
            var result = new Transaction(instructions);
            result.TransactionId = await _keyResolver.ResolveKey(result);
            return result;
        }
    }
}
