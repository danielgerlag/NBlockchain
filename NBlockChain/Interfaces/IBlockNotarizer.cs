using System.Threading.Tasks;
using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface IBlockNotarizer
    {
        Task Notarize(Block block);
    }
}