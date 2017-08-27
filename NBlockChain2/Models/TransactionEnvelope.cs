using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace NBlockchain.Models
{
    public class TransactionEnvelope
    {
        public string TransactionType { get; set; }

        public Guid OriginKey{ get; set; }

        public string Originator { get; set; }

        public byte[] Signature { get; set; }

        public object Transaction { get; set; }

        public TransactionEnvelope()
        {
        }

        public TransactionEnvelope(object transaction)
        {
            Transaction = transaction;
        }

    }
}
