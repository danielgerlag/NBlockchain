using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Models
{
    public class ValidTransactionType
    {
        public string TransactionType { get; private set; }

        public Type ClassType { get; private set; }

        public ValidTransactionType(string transactionType, Type classType)
        {
            TransactionType = transactionType;
            ClassType = classType;
        }
    }
}
