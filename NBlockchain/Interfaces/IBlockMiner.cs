using System;
using System.Threading.Tasks;
using System.Threading;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IBlockMiner
    {
        void Start(KeyPair builderKeys, bool genesis);
        void Stop();
    }
}