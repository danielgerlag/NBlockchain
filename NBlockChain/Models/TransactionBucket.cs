using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NBlockChain.Models
{
    public class TransactionBucket
    {
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(true);
        private readonly Dictionary<uint, ICollection<byte[]>> _buckets = new Dictionary<uint, ICollection<byte[]>>();

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
                _buckets[height].Add(txnId);
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
