using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Tests.Scenarios.Common
{
    [InstructionType("txn-v1")]
    public class TestInstruction : Instruction
    {
        public string Data { get; set; }
    }
}
