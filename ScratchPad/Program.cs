using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBlockChain.Interfaces;
using NBlockChain.Models;
using NBlockChain.Services.Hashers;
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

            node.BuildGenesisBlock(minerKeys).Wait();
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
            
            sigService.SignTransaction(txn1env, keys.PrivateKey);

            node.SendTransaction(txn1env);
            
            //var block = blockBuilder.BuildBlock(new byte[0], minerKeys).Result;

            //blockValidator.ConfirmBlock(block).Wait();


            Console.ReadLine();
            node.StopBuildingBlocks();
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddBlockchain(x =>
            {
                x.UseMongoDB(@"mongodb://localhost:27017", "nbc");
                x.AddTransactionType<TestTransaction>();
                x.AddValidator<TestTransactionValidator>();
                x.UseBlockbaseProvider<TestBlockbaseBuilder>();
                x.UseParameters(new StaticNetworkParameters()
                {
                    BlockTime = TimeSpan.FromSeconds(10),
                    Difficulty = 700,
                    HeaderVersion = 1,
                    ExpectedContentThreshold = 0.8m
                });
            });

            services.AddLogging();            
            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddDebug();
            loggerFactory.AddConsole(LogLevel.Debug);

            return serviceProvider;
        }
    }
}