using NBlockChain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockChain.Services
{
    public class MerkelNodeComparer : IComparer<MerkleNode>
    {

        private readonly IComparer<byte[]> _byteArrayComparer;

        public MerkelNodeComparer()
        {
            _byteArrayComparer = new ByteArrayComparer();
        }

        public int Compare(MerkleNode x, MerkleNode y)
        {
            return _byteArrayComparer.Compare(x.Value, y.Value);
        }
    }
}
