using System;
using System.Collections.Generic;
using System.Text;
using NBlockchain.Interfaces;

namespace NBlockchain.Services
{
    public class HashTester : IHashTester
    {
        public bool TestHash(byte[] hash, uint difficulty)
        {
            var counter = difficulty;

            foreach (var b in hash)
            {
                var byteCounter = Math.Min(counter, 255);

                if (b > (255 - byteCounter))
                    return false;

                counter -= byteCounter;

                if (counter <= 0)
                    break;
            }

            return true;
        }
    }
}
