using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using Newtonsoft.Json;

namespace NBlockchain.Services
{
    public class DefaultSignatureService : ISignatureService
    {
        private readonly IAddressEncoder _addressEncoder;
        private readonly ITransactionKeyResolver _transactionKeyResolver;
        private readonly ECCurve _curve = ECCurve.NamedCurves.nistP256;
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

        public void SignInstruction(Instruction instruction, byte[] privateKey)
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
                instruction.OriginKey = GenerateOriginKey();
                instruction.InstructionId = ResolveInstructionId(instruction);
                var data = instruction.ExtractSignableElements();

                instruction.Signature = dsa.SignData(data, _hashAlgorithm);
            }
        }

        public bool VerifyInstruction(Instruction instruction)
        {
            if (instruction.Signature == null)
                return false;
            
            using (var dsa = ECDsa.Create(_curve))
            {
                dsa.ImportParameters(new ECParameters()
                {
                    Curve = _curve,                    
                    Q = new ECPoint()
                    {
                        X = instruction.PublicKey.Take(instruction.PublicKey.Length / 2).ToArray(),
                        Y = instruction.PublicKey.Skip(instruction.PublicKey.Length / 2).Take(instruction.PublicKey.Length / 2).ToArray()
                    }
                });

                if (!instruction.InstructionId.SequenceEqual(ResolveInstructionId(instruction)))
                    return false;

                var data = instruction.ExtractSignableElements();

                return dsa.VerifyData(data, instruction.Signature, _hashAlgorithm);
            }
        }
        
        private static byte[] ResolveInstructionId(Instruction instruction)
        {
            using (var h = SHA256.Create())
            {
                return h.ComputeHash(instruction.OriginKey.Concat(instruction.PublicKey).ToArray());
            }
        }

        private static byte[] GenerateOriginKey()
        {
            return Guid.NewGuid().ToByteArray();
        }

    }
}
