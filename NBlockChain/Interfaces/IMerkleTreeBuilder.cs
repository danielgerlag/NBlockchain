using System.Collections.Generic;
using System.Threading.Tasks;
using NBlockchain.Models;

namespace NBlockchain.Interfaces
{
    public interface IMerkleTreeBuilder
    {
        Task<MerkleNode> BuildTree(ICollection<byte[]> nodes);
    }
}