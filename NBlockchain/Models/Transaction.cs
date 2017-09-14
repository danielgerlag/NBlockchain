using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace NBlockchain.Models
{
    public class Transaction
    {
        public byte[] TransactionId { get; set; }
        
        public ICollection<Instruction> Instructions { get; set; } = new HashSet<Instruction>();

        public Transaction()
        {
        }

        public Transaction(ICollection<Instruction> instructions)
        {
            Instructions = instructions;
        }
    }
}
