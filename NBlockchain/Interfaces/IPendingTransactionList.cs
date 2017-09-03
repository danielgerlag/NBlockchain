using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Interfaces
{
    public interface IPendingTransactionList
    {

        event EventHandler Changed;

        bool Add(TransactionEnvelope txn);

        void Remove(ICollection<TransactionEnvelope> toRemove);

        ICollection<TransactionEnvelope> Get { get; }

    }
}
