using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockChain.Models
{
    public class Block<TEvent>
        where TEvent : AbstractEvent
    {
        public BlockHeader Header { get; set; }
        public BlockStatus Status { get; set; }
        public ICollection<TEvent> Events { get; set; } = new HashSet<TEvent>();
    }

    public enum BlockStatus
    {
        Open = 0,
        Closed = 1,
        Verified = 2
    }
}
