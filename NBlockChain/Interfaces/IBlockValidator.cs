using System.Threading.Tasks;
using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface IBlockValidator<T> where T : AbstractTransaction
    {
        Task Validate(Block<T> block);
    }
}