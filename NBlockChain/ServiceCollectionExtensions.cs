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
        public static void AddBlockchain(this IServiceCollection services, Action<BlockchainOptions> setupAction)
        {
            var options = new BlockchainOptions(services);
            setupAction(options);
            options.FillDefaults();
        }
    }
}
