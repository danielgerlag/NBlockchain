using NBlockchain.Interfaces;
using NBlockchain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Services
{
    public class InboundBlockQueue : InboundQueue<Block>, IInboundBlockQueue
    {
    }
}
