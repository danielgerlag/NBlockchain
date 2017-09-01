using NBlockchain.Services;
using System;
using System.Collections.Generic;
using System.Text;
using NBlockchain.Models;
using NBlockchain.Interfaces;

namespace NBlockchain.Tests.Scenarios.Common
{
    class BaseBuilder : BlockbaseTransactionBuilder<TestTransaction>
    {
        protected BaseBuilder(IAddressEncoder addressEncoder, ISignatureService signatureService) 
            : base(addressEncoder, signatureService)
        {
        }

        protected override TestTransaction BuildBaseTransaction(ICollection<TransactionEnvelope> transactions)
        {
            return new TestTransaction() { Data = "base" };
        }
    }

}
