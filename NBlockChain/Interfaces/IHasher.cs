using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Interfaces
{
    public interface IHasher
    {
        byte[] ComputeHash(byte[] input);

    }
}
