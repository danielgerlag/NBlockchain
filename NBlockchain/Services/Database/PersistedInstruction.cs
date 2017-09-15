using LiteDB;
using NBlockchain.Models;

namespace NBlockchain.Services.Database
{
    public class PersistedInstruction : PersistedEntity<Instruction, ObjectId, InstructionStatistics>
    {
        public byte[] BlockId { get; set; }

        public byte[] TransactionId { get; set; }

        public PersistedInstruction()
        {
        }

        public PersistedInstruction(byte[] blockId, byte[] transactionId, Instruction instruction, byte[] publicKeyHash)
        {
            Entity = instruction;
            BlockId = blockId;
            TransactionId = transactionId;
            Statistics = new InstructionStatistics()
            {
                PublicKeyHash = publicKeyHash
            };
        }
    }
}