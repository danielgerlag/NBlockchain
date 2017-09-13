using NBlockchain.Interfaces;
using NBlockchain.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NBlockchain.Services
{
    public class ExpectedBlockList : IExpectedBlockList
    {
        private readonly Dictionary<byte[], DateTime> _expectedExpiries = new Dictionary<byte[], DateTime>(new ByteArrayEqualityComparer());
        private readonly AutoResetEvent _resetEvt = new AutoResetEvent(true);
        private readonly INetworkParameters _parameters;
        private readonly Timer _timer;

        public ExpectedBlockList(INetworkParameters parameters)
        {
            _parameters = parameters;
            _timer = new Timer(ExpireUnconfirmed, null, _parameters.BlockTime, _parameters.BlockTime);
        }

        public void ExpectNext(byte[] previousId)
        {
            _resetEvt.WaitOne();
            try
            {
                if (!_expectedExpiries.ContainsKey(previousId))
                    _expectedExpiries[previousId] = DateTime.MaxValue;
            }
            finally
            {
                _resetEvt.Set();
            }
        }

        public bool IsExpected(byte[] previousId)
        {
            _resetEvt.WaitOne();
            try
            {
                if (_expectedExpiries.ContainsKey(previousId))
                    return (_expectedExpiries[previousId] > DateTime.Now);
            }
            finally
            {
                _resetEvt.Set();
            }

            return false;
        }

        public void Confirm(byte[] previousId)
        {
            _resetEvt.WaitOne();
            try
            {
                if (_expectedExpiries.ContainsKey(previousId))
                    _expectedExpiries[previousId] = DateTime.Now.Add(_parameters.BlockTime);
            }
            finally
            {
                _resetEvt.Set();
            }
        }

        private void ExpireUnconfirmed(object state)
        {
            _resetEvt.WaitOne();
            try
            {
                var keys = new List<byte[]>(_expectedExpiries.Keys);
                foreach (var key in keys)
                {
                    if (_expectedExpiries[key] < DateTime.Now)
                        _expectedExpiries.Remove(key);
                }
            }
            finally
            {
                _resetEvt.Set();
            }
        }
        
    }
}
