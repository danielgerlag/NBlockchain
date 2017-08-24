namespace ScratchPad
{
    public interface ITransactionRepository
    {
        decimal GetAccountBalance(string account);
    }
}