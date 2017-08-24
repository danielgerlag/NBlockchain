using NBlockchain.Interfaces;
using NBlockchain.Models;
using NBlockchain.Services;
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
