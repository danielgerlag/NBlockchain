namespace ScratchPad
{
    public interface ICustomTransactionRepository
    {
        decimal GetAccountBalance(string account);
    }
}