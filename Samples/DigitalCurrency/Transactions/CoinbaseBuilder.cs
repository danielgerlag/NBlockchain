using NBlockchain.Interfaces;
using NBlockchain.Models;
using NBlockchain.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCurrency.Transactions
{
    public class CoinbaseBuilder : BlockbaseTransactionBuilder<CoinbaseTransaction>
    {
        public CoinbaseBuilder(IAddressEncoder addressEncoder, ISignatureService signatureService) 
            : base(addressEncoder, signatureService)
        {
        }

        protected override CoinbaseTransaction BuildBaseTransaction(ICollection<TransactionEnvelope> transactions)
        {
            return new CoinbaseTransaction()
            {
                Amount = -50
            };
        }
    }
}
