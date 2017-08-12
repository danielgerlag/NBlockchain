using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface IBlockReceiver
    {
        Task RecieveBlock(Block block);

        Task RecieveTail(Block block);
    }
}
