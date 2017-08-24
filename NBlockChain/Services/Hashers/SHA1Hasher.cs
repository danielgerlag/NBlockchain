using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using NBlockchain.Interfaces;

namespace NBlockchain.Services.Hashers
{
    public class SHA1Hasher : IHasher
    {
        public byte[] ComputeHash(byte[] input)
        {
            using (var h = SHA1.Create())
            {
                return h.ComputeHash(input);
            }
        }
        
    }
}
