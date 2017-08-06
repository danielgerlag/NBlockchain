using NBlockChain.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NBlockChain.Services
{
    public class SHA256Hasher : IHasher, IDisposable
    {
        private readonly HashAlgorithm _algorithm = SHA256.Create();

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
