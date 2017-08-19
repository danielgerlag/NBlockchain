using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockChain.Models
{
    public class PeerNode
    {
        public Guid NodeId { get; set; }
        public string ConnectionString { get; set; }
        public DateTime LastContact { get; set; }

    }
}
