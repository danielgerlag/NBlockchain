using System.Threading.Tasks;
using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface IBlockValidator
    {
        Task Validate(Block block);
    }
}