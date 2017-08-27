namespace NBlockchain.Interfaces
{
    public interface IAddressEncoder
    {
        string EncodeAddress(byte[] publicKey, byte type);
        byte[] ExtractPublicKey(string address);
        bool IsValidAddress(string address);
    }
}