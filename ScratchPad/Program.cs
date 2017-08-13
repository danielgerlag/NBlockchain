using Microsoft.Extensions.DependencyInjection;
using NBlockChain.Interfaces;
using NBlockChain.Models;
using System;
using System.Linq;

namespace ScratchPad
{
    class Program
    {
        static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            var node = serviceProvider.GetService<INodeHost>();

            var sigService = serviceProvider.GetService<ISignatureService>();
            var addressEncoder = serviceProvider.GetService<IAddressEncoder>();

            var minerKeys = sigService.GenerateKeyPair();

            var keys = sigService.GenerateKeyPair();
            var keys2 = sigService.GenerateKeyPair();
            var address = addressEncoder.EncodeAddress(keys.PublicKey, 0);

            node.BuildGenesisBlock(minerKeys);
            node.StartBuildingBlocks(minerKeys);

            var txn1 = new TestTransaction()
            {
                Message = "hello"
            };

            var txn1env = new TransactionEnvelope(txn1)
            {
                OriginKey = Guid.NewGuid(),
                TransactionType = "test-v1",
                Originator = address
            };
            
            sigService.SignTransaction(txn1env, keys2.PrivateKey);

            node.SendTransaction(txn1env);
            
            //var block = blockBuilder.BuildBlock(new byte[0], minerKeys).Result;

            //blockValidator.Notarize(block).Wait();


            Console.ReadLine();
            node.StopBuildingBlocks();
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddBlockchain(x =>
            {
                x.AddTransactionType<TestTransaction>();
                x.AddValidator<TestTransactionValidator>();
                x.UseBlockbaseProvider<TestBlockbaseBuilder>();
                x.UseParameters(new StaticNetworkParameters()
                {
                    BlockTime = TimeSpan.FromMinutes(1),
                    Difficulty = 250,
                    HeaderVersion = 1
                });
            });

            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            //config logging
            //var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            //loggerFactory.AddDebug();
            return serviceProvider;
        }
    }
}