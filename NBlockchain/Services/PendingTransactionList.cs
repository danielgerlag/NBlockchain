using NBlockchain.Interfaces;
using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NBlockchain.Services
{
    public class PendingTransactionList : IPendingTransactionList
    {
        private readonly ICollection<TransactionEnvelope> _list = new List<TransactionEnvelope>();
        private readonly AutoResetEvent _evt = new AutoResetEvent(true);

        public ICollection<TransactionEnvelope> Get
        {
            get
            {
                _evt.WaitOne();
                try
                {
                    var result = new List<TransactionEnvelope>();
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

        public void Add(TransactionEnvelope txn)
        {
            _evt.WaitOne();
            try
            {
                _list.Add(txn);
                Task.Factory.StartNew(() => Changed?.Invoke(this, new EventArgs()));
            }
            finally
            {
                _evt.Set();
            }
        }

        public void Remove(ICollection<TransactionEnvelope> toRemove)
        {
            _evt.WaitOne();
            try
            {
                foreach (var item in toRemove)
                {
                    var removals = _list.Where(x => x.OriginKey == item.OriginKey && x.Originator == item.Originator).ToList();
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
