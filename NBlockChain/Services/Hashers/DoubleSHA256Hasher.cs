using NBlockChain.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NBlockChain.Services.Hashers
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
