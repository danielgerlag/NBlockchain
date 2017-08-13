using System.Threading.Tasks;
using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface INodeHost : IBlockReceiver, ITransactionReceiver
    {
        Task SendTransaction(TransactionEnvelope transaction);
        Task BuildGenesisBlock(KeyPair builderKeys);
        void StartBuildingBlocks(KeyPair builderKeys);
        void StopBuildingBlocks();
    }
}