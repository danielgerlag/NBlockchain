using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBlockchain.Tests.Scenarios.Common;
using Xunit;
using NBlockchain.Services.PeerDiscovery;
using NBlockchain.Models;
using NBlockchain.Interfaces;
using System.Threading.Tasks;
using FluentAssertions;

namespace NBlockchain.Tests.Scenarios.NodeSync
{
    public class NodeOnboardingScenarios
    {

        private static IServiceProvider ConfigureNode(uint port, ICollection<string> peers)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddBlockchain(x =>
            {
                x.UseTcpPeerNetwork(port);
                x.AddPeerDiscovery(sp => new StaticPeerDiscovery(peers));
                x.AddInstructionType<TestInstruction>();
                x.UseBlockbaseProvider<BaseBuilder>();
                x.UseParameters(new StaticNetworkParameters()
                {
                    BlockTime = TimeSpan.FromSeconds(10),                    
                    HeaderVersion = 1
                });
            });

            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }

        private static Block GenerateBlock(byte[] id, byte[] prevBlock, uint height)
        {
            var block = new Block();
            block.Header.BlockId = id;
            block.Header.Height = height;
            block.Header.Status = BlockStatus.Confirmed;
            //block.Header.Timestamp
            block.Header.PreviousBlock = prevBlock;
            return block;
        }

        private static void PopulateInitialData(IBlockRepository repo)
        {
            var prevBlock = new byte[0];
            for (byte i = 0; i < 100; i++)
            {
                var block = GenerateBlock(new byte[] { i }, prevBlock, i);
                repo.AddBlock(block);
                prevBlock = block.Header.BlockId;
            }
        }


        [Fact]
        public async void should_sync_data_over_mesh()
        {
            uint port1 = 1001;//Helpers.GetFreePort();
            uint port2 = 1002; //Helpers.GetFreePort();
            uint port3 = 1003; //Helpers.GetFreePort();
            var node1 = ConfigureNode(port1, new string[0]);
            var node2 = ConfigureNode(port2, new string[] { $"tcp://localhost:{port1}" });
            var node3 = ConfigureNode(port3, new string[] { $"tcp://localhost:{port2}" });
            var repo1 = node1.GetService<IBlockRepository>();
            var repo2 = node2.GetService<IBlockRepository>();
            var repo3 = node3.GetService<IBlockRepository>();
            var net1 = node1.GetService<IPeerNetwork>();
            var net2 = node2.GetService<IPeerNetwork>();
            var net3 = node3.GetService<IPeerNetwork>();
            PopulateInitialData(repo1);
            net1.Open();
            net2.Open();
            net3.Open();

            var target = await repo1.GetBestBlockHeader();
            var timeOut = DateTime.Now.AddSeconds(30);
            while (timeOut > DateTime.Now)
            {
                await Task.Delay(500);
                var header3 = await repo3.GetBestBlockHeader();
                if (header3?.Height == target.Height)
                    break;
            }
            
            net1.Close();
            net2.Close();
            net3.Close();

            var last2 = await repo2.GetBestBlockHeader();
            var last3 = await repo3.GetBestBlockHeader();

            last2.Height.Should().Be(target.Height);
            last3.Height.Should().Be(target.Height);
        }
    }
}
