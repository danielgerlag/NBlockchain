using NBlockChain.Interfaces;
using NBlockChain.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace NBlockChain.Tests.Services
{
    public class MerkleTreeBuilderTests
    {
        private readonly MerkleTreeBuilder _subject;
        private readonly IHasher _hasher;

        public MerkleTreeBuilderTests()
        {
            _hasher = new SHA256Hasher();
            _subject = new MerkleTreeBuilder(_hasher);
        }

        [Fact]
        public void Test1()
        {
        }
    }
}
