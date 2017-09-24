using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using NBlockchain.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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

        #region Incoming Transaction
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
        #endregion

        #region Outgoing Transaction
        [Fact]
        public async void should_locally_process_outgoing_transaction()
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
        public async void should_not_relay_invalid_outgoing_transaction()
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
        public async void should_relay_valid_outgoing_transaction()
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
        #endregion

        //TODO: complete receive block tests

        #region Incoming Block

        [Fact]
        public async void should_ignore_duplicate_blocks()
        {
            //arrange
            var block = BuildBlock();
            A.CallTo(() => _blockRepository.HavePrimaryBlock(block.Header.BlockId))
                .Returns(true);

            A.CallTo(() => _blockRepository.GetBestBlockHeader())
                .Returns(Task.FromResult<BlockHeader>(null));

            //act
            var result = await _subject.RecieveBlock(block);

            //assert
            A.CallTo(() => _blockRepository.HavePrimaryBlock(block.Header.BlockId))
                .MustHaveHappened();

            A.CallTo(() => _blockRepository.AddBlock(block))
                .MustNotHaveHappened();

            result.Should().Be(PeerDataResult.Ignore);
        }

        [Fact]
        public async void should_verify_incoming_block()
        {
            //arrange
            var block = BuildBlock();
            A.CallTo(() => _blockRepository.HavePrimaryBlock(block.Header.BlockId))
                .Returns(false);

            A.CallTo(() => _blockVerifier.Verify(block))
                .Returns(false);

            //act
            var result = await _subject.RecieveBlock(block);

            //assert
            A.CallTo(() => _blockVerifier.Verify(block))
                .MustHaveHappened();

            A.CallTo(() => _blockRepository.AddBlock(block))
                .MustNotHaveHappened();

            result.Should().Be(PeerDataResult.Demerit);
        }

        [Fact]
        public async void should_execute_block_rules_for_incoming_block()
        {
            //arrange
            var block = BuildBlock();
            A.CallTo(() => _blockRepository.HavePrimaryBlock(block.Header.BlockId))
                .Returns(false);

            A.CallTo(() => _blockVerifier.Verify(block))
                .Returns(true);

            A.CallTo(() => _blockVerifier.VerifyBlockRules(block, A<bool>.Ignored))
                .Returns(false);

            //act
            var result = await _subject.RecieveBlock(block);

            //assert
            A.CallTo(() => _blockVerifier.VerifyBlockRules(block, A<bool>.Ignored))
                .MustHaveHappened();

            A.CallTo(() => _blockRepository.AddBlock(block))
                .MustNotHaveHappened();

            result.Should().Be(PeerDataResult.Demerit);
        }

        [Fact]
        public async void should_request_previous_block_if_missing()
        {
            //arrange
            var block = BuildBlock();
            GivenVerificationPasses(block);

            A.CallTo(() => _blockRepository.GetBlockHeader(block.Header.PreviousBlock))
                .Returns(Task.FromResult<BlockHeader>(null));

            A.CallTo(() => _blockRepository.GetBestBlockHeader())
                .Returns(Task.FromResult<BlockHeader>(null));

            A.CallTo(() => _blockRepository.IsEmpty())
                .Returns(false);
            

            //act
            var result = await _subject.RecieveBlock(block);

            //assert
            A.CallTo(() => _peerNetwork.RequestBlock(block.Header.PreviousBlock))
                .MustHaveHappened();

            A.CallTo(() => _blockRepository.AddBlock(block))
                .MustNotHaveHappened();
        }

        [Fact]
        public async void should_verify_transactions_when_block_is_tip()
        {
            //arrange
            var block = BuildBlock();
            GivenVerificationPasses(block);
            GivenBlockIsNextTip(block, block.Header.Height - 1, block.Header.Timestamp - 1, block.Header.Difficulty);

            A.CallTo(() => _blockVerifier.VerifyTransactions(block))
                .Returns(false);

            //act
            var result = await _subject.RecieveBlock(block);

            //assert
            A.CallTo(() => _blockVerifier.VerifyTransactions(block))
                .MustHaveHappened();

            A.CallTo(() => _blockRepository.AddBlock(block))
                .MustNotHaveHappened();

            A.CallTo(() => _unconfirmedTransactionPool.Remove(block.Transactions))
                .MustNotHaveHappened();

            result.Should().Be(PeerDataResult.Demerit);
        }

        [Fact]
        public async void should_ignore_block_with_unexpected_height()
        {
            //arrange
            var block = BuildBlock();
            GivenVerificationPasses(block);
            GivenBlockIsNextTip(block, block.Header.Height, block.Header.Timestamp - 1, block.Header.Difficulty);
            
            //act
            var result = await _subject.RecieveBlock(block);

            //assert
            A.CallTo(() => _blockRepository.AddBlock(block))
                .MustNotHaveHappened();

            A.CallTo(() => _unconfirmedTransactionPool.Remove(block.Transactions))
                .MustNotHaveHappened();

            result.Should().Be(PeerDataResult.Ignore);
        }

        [Fact]
        public async void should_ignore_block_with_unexpected_timestamp()
        {
            //arrange
            var block = BuildBlock();
            GivenVerificationPasses(block);
            GivenBlockIsNextTip(block, block.Header.Height - 1, block.Header.Timestamp + 1, block.Header.Difficulty);

            //act
            var result = await _subject.RecieveBlock(block);

            //assert
            A.CallTo(() => _blockRepository.AddBlock(block))
                .MustNotHaveHappened();

            A.CallTo(() => _unconfirmedTransactionPool.Remove(block.Transactions))
                .MustNotHaveHappened();

            result.Should().Be(PeerDataResult.Ignore);
        }

        [Fact]
        public async void should_ignore_block_with_unexpected_difficulty()
        {
            //arrange
            var block = BuildBlock();
            GivenVerificationPasses(block);
            GivenBlockIsNextTip(block, block.Header.Height - 1, block.Header.Timestamp - 1, block.Header.Difficulty - 1);

            //act
            var result = await _subject.RecieveBlock(block);

            //assert
            A.CallTo(() => _blockRepository.AddBlock(block))
                .MustNotHaveHappened();

            A.CallTo(() => _unconfirmedTransactionPool.Remove(block.Transactions))
                .MustNotHaveHappened();

            result.Should().Be(PeerDataResult.Ignore);
        }

        [Fact]
        public async void should_not_verify_transaction_rules_on_fork_block()
        {
            //arrange
            var block = BuildBlock();
            GivenVerificationPasses(block);
            GivenBlockIsFork(block, block.Header.Height - 1, block.Header.Timestamp - 1, block.Header.Difficulty);

            //act
            var result = await _subject.RecieveBlock(block);

            //assert
            A.CallTo(() => _blockVerifier.VerifyTransactions(block))
                .MustNotHaveHappened();

            A.CallTo(() => _blockRepository.AddBlock(block))
                .MustNotHaveHappened();

            A.CallTo(() => _unconfirmedTransactionPool.Remove(block.Transactions))
                .MustNotHaveHappened();
        }

        [Fact]
        public async void should_save_block_in_orphan_pool_when_no_parent_in_mainchain()
        {
            //arrange
            var block = BuildBlock();
            GivenVerificationPasses(block);
            GivenBlockIsFork(block, block.Header.Height - 1, block.Header.Timestamp - 1, block.Header.Difficulty);

            A.CallTo(() => _blockRepository.HaveSecondaryBlock(block.Header.BlockId))
                .Returns(false);

            //act
            var result = await _subject.RecieveBlock(block);

            //assert
            A.CallTo(() => _blockRepository.AddSecondaryBlock(block))
                .MustHaveHappened();

            A.CallTo(() => _blockRepository.AddBlock(block))
                .MustNotHaveHappened();

            A.CallTo(() => _unconfirmedTransactionPool.Remove(block.Transactions))
                .MustNotHaveHappened();
        }

        [Fact]
        public async void should_rebase_chain_when_path_to_better_tip_exists()
        {
            //arrange
            var block = BuildBlock();
            var divergentHeader = new BlockHeader() { BlockId = new byte[] { 0xA } };
            GivenVerificationPasses(block);
            GivenBlockIsFork(block, block.Header.Height - 1, block.Header.Timestamp - 1, block.Header.Difficulty);

            A.CallTo(() => _blockRepository.GetDivergentHeader(block.Header.BlockId))
                .Returns(divergentHeader);

            //act
            var result = await _subject.RecieveBlock(block);

            //assert
            A.CallTo(() => _forkRebaser.RebaseChain(divergentHeader.BlockId, block.Header.BlockId))
                .MustHaveHappened();

            A.CallTo(() => _blockRepository.AddBlock(block))
                .MustNotHaveHappened();

            A.CallTo(() => _unconfirmedTransactionPool.Remove(block.Transactions))
                .MustNotHaveHappened();
        }

        [Fact]
        public async void should_not_rebase_chain_when_no_path_to_better_tip_exists()
        {
            //arrange
            var block = BuildBlock();            
            GivenVerificationPasses(block);
            GivenBlockIsFork(block, block.Header.Height - 1, block.Header.Timestamp - 1, block.Header.Difficulty);

            A.CallTo(() => _blockRepository.GetDivergentHeader(block.Header.BlockId))
                .Returns(Task.FromResult<BlockHeader>(null));

            //act
            var result = await _subject.RecieveBlock(block);

            //assert
            A.CallTo(() => _forkRebaser.RebaseChain(A<byte[]>.Ignored, A<byte[]>.Ignored))
                .MustNotHaveHappened();

            A.CallTo(() => _blockRepository.AddBlock(block))
                .MustNotHaveHappened();

            A.CallTo(() => _unconfirmedTransactionPool.Remove(block.Transactions))
                .MustNotHaveHappened();
        }

        [Fact]
        public async void should_add_block_if_valid()
        {
            //arrange
            var block = BuildBlock();
            GivenVerificationPasses(block);
            GivenBlockIsNextTip(block, block.Header.Height - 1, block.Header.Timestamp - 1, block.Header.Difficulty);

            A.CallTo(() => _blockVerifier.VerifyTransactions(block))
                .Returns(true);

            //act
            var result = await _subject.RecieveBlock(block);

            //assert
            A.CallTo(() => _blockRepository.AddBlock(block))
                .MustHaveHappened();

            A.CallTo(() => _unconfirmedTransactionPool.Remove(block.Transactions))
                .MustHaveHappened();

            result.Should().Be(PeerDataResult.Relay);
        }

        #endregion

        #region Setup Helpers

        private Block BuildBlock()            
        {
            return BuildBlock(new byte[] { 0x1, 0x2 }, new byte[] { 0x1, 0x1 });
        }

        private Block BuildBlock(byte[] blockId, byte[] previousBlock)
        {
            var result = new Block();
            result.Header.BlockId = blockId;
            result.Header.PreviousBlock = previousBlock;
            result.Header.Height = 100;

            return result;
        }

        private void GivenVerificationPasses(Block block)
        {
            A.CallTo(() => _blockRepository.HavePrimaryBlock(block.Header.BlockId))
                .Returns(false);

            A.CallTo(() => _blockVerifier.Verify(block))
                .Returns(true);

            A.CallTo(() => _blockVerifier.VerifyBlockRules(block, A<bool>.Ignored))
                .Returns(true);
        }

        private void GivenBlockIsNextTip(Block block, uint prevHeight, long prevTimestamp, uint prevDifficulty)
        {
            var prevHeader = new BlockHeader()
            {
                BlockId = block.Header.PreviousBlock,
                Height = prevHeight,
                Timestamp = prevTimestamp,
                Difficulty = prevDifficulty
            };

            A.CallTo(() => _blockRepository.HavePrimaryBlock(block.Header.BlockId))
                .Returns(false);

            A.CallTo(() => _blockRepository.GetBlockHeader(block.Header.PreviousBlock))
                .Returns(prevHeader);

            A.CallTo(() => _blockRepository.GetBestBlockHeader())
                .Returns(prevHeader);

            A.CallTo(() => _blockRepository.IsEmpty())
                .Returns(false);

            A.CallTo(() => _difficultyCalculator.CalculateDifficulty(prevHeader.Timestamp))
                .Returns(prevDifficulty);

        }

        private void GivenBlockIsFork(Block block, uint prevHeight, long prevTimestamp, uint prevDifficulty)
        {
            var prevHeader = new BlockHeader()
            {
                BlockId = block.Header.PreviousBlock,
                Height = prevHeight,
                Timestamp = prevTimestamp,
                Difficulty = prevDifficulty
            };

            var mainTip = new BlockHeader()
            {
                BlockId = new byte[] { 0xFF, 0x2, 0x3 },
                Height = block.Header.Height - 1,
                Timestamp = block.Header.Timestamp - 1,
                Difficulty = prevDifficulty
            };

            var prevMain = new BlockHeader()
            {
                BlockId = new byte[] { 0xFF, 0x2, 0x3, 0x4 },
                Height = block.Header.Height - 2,
                Timestamp = block.Header.Timestamp - 2,
                Difficulty = prevDifficulty
            };

            A.CallTo(() => _blockRepository.HavePrimaryBlock(block.Header.BlockId))
                .Returns(false);

            A.CallTo(() => _blockRepository.GetBlockHeader(block.Header.PreviousBlock))
                .Returns(prevHeader);

            A.CallTo(() => _blockRepository.GetBestBlockHeader())
                .Returns(mainTip);

            A.CallTo(() => _blockRepository.GetPrimaryHeader(block.Header.Height))
                .Returns(mainTip);

            A.CallTo(() => _blockRepository.GetPrimaryHeader(block.Header.Height - 1))
                .Returns(prevMain);

            A.CallTo(() => _blockRepository.IsEmpty())
                .Returns(false);

            A.CallTo(() => _difficultyCalculator.CalculateDifficulty(prevHeader.Timestamp))
                .Returns(prevDifficulty);

        }


        #endregion
    }
}
