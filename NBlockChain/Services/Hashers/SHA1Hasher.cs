using NBlockChain.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NBlockChain.Services.Hashers
{
    public class SHA1Hasher : IHasher, IDisposable
    {
        private readonly HashAlgorithm _algorithm = SHA1.Create();

        public byte[] ComputeHash(byte[] input)
        {            
            return _algorithm.ComputeHash(input);
        }

        public void Dispose()
        {
            _algorithm.Dispose();
        }
    }
}
