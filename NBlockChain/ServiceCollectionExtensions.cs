using NBlockChain.Interfaces;
using NBlockChain.Models;
using NBlockChain.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddBlockchain(this IServiceCollection services)
        {
            services.AddSingleton<INetworkParameters>(new StaticNetworkParameters()
            {
                BlockTime = TimeSpan.FromMinutes(1),
                TransactionVersion = 1,
                Difficulty = 250
            });

            services.AddTransient<IHasher, SHA256Hasher>();
            services.AddTransient<ITransactionKeyResolver, TransactionKeyResolver>();
            services.AddTransient<ISignatureService, DefaultSignatureService>();
            services.AddTransient<IMerkleTreeBuilder, MerkleTreeBuilder>();
            services.AddTransient<IBlockValidator, ProofOfWorkBlockValidator>();

            services.AddSingleton<IBlockBuilder, BlockBuilder>();

        }
    }
}
