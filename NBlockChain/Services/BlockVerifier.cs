using System;
using System.Collections.Generic;
using System.Text;
using NBlockChain.Interfaces;
using NBlockChain.Models;
using System.Linq;

namespace NBlockChain.Services
{
    public class BlockVerifier : IBlockVerifier
    {
        private readonly INetworkParameters _parameters;
        private readonly IEnumerable<ITransactionValidator> _txnValidators;
        private readonly IEnumerable<ValidTransactionType> _validTxnTypes;
        private readonly IAddressEncoder _addressEncoder;
        private readonly ISignatureService _signatureService;

        public BlockVerifier(INetworkParameters parameters, IAddressEncoder addressEncoder, ISignatureService signatureService, IEnumerable<ITransactionValidator> txnValidators, IEnumerable<ValidTransactionType> validTxnTypes)
        {
            _parameters = parameters;
            _addressEncoder = addressEncoder;
            _signatureService = signatureService;
            _txnValidators = txnValidators;
            _validTxnTypes = validTxnTypes;
        }

        public bool Verify(Block block)
        {
            throw new NotImplementedException();
        }

        public bool VerifyContentThreshold(ICollection<byte[]> actual, ICollection<byte[]> expected)
        {
            throw new NotImplementedException();
        }

        public int VerifyTransaction(TransactionEnvelope transaction)
        {
            var result = 0;

            if (!_addressEncoder.IsValidAddress(transaction.Originator))
                return -1;

            if (_validTxnTypes.All(x => x.TransactionType != transaction.TransactionType))
                return -2;

            if (!_signatureService.VerifyTransaction(transaction))
                return -3;

            foreach (var validator in _txnValidators.Where(v => v.TransactionType == transaction.TransactionType))
                result = result & validator.Validate(transaction);

            return result;
        }
    }
}
