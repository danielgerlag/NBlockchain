using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NBlockchain.Rules
{
    public abstract class TransactionRule<T> : ITransactionRule
        where T : class
    {
        public string TransactionType { get; }

        protected TransactionRule()
        {
            var attr = typeof(T).GetTypeInfo().GetCustomAttribute<TransactionTypeAttribute>();
            TransactionType = attr.TypeId;
        }

        public int Validate(TransactionEnvelope transaction, ICollection<TransactionEnvelope> siblings)
        {
            if (!(transaction is T))
                return -5;

            return Validate(transaction, transaction.Transaction as T, siblings);
        }

        protected abstract int Validate(TransactionEnvelope envelope, T transaction, ICollection<TransactionEnvelope> siblings);
    }
}
