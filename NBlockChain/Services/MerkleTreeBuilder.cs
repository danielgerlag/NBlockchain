using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBlockchain.Interfaces;
using NBlockchain.Models;

namespace NBlockchain.Services
{
    public class MerkleTreeBuilder : IMerkleTreeBuilder
    {

        private readonly IHasher _hasher;
        private readonly IComparer<byte[]> _byteArrayComparer;
        private readonly IComparer<MerkleNode> _merkleNodeComparer;

        public MerkleTreeBuilder(IHasher hasher)
        {
            _hasher = hasher;
            _byteArrayComparer = new ByteArrayComparer();
            _merkleNodeComparer = new MerkelNodeComparer();
        }

        public async Task<MerkleNode> BuildTree(ICollection<byte[]> nodes)
        {
            var sortedSet = new SortedSet<byte[]>(nodes, _byteArrayComparer);
            var next = new ConcurrentBag<MerkleNode>();

            Parallel.For(0, sortedSet.Count, i =>
            {
                if ((i % 2) == 0)
                {
                    var left = sortedSet.ElementAt(i);
                    var right = sortedSet.ElementAt(Math.Min(i + 1, sortedSet.Count - 1));
                    var combined = left.Concat(right);

                    var node = new MerkleNode()
                    {
                        Value = _hasher.ComputeHash(combined.ToArray()),
                        Left = new MerkleNode() { Value = _hasher.ComputeHash(left) },
                        Right = new MerkleNode() { Value = _hasher.ComputeHash(right) }
                    };
                    next.Add(node);
                }
            });

            return BuildBranches(new HashSet<MerkleNode>(next));
        }

        private MerkleNode BuildBranches(ICollection<MerkleNode> nodes)
        {
            ICollection<MerkleNode> current = new HashSet<MerkleNode>(nodes);

            while (current.Count > 1)
            {
                var sortedSet = new SortedSet<MerkleNode>(current, _merkleNodeComparer);
                var next = new ConcurrentBag<MerkleNode>();

                Parallel.For(0, sortedSet.Count, i =>
                {
                    if ((i % 2) == 0)
                    {
                        var left = sortedSet.ElementAt(i);
                        var right = sortedSet.ElementAt(Math.Min(i + 1, sortedSet.Count - 1));
                        var combined = left.Value.Concat(right.Value);

                        var node = new MerkleNode()
                        {
                            Value = _hasher.ComputeHash(combined.ToArray()),
                            Left = left,
                            Right = right
                        };
                        next.Add(node);
                    }
                });
                current = new HashSet<MerkleNode>(next);
            }

            return current.Single();
        }

    }
}
