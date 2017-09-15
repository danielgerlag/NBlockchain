namespace NBlockchain.Interfaces
{
    public interface IAddressEncoder
    {
        string EncodeAddress(byte[] publicKey, byte type);
        byte[] ExtractPublicKeyHash(string address);
        byte[] HashPublicKey(byte[] publicKey);
        bool IsValidAddress(string address);
    }
}