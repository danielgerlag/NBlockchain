using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NBlockChain.Interfaces;
using NBlockChain.Models;

namespace NBlockChain.Services
{
    public class OpenBlockService
    {

        private readonly NetworkParameters _networkParameters;
        private readonly IHasher _hasher;
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(true);
        private readonly IDictionary<byte[], AbstractEvent> _pendingEvents;

        public OpenBlockService(IHasher hasher, NetworkParameters networkParameters)
        {
            _networkParameters = networkParameters;
            _hasher = hasher;
            _pendingEvents = new SortedDictionary<byte[], AbstractEvent>();
        }

        public void AddEvent(AbstractEvent newEvent)
        {
            var hash = _hasher.ComputeHash(newEvent.GetRawData());
            _resetEvent.WaitOne();
            try
            {
                _pendingEvents.Add(hash, newEvent);
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public void CloseBlock(DateTime startTime, DateTime endTime)
        {            
            _resetEvent.WaitOne();
            try
            {
                var seletedEvents = _pendingEvents.Where(x => x.Value.Timestamp >= startTime.Ticks && x.Value.Timestamp < endTime.Ticks);
                
            }
            finally
            {
                _resetEvent.Set();
            }
        }

    }
}
