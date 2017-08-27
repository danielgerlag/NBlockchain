using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NBlockchain.Interfaces;
using NBlockchain.Models;

namespace NBlockchain.Services
{
    public class ProofOfWorkBlockNotary : IBlockNotary
    {
        private readonly IHasher _hasher;
        private readonly INetworkParameters _networkParameters;
        private readonly IHashTester _hashTester;
        private readonly AutoResetEvent _lock = new AutoResetEvent(true);

        public ProofOfWorkBlockNotary(IHasher hasher, INetworkParameters networkParameters, IHashTester hashTester)
        {
            _hasher = hasher;
            _networkParameters = networkParameters;
            _hashTester = hashTester;
        }


        public async Task ConfirmBlock(Block block, CancellationToken cancellationToken)
        {
            long counter = 0;
            var cancellationTokenSource = new CancellationTokenSource();
            var innerCancellationToken = cancellationTokenSource.Token;

            var opts = new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                BoundedCapacity = Environment.ProcessorCount + 1
            };
            
            var actionBlock = new ActionBlock<long>(nonce => VerifyForNonce(block.Header, nonce, cancellationTokenSource), opts);

            while ((!innerCancellationToken.IsCancellationRequested) && (!cancellationToken.IsCancellationRequested))
            {
                SpinWait.SpinUntil(() => actionBlock.InputCount == 0);
                actionBlock.Post(counter);
                counter++;                        
            }

            await Task.Yield();
        }

        private void VerifyForNonce(BlockHeader header, long nonce, CancellationTokenSource cancellationTokenSource)
        {            
            var seed = header.CombineHashableElementsWithNonce(nonce);
            var hash = _hasher.ComputeHash(seed);
            
            if (_hashTester.TestHash(hash, _networkParameters.Difficulty))
            {
                _lock.WaitOne();
                try
                {
                    if (header.Status == BlockStatus.Unconfirmed)
                    {
                        header.BlockId = hash;
                        header.Nonce = nonce;
                        header.Status = BlockStatus.Confirmed;
                    }
                }
                finally
                {
                    _lock.Set();
                    cancellationTokenSource.Cancel();
                }
            }
        }
    }
}
