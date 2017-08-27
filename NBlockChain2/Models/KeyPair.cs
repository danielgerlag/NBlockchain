using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Models
{
    public class KeyPair
    {
        public byte[] PublicKey { get; set; }

        public byte[] PrivateKey { get; set; }
    }
}
