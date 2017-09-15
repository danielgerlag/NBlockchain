using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using NBlockchain.Services.Database;

namespace NBlockchain.MongoDB.Models
{
    public class PersistedTransaction
    {
        public byte[] TransactionId { get; set; }

        public ICollection<PersistedInstruction> Instructions { get; set; } = new HashSet<PersistedInstruction>();

        public PersistedTransaction()
        {
        }

        public PersistedTransaction(Transaction txn, IAddressEncoder addressEncoder)
        {
            TransactionId = txn.TransactionId;
            foreach (var ins in txn.Instructions)
                Instructions.Add(new PersistedInstruction(ins, addressEncoder));
        }

        public Transaction ToTransaction()
        {
            var result = new Transaction();
            result.TransactionId = TransactionId;
            foreach (var ins in Instructions)
                result.Instructions.Add(ins.Entity);

            return result;
        }

    }
}