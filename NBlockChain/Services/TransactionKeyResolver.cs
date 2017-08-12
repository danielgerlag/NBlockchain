using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBlockChain.Interfaces;
using NBlockChain.Models;
using Newtonsoft.Json;

namespace NBlockChain.Services
{
    public class TransactionKeyResolver : ITransactionKeyResolver
    {
        private readonly IHasher _hasher;

        public TransactionKeyResolver(IHasher hasher)
        {
            _hasher = hasher;
        }

        public byte[] ResolveKey(TransactionEnvelope txn)
        {
            var txnStr = txn.Transaction.ToString(Formatting.None);

            var seed = txn.OriginKey.ToByteArray()
                .Concat(Encoding.Unicode.GetBytes(txn.Originator))
                .Concat(Encoding.Unicode.GetBytes(txnStr));

            return _hasher.ComputeHash(seed.ToArray());
        }
    }
}
