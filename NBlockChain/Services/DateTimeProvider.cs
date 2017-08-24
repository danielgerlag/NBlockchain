using System;
using System.Collections.Generic;
using System.Text;
using NBlockchain.Interfaces;

namespace NBlockchain.Services
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public long UtcTicks => DateTime.UtcNow.Ticks;
    }
}
