using NBlockChain.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using NBlockChain.Interfaces;

namespace NBlockChain.Services
{
    public class DefaultSignatureService : ISignatureService
    {

        private readonly ECCurve _curve = ECCurve.NamedCurves.brainpoolP512t1;
        private readonly HashAlgorithmName _hashAlgorithm = HashAlgorithmName.SHA512;

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

        public byte[] SignData(byte[] data, byte[] privateKey)
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

                return dsa.SignData(data, _hashAlgorithm);
            }
        }

        public bool VerifyData(byte[] data, byte[] signature, byte[] publicKey)
        {
            using (var dsa = ECDsa.Create(_curve))
            {
                dsa.ImportParameters(new ECParameters()
                {
                    Curve = _curve,                    
                    Q = new ECPoint()
                    {
                        X = publicKey.Take(publicKey.Length / 2).ToArray(),
                        Y = publicKey.Skip(publicKey.Length / 2).Take(publicKey.Length / 2).ToArray()
                    }
                });

                return dsa.VerifyData(data, signature, _hashAlgorithm);
            }
        }

    }
}
