using NBlockchain.Interfaces;
using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NBlockchain.Services
{
    public class UnconfirmedTransactionPool : IUnconfirmedTransactionPool
    {
        private readonly ICollection<Transaction> _list = new List<Transaction>();
        private readonly AutoResetEvent _evt = new AutoResetEvent(true);

        public ICollection<Transaction> Get
        {
            get
            {
                _evt.WaitOne();
                try
                {
                    var result = new List<Transaction>();
                    result.AddRange(_list);
                    return result;
                }
                finally
                {
                    _evt.Set();
                }
            }
        }

        public event EventHandler Changed;

        public bool Add(Transaction txn)
        {
            _evt.WaitOne();
            try
            {
                if (_list.Any(x => txn.TransactionId.SequenceEqual(x.TransactionId)))
                    return false;

                _list.Add(txn);
                Task.Factory.StartNew(() => Changed?.Invoke(this, new EventArgs()));
                return true;
            }
            finally
            {
                _evt.Set();
            }
        }

        public void Remove(ICollection<Transaction> toRemove)
        {
            _evt.WaitOne();
            try
            {
                foreach (var item in toRemove)
                {
                    var removals = _list.Where(x => item.TransactionId.SequenceEqual(x.TransactionId)).ToList();
                    foreach (var remove in removals)
                        _list.Remove(remove);
                }
                Task.Factory.StartNew(() => Changed?.Invoke(this, new EventArgs()));
            }
            finally
            {
                _evt.Set();
            }
        }
    }
}
