namespace NBlockchain.Interfaces
{
    public interface IAsymetricCryptographyService
    {
        byte[] BuildPrivateKeyFromPhrase(string phrase);
        byte[] GeneratePrivateKey();
        byte[] GetPublicKey(byte[] privateKey);
        byte[] Sign(byte[] data, byte[] privateKey);
        bool Verify(byte[] data, byte[] sig, byte[] publicKey);
    }
}