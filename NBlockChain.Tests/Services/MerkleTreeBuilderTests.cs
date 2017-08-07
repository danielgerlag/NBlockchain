using NBlockChain.Interfaces;
using NBlockChain.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using FluentAssertions;

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
            var source = new List<byte[]>();
            source.Add(new byte[] { 0x3 });
            source.Add(new byte[] { 0x1 });
            source.Add(new byte[] { 0x2 });            
            source.Add(new byte[] { 0x4 });
            source.Add(new byte[] { 0x5 });
            source.Add(new byte[] { 0x6 });
            source.Add(new byte[] { 0x7 });

            var rootNode = _subject.BuildTree(source);

            rootNode.Should().NotBeNull();
        }
    }
}
