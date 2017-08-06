using NBlockChain.Interfaces;
using NBlockChain.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NBlockChain.Services
{
    public class MerkleTreeBuilder
    {

        private readonly IHasher _hasher;

        public MerkleTreeBuilder(IHasher hasher)
        {
            _hasher = hasher;
        }

        public MerkleNode BuildTree(ICollection<byte[]> nodes)
        {
            var sortedSet = new SortedSet<byte[]>(nodes);
            var next = new ConcurrentBag<MerkleNode>();

            Parallel.For(0, sortedSet.Count - 2, i =>
            {
                if ((i % 2) == 0)
                {
                    var left = sortedSet.ElementAt(i);
                    var right = sortedSet.ElementAt(Math.Min(i + 1, sortedSet.Count - 1));
                    var combined = left.Concat(right);
                                        
                    var node = new MerkleNode()
                    {
                        Value = _hasher.ComputeHash(combined.ToArray()),
                        Left = new MerkleNode() { Value = left },
                        Right = new MerkleNode() { Value = right }
                    };
                    next.Add(node);
                }
            });

            return BuildTree(new HashSet<MerkleNode>(next));
        }

        private MerkleNode BuildTree(ICollection<MerkleNode> nodes)
        {
            ICollection<MerkleNode> current = new HashSet<MerkleNode>(nodes);

            while (current.Count > 1)
            {
                var sortedSet = new SortedSet<MerkleNode>(current);
                var next = new ConcurrentBag<MerkleNode>();

                Parallel.For(0, sortedSet.Count - 2, i =>
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
