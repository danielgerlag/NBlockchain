using NBlockChain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScratchPad
{
    public class TestTransaction : AbstractTransaction
    {

        public string Message { get; set; }

        public override byte[] GetRawData()
        {
            return ASCIIEncoding.ASCII.GetBytes(Message);
        }
    }
}
