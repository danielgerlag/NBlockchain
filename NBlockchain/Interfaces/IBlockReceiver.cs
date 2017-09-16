using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IBlockReceiver
    {
        Task<PeerDataResult> RecieveBlock(Block block, bool tip);
    }

    public enum PeerDataResult { Ignore, Relay, Demerit }
}
