using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IReceiver
    {
        Task<PeerDataResult> RecieveBlock(Block block);
        Task<PeerDataResult> RecieveTransaction(Transaction transaction);
        event ReceiveBlock OnReceiveBlock;
        event RecieveTransaction OnRecieveTransaction;
    }

    public enum PeerDataResult { Ignore, Relay, Demerit }

    public delegate Task<PeerDataResult> ReceiveBlock(Block block);
    public delegate Task<PeerDataResult> RecieveTransaction(Transaction transaction);
}
