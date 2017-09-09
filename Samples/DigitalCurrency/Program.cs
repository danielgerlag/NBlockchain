﻿using DigitalCurrency.Repositories;
using DigitalCurrency.Rules;
using DigitalCurrency.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;
using NBlockchain.Models;
using NBlockchain.Services.PeerDiscovery;
using System;
using System.Collections.Generic;

namespace DigitalCurrency
{
    class Program
    {
        private static INodeHost _host;
        private static IBlockMiner _miner;
        private static IPeerNetwork _network;
        private static ISignatureService _sigService;
        private static IAddressEncoder _addressEncoder;
        private static ITransactionRepository _txnRepo;

        private static IServiceProvider ConfigureNode(string db, uint port)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddBlockchain(x =>
            {
                x.UseMongoDB(@"mongodb://localhost:27017", db)
                    .UseTransactionRepository<ITransactionRepository, TransactionRepository>();
                x.UseTcpPeerNetwork(port);
                //x.AddPeerDiscovery(sp => new StaticPeerDiscovery("tcp://localhost:503"));
                x.UseMulticastDiscovery("My Currency", "224.100.0.1", 8088);
                x.AddTransactionType<TransferTransaction>();
                x.AddTransactionType<CoinbaseTransaction>();
                x.AddTransactionRule<BalanceRule>();
                x.AddTransactionRule<CoinbaseRule>();
                x.UseBlockbaseProvider<CoinbaseBuilder>();
                x.UseParameters(new StaticNetworkParameters()
                {
                    BlockTime = TimeSpan.FromSeconds(10),                    
                    HeaderVersion = 1,
                    ExpectedContentThreshold = 0.8m
                });
            });

            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();   
            //loggerFactory.AddProvider()

            return serviceProvider;
        }

        static void Main(string[] args)
        {
            var serviceProvider = ConfigureNode("DigitalCurrency", 10500);

            _host = serviceProvider.GetService<INodeHost>();
            _miner = serviceProvider.GetService<IBlockMiner>();
            _network = serviceProvider.GetService<IPeerNetwork>();
            _sigService = serviceProvider.GetService<ISignatureService>();
            _addressEncoder = serviceProvider.GetService<IAddressEncoder>();
            _txnRepo = serviceProvider.GetService<ITransactionRepository>();

            Console.WriteLine("Generating key pair...");
            var keys = _sigService.GenerateKeyPair();            
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
                case "send":
                    if (args.Length != 3)
                    {
                        Console.WriteLine("invalid command");
                        return;
                    }

                    var txn = new TransferTransaction()
                    {
                        Amount = Convert.ToInt32(args[2]),
                        Destination = args[1]
                    };

                    var txnEenv = new TransactionEnvelope(txn)
                    {
                        OriginKey = Guid.NewGuid(),
                        TransactionType = "txn-v1",
                        Originator = ownAddress
                    };
                    Console.WriteLine($"Singing transaction {txnEenv.OriginKey}");
                    _sigService.SignTransaction(txnEenv, keys.PrivateKey);
                    Console.WriteLine($"Sending transaction {txnEenv.OriginKey}");
                    _host.SendTransaction(txnEenv);
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