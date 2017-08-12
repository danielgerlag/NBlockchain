using System;
using System.Collections.Generic;
using System.Text;
using NBlockChain.Interfaces;
using NBlockChain.Models;
using NBlockChain.Services;

namespace ScratchPad
{
    public class TestBlockbaseBuilder : BlockbaseTransactionBuilder<TestTransaction>
    {
        public TestBlockbaseBuilder(IAddressEncoder addressEncoder, ISignatureService signatureService) 
            : base(addressEncoder, signatureService)
        {
        }
        
        protected override TestTransaction BuildBaseTransaction(ICollection<TransactionEnvelope> transactions)
        {
            return new TestTransaction()
            {
                Message = "base"
            };
        }
    }
}
