using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface IBlockReceiver
    {
        Task<PeerDataResult> RecieveBlock(Block block);

        Task<PeerDataResult> RecieveTail(Block block);
    }

    public enum PeerDataResult { Ignore, Relay, Demerit }
}
