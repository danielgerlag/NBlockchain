using NBlockchain.Interfaces;
using NBlockchain.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using FluentAssertions;
using FakeItEasy;

namespace NBlockchain.Tests.Services
{
    public class MerkleTreeBuilderTests
    {
        private readonly MerkleTreeBuilder _subject;
        private readonly IHasher _hasher;

        public MerkleTreeBuilderTests()
        {
            _hasher = A.Fake<IHasher>();
            _subject = new MerkleTreeBuilder(_hasher);
        }

        
        [Fact]
        public void should_compute_merkle_root()
        {
            A.CallTo(() => _hasher.ComputeHash(A<byte[]>.Ignored))
                .ReturnsLazily<byte[], byte[]>(input => input);

            var source = new List<byte[]>();
            source.Add(new byte[] { 0x3 });
            source.Add(new byte[] { 0x1 });
            source.Add(new byte[] { 0x2 });            
            source.Add(new byte[] { 0x4 });
            source.Add(new byte[] { 0x5 });
            source.Add(new byte[] { 0x6 });
            source.Add(new byte[] { 0x7 });

            var rootNode = _subject.BuildTree(source).Result;

            rootNode.Should().NotBeNull();
            rootNode.Value.Should().BeEquivalentTo(new byte[] { 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x7 });
        }
    }
}