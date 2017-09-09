using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using Newtonsoft.Json.Linq;

namespace NBlockchain.Services
{
    public abstract class BlockbaseTransactionBuilder<T> : IBlockbaseTransactionBuilder
        where T : BlockTransaction
    {

        private readonly IAddressEncoder _addressEncoder;
        private readonly ISignatureService _signatureService;
        private readonly TransactionTypeAttribute _transactionTypeMetadata;

        protected BlockbaseTransactionBuilder(IAddressEncoder addressEncoder, ISignatureService signatureService)
        {
            _addressEncoder = addressEncoder;
            _signatureService = signatureService;
            var typeInfo = typeof(T).GetTypeInfo();
            _transactionTypeMetadata = typeInfo.GetCustomAttribute<TransactionTypeAttribute>();
        }

        public TransactionEnvelope Build(KeyPair builderKeys, ICollection<TransactionEnvelope> transactions)
        {
            var result = new TransactionEnvelope();

            result.Originator = _addressEncoder.EncodeAddress(builderKeys.PublicKey, 0);
            result.OriginKey = Guid.NewGuid();
            result.TransactionType = _transactionTypeMetadata.TypeId;
            result.Transaction = BuildBaseTransaction(transactions);

            _signatureService.SignTransaction(result, builderKeys.PrivateKey);

            return result;
        }

        protected abstract T BuildBaseTransaction(ICollection<TransactionEnvelope> transactions);

    }
}
