using NBlockchain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using NBlockchain.Models;
using System.Threading.Tasks;

namespace NBlockchain.Services
{
    public class Receiver : IReceiver
    {
        public event ReceiveBlock OnReceiveBlock;
        public event RecieveTransaction OnRecieveTransaction;

        public Task<PeerDataResult> RecieveBlock(Block block)
        {
            return OnReceiveBlock?.Invoke(block);
        }

        public Task<PeerDataResult> RecieveTransaction(Transaction transaction)
        {
            return OnRecieveTransaction?.Invoke(transaction);
        }
    }
}
