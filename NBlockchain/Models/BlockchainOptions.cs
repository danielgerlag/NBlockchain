using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBlockchain.Interfaces;
using NBlockchain.Services;
using NBlockchain.Services.Database;
using NBlockchain.Services.Hashers;
using NBlockchain.Services.Net;
using NBlockchain.Services.PeerDiscovery;
using NBlockchain.Rules;
using NBlockchain.Services.NatTraversal;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace NBlockchain.Models
{
    public class BlockchainOptions
    {
        
        public readonly IServiceCollection Services;
        

        public BlockchainOptions(IServiceCollection services)
        {
            Services = services;
        }

        public void UseBlockbaseProvider<T>()
            where T : IBlockbaseTransactionBuilder
        {
            Services.AddTransient(typeof(IBlockbaseTransactionBuilder), typeof(T));
        }

        public void UseHasher<T>()
            where T : IHasher
        {
            Services.AddTransient(typeof(IHasher), typeof(T));
        }

        public void UseConsensusMethod<T>()
            where T : IConsensusMethod
        {
            Services.AddTransient(typeof(IConsensusMethod), typeof(T));
        }

        public void UseSignatureService<T>()
            where T : ISignatureService
        {
            Services.AddTransient(typeof(ISignatureService), typeof(T));
        }

        public void UseAddressEncoder<T>()
            where T : IAddressEncoder
        {
            Services.AddTransient(typeof(IAddressEncoder), typeof(T));
        }

        public void UseBlockRepository<T>()
            where T : IBlockRepository
        {
            Services.AddTransient(typeof(IBlockRepository), typeof(T));
        }

        public void UseBlockRepository(Func<IServiceProvider, IBlockRepository> factory)
        {
            Services.AddTransient<IBlockRepository>(factory);
        }

        public void UseTcpPeerNetwork(uint port)
        {
            Services.AddSingleton<IPeerNetwork>(sp => new TcpPeerNetwork(port, sp.GetService<INatTraversal>(), sp.GetService<IBlockRepository>(), sp.GetServices<IPeerDiscoveryService>(), sp.GetService<ILoggerFactory>(), sp.GetService<IOwnAddressResolver>(), sp.GetService<IUnconfirmedTransactionPool>(), sp.GetService<IReceiver>()));
        }

        public void UseStaticNatTraversal(uint staticPort)
        {
            Services.AddSingleton<INatTraversal>(sp => new StaticPortForwarding((int)staticPort, sp.GetService<IProvideUpnpDevice>()));
        }

        public void UseNoNatTraversal()
        {
            Services.AddSingleton<INatTraversal, NoTraversal>();
        }

        public void UseUpnpAutoNatTraversal(string mappingIdentifier)
        {
            Services.AddSingleton<INatTraversal>(sp => new UpnpAutodetectPortForwarding(mappingIdentifier, sp.GetService<ILoggerFactory>(), sp.GetService<IProvideUpnpDevice>()));
        }

        public void UsePnpStaticNatTraversal(uint staticPort, string mappingIdentifier)
        {
            Services.AddSingleton<INatTraversal>(sp => new UpnpStaticPortForwarding(mappingIdentifier, (int)staticPort, sp.GetService<ILoggerFactory>(), sp.GetService<IProvideUpnpDevice>()));
        }
        public void UseDataConnection(string connectionString)
        {
            Services.AddSingleton<IDataConnection>(sp => new DataConnection(connectionString));
        }

        public void UseParameters<T>()
            where T : INetworkParameters
        {
            Services.AddTransient(typeof(INetworkParameters), typeof(T));
        }

        public void UseParameters(INetworkParameters parameters)
        {
            Services.AddSingleton<INetworkParameters>(parameters);
        }
        
        public void AddTransactionRule<T>()
            where T : ITransactionRule
        {
            Services.AddTransient(typeof(ITransactionRule), typeof(T));
        }

        public void AddBlockRule<T>()
            where T : IBlockRule
        {
            Services.AddTransient(typeof(IBlockRule), typeof(T));
        }

        public void AddPeerDiscovery<T>()
            where T : IPeerDiscoveryService
        {
            Services.AddTransient(typeof(IPeerDiscoveryService), typeof(T));
        }

        public void AddPeerDiscovery(Func<IServiceProvider, IPeerDiscoveryService> factory)
        {
            Services.AddTransient<IPeerDiscoveryService>(factory);
        }

        public void AddContentThresholdBlockRule(decimal threshold)
        {
            Services.AddTransient(typeof(IBlockRule), sp => new BlockContentThresholdRule(sp.GetService<IUnconfirmedTransactionPool>(), threshold));
        }

        public void UseMulticastDiscovery(string serviceId, string multicastAddress, int port)
        {
            Services.AddTransient<IPeerDiscoveryService>(sp => new MulticastDiscovery(serviceId, multicastAddress, port, sp.GetService<ILoggerFactory>(), sp.GetService<IOwnAddressResolver>()));
        }

        public void UseInstructionRepository<TService, TImplementation>()
            where TImplementation : InstructionRepository, TService
            where TService : class
        {
            Services.AddTransient<TService, TImplementation>();
        }

        public void AddInstructionType<T>()
        {
            var attr = typeof(T).GetTypeInfo().GetCustomAttribute<InstructionTypeAttribute>();
            if (attr == null)
                throw new NotSupportedException("Missing InstructionTypeAttribute");

            Services.AddSingleton<ValidInstructionType>(new ValidInstructionType(attr.TypeIdentifier, typeof(T)));
        }
        
        internal void FillDefaults()
        {
            AddDefault<INetworkParameters>(ServiceLifetime.Singleton, x => new StaticNetworkParameters()
            {
                BlockTime = TimeSpan.FromMinutes(1),                
                HeaderVersion = 1
            });

            AddDefault<IHasher, SHA256Hasher>(ServiceLifetime.Transient);

            AddDefault<IAsymetricCryptographyService, AsymetricCryptographyService>(ServiceLifetime.Transient);

            AddDefault<ITransactionKeyResolver, TransactionKeyResolver>(ServiceLifetime.Transient);
            AddDefault<ISignatureService, DefaultSignatureService>(ServiceLifetime.Transient);
            AddDefault<IMerkleTreeBuilder, MerkleTreeBuilder>(ServiceLifetime.Transient);
            AddDefault<IConsensusMethod, ProofOfWorkConsensus>(ServiceLifetime.Transient);
            AddDefault<IAddressEncoder, AddressEncoder>(ServiceLifetime.Transient);
            AddDefault<IBlockMiner, BlockMiner>(ServiceLifetime.Singleton);
            AddDefault<IHashTester, HashTester>(ServiceLifetime.Transient);
            AddDefault<IBlockVerifier, BlockVerifier>(ServiceLifetime.Transient);
            AddDefault<IPeerNetwork, InProcessPeerNetwork>(ServiceLifetime.Singleton);
            AddDefault<INatTraversal, UpnpAutodetectPortForwarding>(ServiceLifetime.Singleton);

            AddDefault<IDataConnection, DataConnection>(ServiceLifetime.Singleton);
            AddDefault<IBlockRepository, DefaultBlockRepository>(ServiceLifetime.Singleton);
            AddDefault<IInstructionRepository, InstructionRepository>(ServiceLifetime.Singleton);
            //AddDefault<IPeerDiscoveryService, DefaultPeerRepository>(ServiceLifetime.Singleton);

            //AddDefault<IBlockRepository, InMemoryBlockRepository>(ServiceLifetime.Singleton);

            AddDefault<IBlockchainNode, BlockchainNode>(ServiceLifetime.Singleton);
            AddDefault<IReceiver, Receiver>(ServiceLifetime.Singleton);
            AddDefault<IProvideUpnpDevice, OpenNatUpnpProvider>(ServiceLifetime.Singleton);

            AddDefault<IDateTimeProvider, DateTimeProvider>(ServiceLifetime.Singleton);
            AddDefault<IDifficultyCalculator, DifficultyCalculator>(ServiceLifetime.Singleton);
            
            AddDefault<IOwnAddressResolver, OwnAddressResolver>(ServiceLifetime.Singleton);

            AddDefault<IUnconfirmedTransactionPool, UnconfirmedTransactionPool>(ServiceLifetime.Singleton);
            AddDefault<IExpectedBlockList, ExpectedBlockList>(ServiceLifetime.Singleton);

            AddDefault<ITransactionBuilder, TransactionBuilder>(ServiceLifetime.Singleton);
            AddDefault<IForkRebaser, ForkRebaser>(ServiceLifetime.Singleton);

        }

        private void AddDefault<TService, TImplementation>(ServiceLifetime lifetime)
            where TImplementation : TService
        {
            if (Services.All(x => x.ServiceType != typeof(TService)))
                Services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime));
        }

        private void AddDefault<TService>(ServiceLifetime lifetime, Func<IServiceProvider, object> factory)
        {
            if (Services.All(x => x.ServiceType != typeof(TService)))
                Services.Add(new ServiceDescriptor(typeof(TService), factory, lifetime));
        }
        
    }
}
