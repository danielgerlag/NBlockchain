using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IInboundBlockQueue
    {
        Block Dequeue();
        void Enqueue(Block data);
    }
}