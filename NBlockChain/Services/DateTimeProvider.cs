using System;
using System.Collections.Generic;
using System.Text;
using NBlockChain.Interfaces;

namespace NBlockChain.Services
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public long UtcTicks => DateTime.UtcNow.Ticks;
    }
}
