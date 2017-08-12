using NBlockChain.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using NBlockChain.Interfaces;
using Newtonsoft.Json;

namespace NBlockChain.Services
{
    public class DefaultSignatureService : ISignatureService
    {
        private readonly IAddressEncoder _addressEncoder;
        private readonly ITransactionKeyResolver _transactionKeyResolver;
        private readonly ECCurve _curve = ECCurve.NamedCurves.brainpoolP192t1;
        private readonly HashAlgorithmName _hashAlgorithm = HashAlgorithmName.SHA512;

        public DefaultSignatureService(IAddressEncoder addressEncoder, ITransactionKeyResolver transactionKeyResolver)
        {
            _addressEncoder = addressEncoder;
            _transactionKeyResolver = transactionKeyResolver;
        }

        public KeyPair GenerateKeyPair()
        {
            using (var dsa = ECDsa.Create(_curve))
            {
                var parameters = dsa.ExportParameters(true);
                var publicKey = parameters.Q.X.Concat(parameters.Q.Y).ToArray();
                return new KeyPair()
                {
                    PrivateKey = parameters.D.Concat(publicKey).ToArray(),
                    PublicKey = publicKey
                };
            }
        }

        public void SignTransaction(TransactionEnvelope transaction, byte[] privateKey)
        {
            using (var dsa = ECDsa.Create(_curve))
            {                
                dsa.ImportParameters(new ECParameters()
                {
                    Curve = _curve,
                    D = privateKey.Take(privateKey.Length / 3).ToArray(),
                    Q = new ECPoint()
                    {
                        X = privateKey.Skip(privateKey.Length / 3).Take(privateKey.Length / 3).ToArray(),
                        Y = privateKey.Skip((privateKey.Length / 3) * 2).Take(privateKey.Length / 3).ToArray()
                    }
                });

                var data = ExtractSignableElements(transaction);

                transaction.Signature = dsa.SignData(data, _hashAlgorithm);
            }
        }

        public bool VerifyTransaction(TransactionEnvelope transaction)
        {
            if (transaction.Signature == null)
                return false;

            if (!_addressEncoder.IsValidAddress(transaction.Originator))
                return false;

            var pubKey = _addressEncoder.ExtractPublicKey(transaction.Originator);

            using (var dsa = ECDsa.Create(_curve))
            {
                dsa.ImportParameters(new ECParameters()
                {
                    Curve = _curve,                    
                    Q = new ECPoint()
                    {
                        X = pubKey.Take(pubKey.Length / 2).ToArray(),
                        Y = pubKey.Skip(pubKey.Length / 2).Take(pubKey.Length / 2).ToArray()
                    }
                });

                var data = ExtractSignableElements(transaction);

                return dsa.VerifyData(data, transaction.Signature, _hashAlgorithm);
            }
        }

        private byte[] ExtractSignableElements(TransactionEnvelope txn)
        {
            var txnStr = txn.Transaction.ToString(Formatting.None);

            var result = txn.OriginKey.ToByteArray()
                .Concat(Encoding.Unicode.GetBytes(txn.Originator))
                .Concat(Encoding.Unicode.GetBytes(txnStr));

            return result.ToArray();
        }

    }
}
