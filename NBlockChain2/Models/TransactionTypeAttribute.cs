using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Models
{
    public class TransactionTypeAttribute : Attribute
    {
        public string TypeId { get; }

        public TransactionTypeAttribute(string typeId)
        {
            TypeId = typeId;
        }
    }
}
