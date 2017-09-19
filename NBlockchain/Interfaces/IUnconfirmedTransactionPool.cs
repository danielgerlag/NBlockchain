using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Interfaces
{
    public interface IUnconfirmedTransactionPool
    {

        event EventHandler Changed;

        bool Add(Transaction txn);

        void Remove(ICollection<Transaction> toRemove);

        ICollection<Transaction> Get { get; }

    }
}
