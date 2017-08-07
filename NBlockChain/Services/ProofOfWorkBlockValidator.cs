using NBlockChain.Interfaces;
using NBlockChain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NBlockChain.Services
{
    public class ProofOfWorkBlockValidator<T> : IBlockValidator<T>
        where T : AbstractTransaction
    {

        private readonly IHasher _hasher;
        private readonly INetworkParameters _networkParameters;
        private readonly AutoResetEvent _lock = new AutoResetEvent(true);

        public ProofOfWorkBlockValidator(IHasher hasher, INetworkParameters networkParameters)
        {
            _hasher = hasher;
            _networkParameters = networkParameters;
        }


        public async Task Validate(Block<T> block)
        {
            long counter = 0;
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var opts = new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                BoundedCapacity = Environment.ProcessorCount + 1
            };
            
            var actionBlock = new ActionBlock<long>(nonce => VerifyForNonce(block.Header, nonce, cancellationTokenSource), opts);

            while (!cancellationToken.IsCancellationRequested)
            {
                SpinWait.SpinUntil(() => actionBlock.InputCount == 0);
                actionBlock.Post(counter);
                counter++;                        
            }

            await Task.Yield();
        }

        private void VerifyForNonce(BlockHeader header, long nonce, CancellationTokenSource cancellationTokenSource)
        {            
            var seed = GetHashSeed(header, nonce);
            var hash = _hasher.ComputeHash(seed);

            Console.WriteLine($"nonce: {nonce} hash: {BitConverter.ToString(hash)}");

            if (TestHash(hash, _networkParameters.Difficulty))
            {
                Console.WriteLine($"Accepted -> nonce: {nonce} hash: {BitConverter.ToString(hash)}");
                _lock.WaitOne();
                try
                {
                    header.BlockID = hash;
                    header.Nonce = nonce;
                }
                finally
                {
                    _lock.Set();
                    cancellationTokenSource.Cancel();
                }
            }
        }

        private static byte[] GetHashSeed(BlockHeader header, long nonce)
        {
            return header.MerkelRoot
                .Concat(header.PreviousBlock)
                .Concat(BitConverter.GetBytes(header.Version))
                .Concat(BitConverter.GetBytes(nonce))
                .ToArray();
        }

        private static bool TestHash(byte[] hash, uint difficulty)
        {
            var counter = difficulty;            

            foreach (var b in hash)
            {
                var byteCounter = Math.Max(255, Math.Min(counter, 255));

                if (b > (255 - byteCounter))
                    return false;

                counter -= Math.Min(255, difficulty);

                if (counter <= 0)
                    break;
            }

            return true;
        }

    }
}
