using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBlockchain.Services
{
    public class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] x, byte[] y)
        {
            return x.SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj)
        {
            return (obj.Sum(x => x) + obj.Length).GetHashCode();
        }
    }
}
