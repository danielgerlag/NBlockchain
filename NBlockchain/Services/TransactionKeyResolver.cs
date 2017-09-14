using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using Newtonsoft.Json;

namespace NBlockchain.Services
{
    public class TransactionKeyResolver : ITransactionKeyResolver
    {
        private readonly IHasher _hasher;
        private readonly IMerkleTreeBuilder _merkleTreeBuilder;

        public TransactionKeyResolver(IHasher hasher, IMerkleTreeBuilder merkleTreeBuilder)
        {
            _hasher = hasher;
            _merkleTreeBuilder = merkleTreeBuilder;
        }

        public async Task<byte[]> ResolveKey(Transaction txn)
        {
            var tree = await _merkleTreeBuilder.BuildTree(txn.Instructions.Select(x => x.InstructionId).ToList());
            return tree.Value;
        }
    }
}
