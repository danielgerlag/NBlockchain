using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NBlockChain.Interfaces;
using NBlockChain.Models;

namespace NBlockChain.Services
{
    public abstract class TransactionValidator<T> : ITransactionValidator
    {
        public string TransactionType { get; }

        protected TransactionValidator()
        {
            var attr = typeof(T).GetTypeInfo().GetCustomAttribute<TransactionTypeAttribute>();
            TransactionType = attr.TypeId;
        }

        public Task<int> Validate(TransactionEnvelope transaction)
        {
            throw new NotImplementedException();
        }
    }
}
