using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Linq;

namespace NBlockchain.Models
{
    public abstract class Instruction
    {
        public byte[] InstructionId { get; set; }

        public byte[] OriginKey { get; set; }

        public byte[] PublicKey { get; set; }

        public byte[] Signature { get; set; }
        
        public abstract ICollection<byte[]> ExtractSignableElements();
    }
}
