using System;
using System.Collections.Generic;
using System.Text;
using NBlockChain.Interfaces;
using NBlockChain.Models;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NBlockChain.Services
{
    public class BlockVerifier : IBlockVerifier
    {
        private readonly INetworkParameters _parameters;
        private readonly IEnumerable<ITransactionValidator> _txnValidators;
        private readonly IEnumerable<ValidTransactionType> _validTxnTypes;
        private readonly IAddressEncoder _addressEncoder;
        private readonly ISignatureService _signatureService;
        private readonly IHashTester _hashTester;
        private readonly IHasher _hasher;
        private readonly IMerkleTreeBuilder _merkleTreeBuilder;
        private readonly ITransactionKeyResolver _transactionKeyResolver;
        private readonly IEqualityComparer<byte[]> _byteArrayEqualityComparer = new ByteArrayEqualityComparer();

        public BlockVerifier(INetworkParameters parameters, IAddressEncoder addressEncoder, ISignatureService signatureService, IEnumerable<ITransactionValidator> txnValidators, IEnumerable<ValidTransactionType> validTxnTypes, IMerkleTreeBuilder merkleTreeBuilder, ITransactionKeyResolver transactionKeyResolver, IHashTester hashTester, IHasher hasher)
        {
            _parameters = parameters;
            _addressEncoder = addressEncoder;
            _signatureService = signatureService;
            _txnValidators = txnValidators;
            _validTxnTypes = validTxnTypes;
            _merkleTreeBuilder = merkleTreeBuilder;
            _transactionKeyResolver = transactionKeyResolver;
            _hashTester = hashTester;
            _hasher = hasher;
        }

        public bool Verify(Block block, uint difficulty)
        {
            if (!_hashTester.TestHash(block.Header.BlockId, difficulty))
                return false;

            var seed = block.Header.CombineHashableElementsWithNonce(block.Header.Nonce);
            var hash = _hasher.ComputeHash(seed);

            if (!hash.SequenceEqual(block.Header.BlockId))
                return false;

            var hashDict = HashTransactions(block.Transactions);
            var merkleRoot = _merkleTreeBuilder.BuildTree(hashDict.Keys).Result;

            if (!merkleRoot.Value.SequenceEqual(block.Header.MerkelRoot))
                return false;

            foreach (var txn in block.Transactions)
            {
                var siblings = block.Transactions.Where(x => x != txn).ToList();
                if (VerifyTransaction(txn, siblings) != 0)
                    return false;
            }

            return true;
        }

        public bool VerifyContentThreshold(ICollection<byte[]> actual, ICollection<byte[]> expected)
        {
            if (expected.Count == 0)
                return true;

            var count = expected.Count(txn => actual.Contains(txn, _byteArrayEqualityComparer));
            var ratio = (decimal)count / (decimal)expected.Count;
            return (ratio >= _parameters.ExpectedContentThreshold);
        }

        public int VerifyTransaction(TransactionEnvelope transaction, ICollection<TransactionEnvelope> siblings)
        {
            var result = 0;

            if (!_addressEncoder.IsValidAddress(transaction.Originator))
                return -1;

            if (_validTxnTypes.All(x => x.TransactionType != transaction.TransactionType))
                return -2;

            if (!_signatureService.VerifyTransaction(transaction))
                return -3;

            foreach (var validator in _txnValidators.Where(v => v.TransactionType == transaction.TransactionType))
                result = result & validator.Validate(transaction, siblings);

            return result;
        }
        
        private IDictionary<byte[], TransactionEnvelope> HashTransactions(ICollection<TransactionEnvelope> transactions)
        {
            var result = new ConcurrentDictionary<byte[], TransactionEnvelope>();

            Parallel.ForEach(transactions, txn =>
            {
                var key = _transactionKeyResolver.ResolveKey(txn);
                result[key] = txn;
            });

            return result;
        }
    }
}
