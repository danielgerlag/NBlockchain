using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface INodeHost : IBlockReceiver, ITransactionReceiver
    {
        Task SendTransaction(TransactionEnvelope transaction);
        Task BuildGenesisBlock(KeyPair builderKeys);
        void StartBuildingBlocks(KeyPair builderKeys);
        void StopBuildingBlocks();
    }
}