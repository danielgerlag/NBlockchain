using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using Newtonsoft.Json;

namespace NBlockchain.Services
{
    public class DefaultSignatureService : ISignatureService
    {
        private readonly IAddressEncoder _addressEncoder;
        private readonly IAsymetricCryptographyService _asymetricCryptography;

        public DefaultSignatureService(IAddressEncoder addressEncoder, IAsymetricCryptographyService asymetricCryptography)
        {
            _addressEncoder = addressEncoder;
            _asymetricCryptography = asymetricCryptography;
        }

        public KeyPair GenerateKeyPair()
        {
            var privateKey = _asymetricCryptography.GeneratePrivateKey();
            var publicKey = _asymetricCryptography.GetPublicKey(privateKey);

            return new KeyPair()
            {
                PrivateKey = privateKey,
                PublicKey = publicKey
            };
        }

        public KeyPair GetKeyPairFromPhrase(string phrase)
        {
            var privateKey = _asymetricCryptography.BuildPrivateKeyFromPhrase(phrase);
            var publicKey = _asymetricCryptography.GetPublicKey(privateKey);

            return new KeyPair()
            {
                PrivateKey = privateKey,
                PublicKey = publicKey
            };
        }

        public void SignInstruction(Instruction instruction, byte[] privateKey)
        {
            instruction.OriginKey = GenerateOriginKey();
            instruction.InstructionId = ResolveInstructionId(instruction);
            instruction.Signature = _asymetricCryptography.Sign(instruction.InstructionId, privateKey);
        }

        public bool VerifyInstruction(Instruction instruction)
        {
            if (instruction.Signature == null)
                return false;

            if (!instruction.InstructionId.SequenceEqual(ResolveInstructionId(instruction)))
                return false;

            return _asymetricCryptography.Verify(instruction.InstructionId, instruction.Signature, instruction.PublicKey);
        }
        
        private static byte[] ResolveInstructionId(Instruction instruction)
        {
            using (var h = SHA256.Create())
            {
                var items = instruction.ExtractSignableElements();
                items.Add(instruction.OriginKey);
                items.Add(instruction.PublicKey);

                var data = items.SelectMany(x => x).ToArray();

                return h.ComputeHash(data);
            }
        }

        private static byte[] GenerateOriginKey()
        {
            return Guid.NewGuid().ToByteArray();
        }

    }
}
