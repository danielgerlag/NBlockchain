using NBlockChain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NBlockChain.Models
{
    public class TransactionBucket
    {
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(true);
        private readonly Dictionary<uint, ISet<byte[]>> _buckets = new Dictionary<uint, ISet<byte[]>>();
        private readonly Dictionary<byte[], TransactionEnvelope> _txns = new Dictionary<byte[], TransactionEnvelope>(new ByteArrayEqualityComparer());
        private readonly IEqualityComparer<byte[]> _byteArrayEqualityComparer = new ByteArrayEqualityComparer();

        public bool AddTransaction(byte[] txnId, TransactionEnvelope txn, uint height)
        {
            _resetEvent.WaitOne();
            try
            {
                EnsureKey(height);
                if (_buckets[height].Add(txnId))
                {
                    _txns[txnId] = txn;
                    return true;
                }
                return false;
            }
            finally
            {
                _resetEvent.Set();
            }
        }
        
        public ICollection<byte[]> GetBucket(uint height)
        {
            _resetEvent.WaitOne();
            try
            {
                EnsureKey(height);
                return _buckets[height];
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public void Shift(uint height, ICollection<byte[]> toRemove)
        {
            _resetEvent.WaitOne();
            try
            {
                EnsureKey(height);
                EnsureKey(height + 1);

                foreach (var item in _buckets[height].Where(x => !toRemove.Contains(x, _byteArrayEqualityComparer)))
                    _buckets[height + 1].Add(item);

                _buckets.Remove(height);

                foreach (var txnId in toRemove)
                    _txns.Remove(txnId);
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public ICollection<TransactionEnvelope> GetTransactions(uint height)
        {
            _resetEvent.WaitOne();
            try
            {
                if (_buckets[height] != null)
                {
                    return _txns.Where(x => _buckets[height].Contains(x.Key, _byteArrayEqualityComparer))
                        .Select(x => x.Value)
                        .ToList();                    
                }
                return new List<TransactionEnvelope>();
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        private void EnsureKey(uint height)
        {
            if (!_buckets.ContainsKey(height))
                _buckets.Add(height, new HashSet<byte[]>(_byteArrayEqualityComparer));
        }

    }
}
