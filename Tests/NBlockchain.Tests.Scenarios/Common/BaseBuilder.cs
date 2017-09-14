using NBlockchain.Services;
using System;
using System.Collections.Generic;
using System.Text;
using NBlockchain.Models;
using NBlockchain.Interfaces;

namespace NBlockchain.Tests.Scenarios.Common
{
    class BaseBuilder : BlockbaseTransactionBuilder
    {
        public BaseBuilder(IAddressEncoder addressEncoder, ISignatureService signatureService, ITransactionBuilder transactionBuilder) 
            : base(addressEncoder, signatureService, transactionBuilder)
        {
        }

        protected override ICollection<Instruction> BuildInstructions(KeyPair builderKeys, ICollection<Transaction> transactions)
        {
            var instructions = new HashSet<Instruction>();
            var i1 = new TestInstruction();
            i1.Data = "test";
            i1.PublicKey = builderKeys.PublicKey;
            SignatureService.SignInstruction(i1, builderKeys.PrivateKey);
            instructions.Add(i1);

            return instructions;
        }
    }

}
