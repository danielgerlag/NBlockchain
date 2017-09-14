﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NBlockchain.Interfaces
{
    public interface IInstructionRepository
    {
        Task<bool> HaveInstruction(byte[] instructionId);
    }
}
