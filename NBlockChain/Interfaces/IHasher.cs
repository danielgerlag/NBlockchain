using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockChain.Interfaces
{
    public interface IHasher
    {
        byte[] ComputeHash(byte[] input);

    }
}
