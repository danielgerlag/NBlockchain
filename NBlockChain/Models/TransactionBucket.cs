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
        private readonly Dictionary<uint, ICollection<byte[]>> _buckets = new Dictionary<uint, ICollection<byte[]>>();
        private readonly IEqualityComparer<byte[]> _byteArrayEqualityComparer = new ByteArrayEqualityComparer();

        public void AddTransaction(byte[] txnId, uint height)
        {
            _resetEvent.WaitOne();
            try
            {
                EnsureKey(height);
                _buckets[height].Add(txnId);
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
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        private void EnsureKey(uint height)
        {
            if (!_buckets.ContainsKey(height))
                _buckets.Add(height, new HashSet<byte[]>());
        }

    }
}
