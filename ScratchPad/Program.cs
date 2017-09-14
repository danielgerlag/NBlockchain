using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using NBlockchain.Services.Hashers;
using NBlockchain.Services.PeerDiscovery;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ScratchPad
{
    class Program
    {
        static void Main(string[] args)
        {
            IServiceProvider miner1 = ConfigureNode("miner1", 500, new string[0], true);
            //IServiceProvider miner2 = ConfigureNode("miner2", 501, "tcp://localhost:500", true);
            //IServiceProvider node1 = ConfigureNode("node1", 502, "tcp://localhost:500", true);

            //Console.WriteLine("starting miner");
            var keys1 = RunMiner(miner1, true);

            //var miner1Net = miner1.GetService<IPeerNetwork>();

            //RunMiner(miner2, false);

            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(5000);
                Console.WriteLine("starting node");
                var node1 = ConfigureNode("node1", 502, new string[] {"tcp://localhost:500"}, true);
                var node1Keys = RunNode(node1);

                Task.Factory.StartNew(() => CheckBalance("node1", node1, node1Keys));

                await Task.Delay(5000);
                var addressEncoder = node1.GetService<IAddressEncoder>();
                var nodeAddr = addressEncoder.EncodeAddress(node1Keys.PublicKey, 0);
                Console.WriteLine("sending txn...");
                SendTxn(miner1, keys1, nodeAddr, 5);
            });


            //RunNode(node1, true);
            //var block = blockBuilder.BuildBlock(new byte[0], minerKeys).Result;

            //blockValidator.ConfirmBlock(block).Wait();

            Task.Factory.StartNew(() => CheckBalance("miner1", miner1, keys1));

            Console.ReadLine();
        }

        private static KeyPair RunMiner(IServiceProvider sp, bool genesis)
        {
            var node = sp.GetService<INodeHost>();
            var miner = sp.GetService<IBlockMiner>();
            var network = sp.GetService<IPeerNetwork>();
            var sigService = sp.GetService<ISignatureService>();
            //var addressEncoder = sp.GetService<IAddressEncoder>();

            network.Open();

            //var minerKeys = sigService.GenerateKeyPair();

            var keys = sigService.GenerateKeyPair();
            //var keys2 = sigService.GenerateKeyPair();
            //var address = addressEncoder.EncodeAddress(keys.PublicKey, 0);

            miner.Start(keys, genesis);

            return keys;            
        }

        private static void SendTxn(IServiceProvider sp, KeyPair keys, string address, int amount)
        {
            var node = sp.GetService<INodeHost>();            
            var sigService = sp.GetService<ISignatureService>();
            var addressEncoder = sp.GetService<IAddressEncoder>();
            var origin = addressEncoder.EncodeAddress(keys.PublicKey, 0);

            var txn1 = new TestTransaction()
            {
                Message = "hello",
                Amount = amount,
                Destination = address
            };

            var txn1env = new NBlockchain.Models.Transaction(txn1)
            {
                OriginKey = Guid.NewGuid(),
                TransactionType = "txn-v1",
                Originator = origin
            };

            sigService.SignTransaction(txn1env, keys.PrivateKey);

            node.SendTransaction(txn1env);
        }

        private static decimal GetBalance(IServiceProvider sp, KeyPair keys)
        {
            var repo = sp.GetService<ICustomInstructionRepository>();
            var addressEncoder = sp.GetService<IAddressEncoder>();
            var address = addressEncoder.EncodeAddress(keys.PublicKey, 0);

            return repo.GetAccountBalance(address);
        }

        private static async void CheckBalance(string name, IServiceProvider sp, KeyPair keys) 
        {
            while (true)
            {
                await Task.Delay(5000);
                try
                {
                    Console.WriteLine($"{name} balance: {GetBalance(sp, keys)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static KeyPair RunNode(IServiceProvider sp)
        {
            var node = sp.GetService<INodeHost>();
            var network = sp.GetService<IPeerNetwork>();
            var sigService = sp.GetService<ISignatureService>();
            //var addressEncoder = sp.GetService<IAddressEncoder>();
            var keys = sigService.GenerateKeyPair();
            network.Open();

            //var address = addressEncoder.EncodeAddress(keys.PublicKey, 0);
            return keys;
        }

        private static IServiceProvider ConfigureNode(string db, uint port, string[] peers, bool logging)
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddBlockchain(x =>
            {
                //x.UseMongoDB(@"mongodb://localhost:27017", db)
                x.UseTransactionRepository<ICustomInstructionRepository, CustomInstructionRepository>();
                x.UseTcpPeerNetwork(port);
                x.AddPeerDiscovery(sp => new StaticPeerDiscovery(peers));
                //x.UseMulticastDiscovery("test", "224.100.0.1", 8088);
                //x.UseDataConnection($"{db}.db");
                x.AddTransactionType<TestTransaction>();
                x.AddTransactionType<CoinbaseTransaction>();
                x.AddTransactionRule<TestTransactionValidator>();
                x.AddTransactionRule<CoinbaseTransactionValidator>();
                x.UseBlockbaseProvider<TestBlockbaseBuilder>();
                x.UseParameters(new StaticNetworkParameters()
                {
                    BlockTime = TimeSpan.FromSeconds(10),                    
                    HeaderVersion = 1
                });
            });

            services.AddLogging();            
            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            if (logging)
            {
                loggerFactory.AddDebug();
                loggerFactory.AddConsole(LogLevel.Debug);
            }

            return serviceProvider;
        }
    }
}