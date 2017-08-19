using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NBlockChain.Interfaces;
using NBlockChain.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NBlockChain.Services
{
    public abstract class TransactionValidator<T> : ITransactionValidator
        where T : class
    {
        public string TransactionType { get; }

        protected TransactionValidator()
        {
            var attr = typeof(T).GetTypeInfo().GetCustomAttribute<TransactionTypeAttribute>();
            TransactionType = attr.TypeId;
        }

        public int Validate(TransactionEnvelope transaction)
        {
            if (!(transaction is T))
                return -5;

            return Validate(transaction, transaction.Transaction as T);
        }

        protected abstract int Validate(TransactionEnvelope envelope, T transaction);
    }
}
