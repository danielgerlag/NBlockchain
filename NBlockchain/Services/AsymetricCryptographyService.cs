using System;
using System.Collections.Generic;
using System.Text;
using NBlockchain.Interfaces;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace NBlockchain.Services
{
    public class AsymetricCryptographyService : IAsymetricCryptographyService
    {
        private readonly DerObjectIdentifier _curveId = SecObjectIdentifiers.SecP256r1;

        public byte[] GeneratePrivateKey()
        {
            return SecureRandom.GetNextBytes(SecureRandom.GetInstance("SHA256PRNG"), 32);
        }

        public byte[] BuildPrivateKeyFromPhrase(string phrase)
        {
            using (var hasher = System.Security.Cryptography.SHA256.Create())
            {
                var privateKey = hasher.ComputeHash(Encoding.Unicode.GetBytes(phrase));
                return privateKey;
            }
        }

        public byte[] GetPublicKey(byte[] privateKey)
        {
            var privKeyInt = new BigInteger(privateKey);
            var parameters = NistNamedCurves.GetByOid(_curveId);
            var qa = parameters.G.Multiply(privKeyInt);

            return qa.GetEncoded();
        }

        public byte[] Sign(byte[] data, byte[] privateKey)
        {
            var cp = GetPrivateKeyParameters(privateKey);
            var signer = SignerUtilities.GetSigner("ECDSA");
            signer.Init(true, cp);
            signer.BlockUpdate(data, 0, data.Length);
            return signer.GenerateSignature();
        }

        public bool Verify(byte[] data, byte[] sig, byte[] publicKey)
        {
            var cp = GetPublicKeyParams(publicKey);
            var signer = SignerUtilities.GetSigner("ECDSA");
            signer.Init(false, cp);
            signer.BlockUpdate(data, 0, data.Length);
            return signer.VerifySignature(sig);
        }

        private ECPrivateKeyParameters GetPrivateKeyParameters(byte[] privateKey)
        {
            var parameters = NistNamedCurves.GetByOid(_curveId);
            var privKeyInt = new BigInteger(privateKey);
            var dp = new ECDomainParameters(parameters.Curve, parameters.G, parameters.N);
            return new ECPrivateKeyParameters("ECDSA", privKeyInt, dp);
        }

        private ECPublicKeyParameters GetPublicKeyParams(byte[] publicKey)
        {
            var parameters = NistNamedCurves.GetByOid(_curveId);
            var dp = new ECDomainParameters(parameters.Curve, parameters.G, parameters.N);
            var q = parameters.Curve.DecodePoint(publicKey);
            return new ECPublicKeyParameters("ECDSA", q, dp);
        }
    }
}
