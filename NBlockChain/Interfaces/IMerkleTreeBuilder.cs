using System.Collections.Generic;
using System.Threading.Tasks;
using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface IMerkleTreeBuilder
    {
        Task<MerkleNode> BuildTree(ICollection<byte[]> nodes);
    }
}