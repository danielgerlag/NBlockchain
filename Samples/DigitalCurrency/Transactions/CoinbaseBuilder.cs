using NBlockchain.Interfaces;
using NBlockchain.Models;
using NBlockchain.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCurrency.Transactions
{
    public class CoinbaseBuilder : BlockbaseTransactionBuilder
    {
        public CoinbaseBuilder(IAddressEncoder addressEncoder, ISignatureService signatureService, ITransactionBuilder transactionBuilder) 
            : base(addressEncoder, signatureService, transactionBuilder)
        {
        }

        protected override ICollection<Instruction> BuildInstructions(KeyPair builderKeys, ICollection<Transaction> transactions)
        {
            var result = new List<Instruction>();
            var instruction = new CoinbaseInstruction();
            instruction.Amount = -50;
            instruction.PublicKey = builderKeys.PublicKey;

            SignatureService.SignInstruction(instruction, builderKeys.PrivateKey);
            result.Add(instruction);

            return result;
        }
    }
}
