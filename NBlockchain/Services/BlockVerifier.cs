using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NBlockchain.Interfaces;
using NBlockchain.Models;

namespace NBlockchain.Services
{
    public class BlockVerifier : IBlockVerifier
    {
        private readonly INetworkParameters _parameters;
        private readonly IEnumerable<ITransactionRule> _txnRules;
        private readonly IEnumerable<IBlockRule> _blockRules;
        private readonly IInstructionRepository _instructionRepository;
        private readonly ISignatureService _signatureService;
        private readonly IConsensusMethod _consensusMethod;
        private readonly IHasher _hasher;
        private readonly IMerkleTreeBuilder _merkleTreeBuilder;
        private readonly ITransactionKeyResolver _transactionKeyResolver;

        public BlockVerifier(INetworkParameters parameters, ISignatureService signatureService, IEnumerable<ITransactionRule> txnRules, IEnumerable<IBlockRule> blockRules, IEnumerable<ValidInstructionType> validTxnTypes, IMerkleTreeBuilder merkleTreeBuilder, ITransactionKeyResolver transactionKeyResolver, IConsensusMethod consensusMethod, IHasher hasher, IInstructionRepository instructionRepository)
        {
            _parameters = parameters;
            _signatureService = signatureService;
            _txnRules = txnRules;
            _blockRules = blockRules;
            _merkleTreeBuilder = merkleTreeBuilder;
            _transactionKeyResolver = transactionKeyResolver;
            _consensusMethod = consensusMethod;
            _hasher = hasher;
            _instructionRepository = instructionRepository;
        }

        public async Task<bool> Verify(Block block)
        {
            if (!_consensusMethod.VerifyConsensus(block))
                return false;

            var seed = block.Header.CombineHashableElementsWithNonce(block.Header.Nonce);
            var hash = _hasher.ComputeHash(seed);

            if (!hash.SequenceEqual(block.Header.BlockId))
                return false;
            
            var merkleRoot = await _merkleTreeBuilder.BuildTree(block.Transactions.Select(x => x.TransactionId).ToList());

            if (!merkleRoot.Value.SequenceEqual(block.Header.MerkelRoot))
                return false;
            
            return true;
        }

        public async Task<bool> VerifyTransactions(Block block)
        {
            foreach (var txn in block.Transactions)
            {
                var siblings = block.Transactions.Where(x => x != txn).ToList();
                if (await VerifyTransaction(txn, siblings) != 0)
                    return false;
            }
            return true;
        }

        public async Task<bool> VerifyBlockRules(Block block, bool tail)
        {
            foreach (var rule in _blockRules.Where(x => x.TailRule == tail || tail))
            {
                if (! await rule.Validate(block))
                    return false;
            }
            return true;
        }

        public async Task<int> VerifyTransaction(Transaction transaction, ICollection<Transaction> siblings)
        {
            foreach (var instruction in transaction.Instructions)
                if (!await VerifyInstruction(instruction, siblings))
                    return -1;

            var expectedId = await _transactionKeyResolver.ResolveKey(transaction);

            if (!expectedId.SequenceEqual(transaction.TransactionId))
                return -2;

            foreach (var txnRule in _txnRules)
            {
                var txnResult = txnRule.Validate(transaction, siblings);
                if (txnResult != 0)
                    return txnResult;
            }

            return 0;
        }


        private async Task<bool> VerifyInstruction(Instruction instruction, ICollection<Transaction> siblings)
        {
            if (siblings.Any(x => x.Instructions.Any(y => y.InstructionId.SequenceEqual(instruction.InstructionId))))
                return false;

            if (!_signatureService.VerifyInstruction(instruction))
                return false;

            return (!await _instructionRepository.HaveInstruction(instruction.InstructionId));
        }
    }
}
