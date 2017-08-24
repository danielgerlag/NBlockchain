using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Services
{
    public class ByteArrayComparer : IComparer<byte[]>
    {
        public int Compare(byte[] x, byte[] y)
        {
            var len = Math.Min(x.Length, y.Length);
            for (var i = 0; i < len; i++)
            {
                var c = x[i].CompareTo(y[i]);
                if (c != 0)
                {
                    return c;
                }
            }

            return x.Length.CompareTo(y.Length);
        }
    }
}
