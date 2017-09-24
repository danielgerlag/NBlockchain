using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IBlockchainNode
    {
        Task SendTransaction(Transaction transaction);
        Task<PeerDataResult> RecieveBlock(Block block);
        Task<PeerDataResult> RecieveTransaction(Transaction transaction);
    }
}