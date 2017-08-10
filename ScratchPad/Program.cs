using Microsoft.Extensions.DependencyInjection;
using NBlockChain.Interfaces;
using NBlockChain.Models;
using System;

namespace ScratchPad
{
    class Program
    {
        static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            var blockBuilder = serviceProvider.GetService<IBlockBuilder>();
            var blockValidator = serviceProvider.GetService<IBlockValidator>();




            blockBuilder.QueueTransaction(new TransactionEnvelope(new TestTransaction()
            {
                Message = "hello"
            })
            {
                Timestamp = DateTime.Now.Ticks,
                TransactionType = "test-v1",
                Originator = new byte[0]
            });            

            var block = blockBuilder.BuildBlock(new byte[0]).Result;

            blockValidator.Validate(block).Wait();


            Console.ReadLine();
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddBlockchain();
            services.AddTransient<ITransactionValidator, TestTransactionValidator>();
            
            var serviceProvider = services.BuildServiceProvider();

            //config logging
            //var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            //loggerFactory.AddDebug();
            return serviceProvider;
        }
    }
}