﻿using NBlockChain.Models;

namespace NBlockChain.Interfaces
{
    public interface IBuildQueue
    {
        void CancelBlock(uint height);
        void EnqueueBlock(uint height);
        void Start(KeyPair builderKeys);
        void Stop();

        bool Running { get; }
    }
}