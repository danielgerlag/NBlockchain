using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using NBlockchain.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace NBlockchain.Tests.Services
{
    public class BlockchainNodeTests
    {
        private readonly BlockchainNode _subject;
        private readonly IReceiver _receiver;

        private readonly INetworkParameters _parameters;
        private readonly IBlockRepository _blockRepository;
        private readonly IBlockVerifier _blockVerifier;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IForkRebaser _forkRebaser;        
        private readonly IPeerNetwork _peerNetwork;
        private readonly IUnconfirmedTransactionPool _unconfirmedTransactionPool;
        private readonly IDifficultyCalculator _difficultyCalculator;

        public BlockchainNodeTests()
        {
            _receiver = A.Fake<IReceiver>();
            _parameters = new StaticNetworkParameters() { BlockTime = TimeSpan.FromSeconds(1) };
            _loggerFactory = A.Fake<ILoggerFactory>();
            _blockRepository = A.Fake<IBlockRepository>();
            _blockVerifier = A.Fake<IBlockVerifier>();
            _forkRebaser = A.Fake<IForkRebaser>();
            _peerNetwork = A.Fake<IPeerNetwork>();
            _unconfirmedTransactionPool = A.Fake<IUnconfirmedTransactionPool>();
            _difficultyCalculator = A.Fake<IDifficultyCalculator>();
            
            _subject = new BlockchainNode(_blockRepository, _blockVerifier, _receiver, _loggerFactory, _forkRebaser, _parameters, _unconfirmedTransactionPool, _peerNetwork, _difficultyCalculator);
            _subject.PollTimer.Dispose();
        }


        [Fact]
        public async void should_verify_incoming_transaction()
        {            
            await _subject.RecieveTransaction(new Transaction() { TransactionId = new byte[0] });

            A.CallTo(() => _blockVerifier.VerifyTransaction(A<Transaction>.Ignored, A<ICollection<Transaction>>.Ignored))
                .MustHaveHappened();
        }

        [Fact]
        public async void should_ignore_invalid_incoming_transaction()
        {
            //arrange
            A.CallTo(() => _blockVerifier.VerifyTransaction(A<Transaction>.Ignored, A<ICollection<Transaction>>.Ignored))
                .Returns(-1);

            //act
            var result = await _subject.RecieveTransaction(new Transaction() { TransactionId = new byte[0] });

            //assert
            A.CallTo(() => _blockVerifier.VerifyTransaction(A<Transaction>.Ignored, A<ICollection<Transaction>>.Ignored))
                .MustHaveHappened();

            A.CallTo(() => _unconfirmedTransactionPool.Add(A<Transaction>.Ignored))
                .MustNotHaveHappened();

            result.Should().Be(PeerDataResult.Ignore);
        }

        [Fact]
        public async void should_add_valid_incoming_transaction_to_unconfirmed_pool()
        {
            //arrange
            A.CallTo(() => _blockVerifier.VerifyTransaction(A<Transaction>.Ignored, A<ICollection<Transaction>>.Ignored))
                .Returns(0);

            A.CallTo(() => _unconfirmedTransactionPool.Add(A<Transaction>.Ignored))
                .Returns(true);

            //act
            var result = await _subject.RecieveTransaction(new Transaction() { TransactionId = new byte[0] });

            //assert
            A.CallTo(() => _blockVerifier.VerifyTransaction(A<Transaction>.Ignored, A<ICollection<Transaction>>.Ignored))
                .MustHaveHappened();

            A.CallTo(() => _unconfirmedTransactionPool.Add(A<Transaction>.Ignored))
                .MustHaveHappened();

            result.Should().Be(PeerDataResult.Relay);
        }

        [Fact]
        public async void should_locally_process_outcoming_transaction()
        {
            //arrange
            var txn = new Transaction() { TransactionId = new byte[0] };

            //act
            await _subject.SendTransaction(txn);

            //assert
            A.CallTo(() => _receiver.RecieveTransaction(txn))
                .MustHaveHappened();
        }

        [Fact]
        public async void should_not_relay_invalid_outcoming_transaction()
        {
            //arrange
            var txn = new Transaction() { TransactionId = new byte[0] };
            A.CallTo(() => _receiver.RecieveTransaction(txn))
                .Returns(PeerDataResult.Ignore);

            //act
            await _subject.SendTransaction(txn);

            //assert
            A.CallTo(() => _peerNetwork.BroadcastTransaction(txn))
                .MustNotHaveHappened();
        }

        [Fact]
        public async void should_relay_valid_outcoming_transaction()
        {
            //arrange
            var txn = new Transaction() { TransactionId = new byte[0] };
            A.CallTo(() => _receiver.RecieveTransaction(txn))
                .Returns(PeerDataResult.Relay);

            //act
            await _subject.SendTransaction(txn);

            //assert
            A.CallTo(() => _peerNetwork.BroadcastTransaction(txn))
                .MustHaveHappened();
        }


        //TODO: receive block tests
    }
}
