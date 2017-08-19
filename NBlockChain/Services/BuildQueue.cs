using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NBlockChain.Interfaces;
using NBlockChain.Models;

namespace NBlockChain.Services
{
    public class BuildQueue : IBuildQueue
    {
        private readonly ConcurrentDictionary<uint, CancellationTokenSource> _cancelTokens = new ConcurrentDictionary<uint, CancellationTokenSource>();
        private readonly IBlockBuilder _blockBuilder;
        private readonly IBlockRepository _blockRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPeerNetwork _peerNetwork;

        private IBlockReceiver _receiver;
        
        private KeyPair _builderKeys;
        private IObserver<uint> _observer;

        public BuildQueue(IBlockBuilder blockBuilder, IBlockRepository blockRepository, IServiceProvider serviceProvider, IPeerNetwork peerNetwork)
        {
            _blockBuilder = blockBuilder;
            _blockRepository = blockRepository;
            _serviceProvider = serviceProvider;
            _peerNetwork = peerNetwork;
        }

        public void EnqueueBlock(uint height)
        {
            Task.Factory.StartNew(() => _observer?.OnNext(height));
        }

        public void CancelBlock(uint height)
        {
            if (_cancelTokens.TryRemove(height, out var cts))
            {
                cts.Cancel();
            }
        }

        public void Start(KeyPair builderKeys)
        {
            _receiver = _serviceProvider.GetService<IBlockReceiver>();
            _builderKeys = builderKeys;
            _observer = Observer.Create<uint>(BuildBlock);
        }

        public void Stop()
        {
            _observer = null;
        }

        public bool Running => _observer != null;

        private async void BuildBlock(uint height)
        {
            var prevBlockHeader = await _blockRepository.GetNewestBlockHeader();
            var cts = new CancellationTokenSource();
            _cancelTokens[height] = cts;

            if (prevBlockHeader == null)
                return;

            if (prevBlockHeader.Height < (height - 1))
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
                if (!cts.Token.IsCancellationRequested)
                    EnqueueBlock(height);

                return;
            }

            if (prevBlockHeader.Height != (height - 1))
                return;

            var block = await _blockBuilder.BuildBlock(prevBlockHeader.BlockId, height, _builderKeys, cts.Token);

            if (block != null)
            {
                if (block.Header.Status == BlockStatus.Confirmed)
                {
                    await _receiver.RecieveTail(block);
                    _peerNetwork.BroadcastTail(block);
                }
            }
        }
    }
}
