using DigitalCurrency.Repositories;
using DigitalCurrency.Repositories.LiteDb;
using DigitalCurrency.Repositories.Mongo;
using DigitalCurrency.Rules;
using DigitalCurrency.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using NBlockchain.Services.PeerDiscovery;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCurrency
{
    class Program
    {
        private static INodeHost _host;
        private static IBlockMiner _miner;
        private static IPeerNetwork _network;
        private static ISignatureService _sigService;
        private static IAddressEncoder _addressEncoder;
        private static ICustomInstructionRepository _txnRepo;
        private static IBlockRepository _blockRepo;
        private static ITransactionBuilder _txnBuilder;
        
        static void Main(string[] args)
        {
            //var serviceProvider = ConfigureForMongoDB("DigitalCurrency", 10500);
            var serviceProvider = ConfigureForLiteDb("node.db", 10500);

            _host = serviceProvider.GetService<INodeHost>();
            _miner = serviceProvider.GetService<IBlockMiner>();
            _network = serviceProvider.GetService<IPeerNetwork>();
            _sigService = serviceProvider.GetService<ISignatureService>();
            _addressEncoder = serviceProvider.GetService<IAddressEncoder>();
            _txnRepo = serviceProvider.GetService<ICustomInstructionRepository>();
            _blockRepo = serviceProvider.GetService<IBlockRepository>();
            _txnBuilder = serviceProvider.GetService<ITransactionBuilder>();

            Console.Write("Enter passphrase:");
            var phrase = Console.ReadLine();
            var keys = _sigService.GetKeyPairFromPhrase(phrase);
            var address = _addressEncoder.EncodeAddress(keys.PublicKey, 0);
            Console.WriteLine($"Your address is {address}");

            _network.Open();

            PrintHelp();
            while (true)
            {
                Console.Write(">");
                var command = Console.ReadLine();

                if (command == "exit")
                    break;

                RunCommand(command, keys);
            }
            _network.Close();
        }

        private static IServiceProvider ConfigureForLiteDb(string db, uint port)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddBlockchain(x =>
            {
                x.UseDataConnection("node.db");
                x.UseInstructionRepository<ICustomInstructionRepository, CustomInstructionRepository>();
                x.UseTcpPeerNetwork(port);
                x.UseMulticastDiscovery("My Currency", "224.100.0.1", 8088);
                x.AddPeerDiscovery(sp => new StaticPeerDiscovery(new string[] { "tcp://192.168.0.13:10500" }));

                x.AddInstructionType<TransferInstruction>();
                x.AddInstructionType<CoinbaseInstruction>();
                x.AddTransactionRule<BalanceRule>();
                x.AddTransactionRule<CoinbaseTransactionRule>();
                x.AddBlockRule<CoinbaseBlockRule>();
                x.UseBlockbaseProvider<CoinbaseBuilder>();
                x.UseParameters(new StaticNetworkParameters()
                {
                    BlockTime = TimeSpan.FromSeconds(120),
                    HeaderVersion = 1
                });
            });

            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddDebug();
            loggerFactory.AddFile("node.log", LogLevel.Debug);

            return serviceProvider;
        }

        private static IServiceProvider ConfigureForMongoDB(string db, uint port)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddBlockchain(x =>
            {
                x.UseTcpPeerNetwork(port);
                x.UseMongoDB(@"mongodb://localhost:27017", db)
                    .UseInstructionRepository<ICustomInstructionRepository, CustomMongoInstructionRepository>();
                //x.AddPeerDiscovery(sp => new StaticPeerDiscovery("tcp://localhost:503"));
                x.UseMulticastDiscovery("My Currency", "224.100.0.1", 8088);
                x.AddInstructionType<TransferInstruction>();
                x.AddInstructionType<CoinbaseInstruction>();
                x.AddTransactionRule<BalanceRule>();
                x.AddTransactionRule<CoinbaseTransactionRule>();
                x.AddBlockRule<CoinbaseBlockRule>();
                x.UseBlockbaseProvider<CoinbaseBuilder>();
                x.UseParameters(new StaticNetworkParameters()
                {
                    BlockTime = TimeSpan.FromSeconds(120),
                    HeaderVersion = 1
                });
            });

            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddDebug();
            loggerFactory.AddFile("node.log", LogLevel.Debug);

            return serviceProvider;
        }



        static void RunCommand(string command, KeyPair keys)
        {
            var args = command.Split(' ');
            var ownAddress = _addressEncoder.EncodeAddress(keys.PublicKey, 0);

            switch (args[0])
            {
                case "help":
                    PrintHelp();
                    break;
                case "mine-genesis":
                    Console.WriteLine("Mining...");
                    _miner.Start(keys, true);
                    break;
                case "mine":
                    Console.WriteLine("Mining...");
                    _miner.Start(keys, false);
                    break;
                case "stop-mining":
                    Console.WriteLine("Stopping...");
                    _miner.Stop();
                    break;
                case "peers":                    
                    var peersIn = _network.GetPeersIn();
                    var peersOut = _network.GetPeersOut();
                    Console.WriteLine("Incoming peers");
                    PrintPeerList(peersIn);
                    Console.WriteLine("Outgoing peers");
                    PrintPeerList(peersOut);
                    break;
                case "balance":
                    if (args.Length == 1)
                        Console.WriteLine($"Balance = {_txnRepo.GetAccountBalance(ownAddress)}");
                    else
                        Console.WriteLine($"Balance = {_txnRepo.GetAccountBalance(args[1])}");
                    break;
                case "best-block":
                    var header = _blockRepo.GetBestBlockHeader().Result;
                    Console.WriteLine($"Height: {header.Height}, Id: {BitConverter.ToString(header.BlockId)}");                    
                    break;
                case "gen-key":
                    var genkeys = _sigService.GetKeyPairFromPhrase(args[1]);
                    var address = _addressEncoder.EncodeAddress(genkeys.PublicKey, 0);
                    Console.WriteLine($"{address}");
                    break;
                case "avg-time":
                    var avgTime = _blockRepo.GetAverageBlockTimeInSecs(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow).Result;
                    Console.WriteLine($"Avg time: {avgTime}s");
                    break;
                case "send":
                    if (args.Length != 3)
                    {
                        Console.WriteLine("invalid command");
                        return;
                    }

                    var instruction = new TransferInstruction()
                    {
                        PublicKey = keys.PublicKey,
                        Amount = Convert.ToInt32(args[2]),
                        Destination = _addressEncoder.ExtractPublicKeyHash(args[1])
                    };

                    Console.WriteLine($"Signing instruction");
                    _sigService.SignInstruction(instruction, keys.PrivateKey);
                    var txn = _txnBuilder.Build(new List<Instruction>() { instruction }).Result;

                    Console.WriteLine($"Sending transaction {BitConverter.ToString(txn.TransactionId)}");
                    _host.SendTransaction(txn);
                    break;
                default:
                    Console.WriteLine("invalid command");
                    break;
            }
        }
                
        static void PrintHelp()
        {
            Console.WriteLine();
            Console.WriteLine("help - prints this message");
            Console.WriteLine("mine-genesis - build the genesis block and start mining");
            Console.WriteLine("mine - start mining");
            Console.WriteLine("peers - show connected peers");
            Console.WriteLine("balance - prints your balance");
            Console.WriteLine("balance [address] - prints balance of [address]");
            Console.WriteLine("send [address] [amount] - sends [amount] to [address]");
            Console.WriteLine("exit - end process");
            Console.WriteLine();
        }

        static void PrintPeerList(ICollection<ConnectedPeer> peers)
        {
            foreach (var item in peers)
                Console.WriteLine($"{item.NodeId} - {item.Address}");
        }
    }
}