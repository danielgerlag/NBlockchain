using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using NBlockchain.Interfaces;
using Wiry.Base32;

namespace NBlockchain.Services
{
    public class AddressEncoder : IAddressEncoder
    {
        private const int ChecksumLength = 4;
        public AddressEncoder()
        {
        }

        public string EncodeAddress(byte[] publicKey, byte type)
        {
            var result = new List<byte> {type};
            result.AddRange(HashPublicKey(publicKey));
            result.AddRange(CalculateCheckSum(result.ToArray()));

            return Base32Encoding.ZBase32.GetString(result.ToArray());
        }

        public byte[] ExtractPublicKeyHash(string address)
        {
            var raw = Base32Encoding.ZBase32.ToBytes(address);
            return raw.Skip(1).Take(raw.Length - (1 + ChecksumLength)).ToArray();
        }

        public byte[] HashPublicKey(byte[] publicKey)
        {
            using (var hasher = SHA1.Create())
            {
                return hasher.ComputeHash(publicKey);
            }
        }
        
        public bool IsValidAddress(string address)
        {
            var raw = Base32Encoding.ZBase32.ToBytes(address);
            return (raw.Skip(raw.Count() - ChecksumLength).SequenceEqual(CalculateCheckSum(raw.Take(raw.Count() - ChecksumLength).ToArray())));
        }

        private static byte[] CalculateCheckSum(byte[] data)
        {
            using (var hasher = SHA256.Create())
            {
                return hasher.ComputeHash(data).Take(ChecksumLength).ToArray();
            }
        }
    }
}
