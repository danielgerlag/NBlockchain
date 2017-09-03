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

        public void UseBlockNotary<T>()
            where T : IBlockNotary
        {
            Services.AddTransient(typeof(IBlockNotary), typeof(T));
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
            Services.AddSingleton<IPeerNetwork>(sp => new TcpPeerNetwork(port, sp.GetService<IBlockRepository>(), sp.GetServices<IPeerDiscoveryService>(), sp.GetService<ILoggerFactory>(), sp.GetService<IOwnAddressResolver>()));
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

        public void AddPeerDiscovery<T>()
            where T : IPeerDiscoveryService
        {
            Services.AddTransient(typeof(IPeerDiscoveryService), typeof(T));
        }

        public void AddPeerDiscovery(Func<IServiceProvider, IPeerDiscoveryService> factory)
        {
            Services.AddTransient<IPeerDiscoveryService>(factory);
        }

        public void UseMulticastDiscovery(string serviceId, string multicastAddress, int port)
        {
            Services.AddTransient<IPeerDiscoveryService>(sp => new MulticastDiscovery(serviceId, multicastAddress, port, sp.GetService<ILoggerFactory>(), sp.GetService<IOwnAddressResolver>()));
        }

        public void UseTransactionRepository<TService, TImplementation>()
            where TImplementation : TransactionRepository, TService
            where TService : class
        {
            Services.AddTransient<TService, TImplementation>();
        }

        public void AddTransactionType<T>()
        {
            var attr = typeof(T).GetTypeInfo().GetCustomAttribute<TransactionTypeAttribute>();
            if (attr == null)
                throw new NotSupportedException("Missing TransactionTypeAttribute");

            Services.AddSingleton<ValidTransactionType>(new ValidTransactionType(attr.TypeId, typeof(T)));
        }
        
        internal void FillDefaults()
        {
            AddDefault<INetworkParameters>(ServiceLifetime.Singleton, x => new StaticNetworkParameters()
            {
                BlockTime = TimeSpan.FromMinutes(1),
                Difficulty = 250,
                HeaderVersion = 1,
                ExpectedContentThreshold = 0.8m
            });

            AddDefault<IHasher, SHA256Hasher>(ServiceLifetime.Transient);
            AddDefault<ITransactionKeyResolver, TransactionKeyResolver>(ServiceLifetime.Transient);
            AddDefault<ISignatureService, DefaultSignatureService>(ServiceLifetime.Transient);
            AddDefault<IMerkleTreeBuilder, MerkleTreeBuilder>(ServiceLifetime.Transient);
            AddDefault<IBlockNotary, ProofOfWorkBlockNotary>(ServiceLifetime.Transient);
            AddDefault<IAddressEncoder, AddressEncoder>(ServiceLifetime.Transient);
            AddDefault<IBlockBuilder, BlockBuilder>(ServiceLifetime.Singleton);
            AddDefault<IHashTester, HashTester>(ServiceLifetime.Transient);
            AddDefault<IBlockVerifier, BlockVerifier>(ServiceLifetime.Transient);
            AddDefault<IPeerNetwork, InProcessPeerNetwork>(ServiceLifetime.Singleton);

            AddDefault<IDataConnection, DataConnection>(ServiceLifetime.Singleton);
            AddDefault<IBlockRepository, DefaultBlockRepository>(ServiceLifetime.Singleton);
            AddDefault<IPeerDiscoveryService, DefaultPeerRepository>(ServiceLifetime.Singleton);

            AddDefault<INodeHost, NodeHost>(ServiceLifetime.Singleton);
            AddDefault<IBlockReceiver>(ServiceLifetime.Singleton, sp => sp.GetService<INodeHost>());
            AddDefault<ITransactionReceiver>(ServiceLifetime.Singleton, sp => sp.GetService<INodeHost>());

            AddDefault<IDateTimeProvider, DateTimeProvider>(ServiceLifetime.Singleton);
            AddDefault<IDifficultyCalculator, DifficultyCalculator>(ServiceLifetime.Singleton);
            //AddDefault<IBuildQueue, BuildQueue>(ServiceLifetime.Singleton);

            AddDefault<IOwnAddressResolver, OwnAddressResolver>(ServiceLifetime.Singleton);

            AddDefault<IPendingTransactionList, PendingTransactionList>(ServiceLifetime.Singleton);


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
