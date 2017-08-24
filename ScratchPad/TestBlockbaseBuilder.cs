using System;
using System.Collections.Generic;
using System.Text;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using NBlockchain.Services;

namespace ScratchPad
{
    public class TestBlockbaseBuilder : BlockbaseTransactionBuilder<CoinbaseTransaction>
    {
        public TestBlockbaseBuilder(IAddressEncoder addressEncoder, ISignatureService signatureService) 
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
