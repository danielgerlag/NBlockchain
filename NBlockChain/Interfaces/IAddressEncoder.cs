namespace NBlockChain.Interfaces
{
    public interface IAddressEncoder
    {
        string EncodeAddress(byte[] publicKey);
        byte[] ExtractPublicKeyAddress(string publicKey);
    }
}