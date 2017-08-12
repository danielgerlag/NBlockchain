using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace NBlockChain.Models
{
    public class TransactionEnvelope
    {
        public string TransactionType { get; set; }

        public Guid OriginKey{ get; set; }

        public string Originator { get; set; }

        public byte[] Signature { get; set; }

        public JObject Transaction { get; set; }

        public TransactionEnvelope()
        {
        }

        public TransactionEnvelope(object transaction)
        {
            Transaction = JObject.FromObject(transaction);
        }

    }
}
