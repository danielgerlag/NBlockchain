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
        public static void AddBlockchain<TTransaction>(this IServiceCollection services)
            where TTransaction : AbstractTransaction
        {
            services.AddSingleton<INetworkParameters>(new StaticNetworkParameters()
            {
                BlockTime = TimeSpan.FromMinutes(1),
                TransactionVersion = 1,
                Difficulty = 250
            });

            services.AddTransient<IHasher, SHA256Hasher>();
            services.AddTransient<IMerkleTreeBuilder, MerkleTreeBuilder>();
            services.AddTransient<IBlockValidator<TTransaction>, ProofOfWorkBlockValidator<TTransaction>>();

            services.AddSingleton<IBlockBuilder<TTransaction>, BlockBuilder<TTransaction>>();

        }
    }
}
