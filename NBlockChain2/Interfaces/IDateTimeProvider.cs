namespace NBlockchain.Interfaces
{
    public interface IDateTimeProvider
    {
        long UtcTicks { get; }
    }
}