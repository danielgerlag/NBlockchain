using NBlockChain.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NBlockChain.Services.Hashers
{
    public class DoubleSHA256HasherSHA256Hasher : IHasher, IDisposable
    {
        private readonly HashAlgorithm _algorithm = SHA256.Create();

        public byte[] ComputeHash(byte[] input)
        {
            var pass1 = _algorithm.ComputeHash(input);
            return _algorithm.ComputeHash(pass1);
        }

        public void Dispose()
        {
            _algorithm.Dispose();
        }
    }
}
