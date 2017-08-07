using Microsoft.Extensions.DependencyInjection;
using NBlockChain.Interfaces;
using System;

namespace ScratchPad
{
    class Program
    {
        static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            var blockBuilder = serviceProvider.GetService<IBlockBuilder<TestTransaction>>();
            var blockValidator = serviceProvider.GetService<IBlockValidator<TestTransaction>>();

            blockBuilder.QueueTransaction(new TestTransaction()
            {
                Timestamp = DateTime.Now.Ticks,
                Version = 1,
                Message = "hello"
            });

            blockBuilder.QueueTransaction(new TestTransaction()
            {
                Timestamp = DateTime.Now.Ticks,
                Version = 1,
                Message = "bye"
            });

            var block = blockBuilder.BuildBlock(DateTime.Now, new byte[0]).Result;

            blockValidator.Validate(block).Wait();


            Console.ReadLine();
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddBlockchain<TestTransaction>();
            

            var serviceProvider = services.BuildServiceProvider();

            //config logging
            //var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            //loggerFactory.AddDebug();
            return serviceProvider;
        }
    }
}