using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Models
{
    public class ValidInstructionType
    {
        public string InstructionType { get; private set; }

        public Type ClassType { get; private set; }

        public ValidInstructionType(string instructionType, Type classType)
        {
            InstructionType = instructionType;
            ClassType = classType;
        }
    }
}
