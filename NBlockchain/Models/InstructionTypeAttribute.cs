using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Models
{
    public class InstructionTypeAttribute : Attribute
    {
        public string TypeIdentifier { get; }

        public InstructionTypeAttribute(string typeIdentifier)
        {
            TypeIdentifier = typeIdentifier;
        }
    }
}
