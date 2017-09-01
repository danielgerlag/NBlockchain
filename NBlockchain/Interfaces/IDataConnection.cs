using LiteDB;

namespace NBlockchain.Interfaces
{
    public interface IDataConnection
    {
        LiteDatabase Database { get; }
    }
}