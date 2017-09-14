using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using NBlockchain.Services.Database;

namespace NBlockchain.MongoDB.Models
{
    public class PersistedInstruction : PersistedEntity<Instruction, byte[], InstructionStatistics>
    {
        public PersistedInstruction(Instruction entity, IAddressEncoder addressEncoder) 
            : base(entity)
        {
            Id = entity.InstructionId;
            Statistics.PublicKeyHash = addressEncoder.HashPublicKey(entity.PublicKey);
        }
    }
}
