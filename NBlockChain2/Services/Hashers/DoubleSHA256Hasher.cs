using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using NBlockchain.Interfaces;

namespace NBlockchain.Services.Hashers
{
    public class DoubleSHA256HasherSHA256Hasher : IHasher
    {
        public byte[] ComputeHash(byte[] input)
        {
            using (var h = SHA256.Create())
            {
                return h.ComputeHash(h.ComputeHash(input));
            }
        }        
    }
}
