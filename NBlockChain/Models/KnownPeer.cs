using System;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Models
{
    public class KnownPeer
    {
        public string ConnectionString { get; set; }
        public DateTime LastContact { get; set; }

    }
}
