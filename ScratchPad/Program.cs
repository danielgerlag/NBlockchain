using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBlockChain.Interfaces;
using NBlockChain.Models;
using NBlockChain.Services.Hashers;
using NBlockChain.Services.PeerDiscovery;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ScratchPad
{
    class Program
    {
        static void Main(string[] args)
        {
            IServiceProvider miner1 = ConfigureNode("miner1", 500, "", Guid.NewGuid(), false);
            //IServiceProvider miner2 = ConfigureNode("miner2", 501, "tcp://localhost:500", true);
            IServiceProvider node1;// = ConfigureNode("node1", true);

            Console.WriteLine("starting miner");
            RunMiner(miner1, true);

            var miner1Net = miner1.GetService<IPeerNetwork>();
            
            //RunMiner(miner2, false);

            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(5000);
                Console.WriteLine("starting node");
                node1 = ConfigureNode("node1", 502, "tcp://localhost:500", miner1Net.NodeId, true);
                await RunNode(node1, true);
            });
            

            //RunNode(node1, true);
            //var block = blockBuilder.BuildBlock(new byte[0], minerKeys).Result;

            //blockValidator.ConfirmBlock(block).Wait();


            Console.ReadLine();
        }

        private static void RunMiner(IServiceProvider sp, bool genesis)
        {
            var node = sp.GetService<INodeHost>();
            var network = sp.GetService<IPeerNetwork>();
            var sigService = sp.GetService<ISignatureService>();
            var addressEncoder = sp.GetService<IAddressEncoder>();

            network.Open();

            var minerKeys = sigService.GenerateKeyPair();

            var keys = sigService.GenerateKeyPair();
            var keys2 = sigService.GenerateKeyPair();
            var address = addressEncoder.EncodeAddress(keys.PublicKey, 0);

            if (genesis)
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
        }

        private static async Task RunNode(IServiceProvider sp, bool sendTxn)
        {
            var node = sp.GetService<INodeHost>();
            var network = sp.GetService<IPeerNetwork>();
            var sigService = sp.GetService<ISignatureService>();
            var addressEncoder = sp.GetService<IAddressEncoder>();
            var keys = sigService.GenerateKeyPair();
            network.Open();

            var address = addressEncoder.EncodeAddress(keys.PublicKey, 0);

            if (sendTxn)
            {
                var txn1 = new TestTransaction()
                {
                    Message = "node txn"
                };

                var txn1env = new TransactionEnvelope(txn1)
                {
                    OriginKey = Guid.NewGuid(),
                    TransactionType = "test-v1",
                    Originator = address
                };

                sigService.SignTransaction(txn1env, keys.PrivateKey);

                await Task.Delay(5000);
                await node.SendTransaction(txn1env);
            }
        }

        private static IServiceProvider ConfigureNode(string db, uint port, string peerStr, Guid peerKey, bool logging)
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddBlockchain(x =>
            {
                x.UseMongoDB(@"mongodb://localhost:27017", db);
                x.UseTcpPeerNetwork(port);
                //x.AddPeerDiscovery(sp => new StaticPeerDiscovery(peerStr, peerKey));
                x.UseMulticastDiscovery("test", "224.5.6.7", 4567);
                x.AddTransactionType<TestTransaction>();
                x.AddValidator<TestTransactionValidator>();
                x.UseBlockbaseProvider<TestBlockbaseBuilder>();
                x.UseParameters(new StaticNetworkParameters()
                {
                    BlockTime = TimeSpan.FromSeconds(10),
                    Difficulty = 200,
                    HeaderVersion = 1,
                    ExpectedContentThreshold = 0.8m
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