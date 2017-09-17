using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace NBlockchain.Services
{
    public abstract class InboundQueue<T>
        where T : class
    {
        private ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        public void Enqueue(T data)
        {
            _queue.Enqueue(data);
        }

        public T Dequeue()
        {
            if (_queue.TryDequeue(out var result))
                return result;

            return null;
        }

    }
}
