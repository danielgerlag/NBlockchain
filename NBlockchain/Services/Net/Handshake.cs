using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Services.Net
{
    public class Handshake
    {
        public Guid NodeId { get; set; }
        public int Version { get; set; }
        public long Height { get; set; }

    }
}
