# NBlockchain

**This documentation is still a work in progress!!!**

NBlockchain is a .NET standard library for building blockchain applications.

## Block content

A block consists of a collection of transactions, and transactions in turn are simply a container for instructions.
You may define your own schema for each of your own instruction types by inheriting off the `Instruction` abstract class.

Once you have defined your own instruction types, you can implement an instruction repository in order to query accepted instructions within your custom rule sets.  Do this by inheriting off the `InstructionRepository` class for LiteDb or the `MongoInstructionRepository` for MongoDB.

You may then define transaction level rules that can inspect the enclosed instructions by implementing the `ITransactionRule` interface.
At this point you may inject your custom instruction repositories into these rule classes via the constructor.

Furthermore, you may define block level rules that can inspect all the transactions within a block by implementing the `IBlockRule` interface.

## Node configuration

You must configure the IoC container with the various pieces you wish to include.  eg.

```c#
services.AddBlockchain(blockchain =>
{
    blockchain.UseDataConnection("node.db");
    blockchain.UseInstructionRepository<ICustomInstructionRepository, CustomInstructionRepository>();
    blockchain.UseTcpPeerNetwork(port);
    blockchain.UseMulticastDiscovery("My Currency", "224.100.0.1", 8088);
    blockchain.AddInstructionType<TransferInstruction>();
    blockchain.AddInstructionType<CoinbaseInstruction>();
    blockchain.AddTransactionRule<BalanceRule>();
    blockchain.AddTransactionRule<CoinbaseTransactionRule>();
    blockchain.AddBlockRule<CoinbaseBlockRule>();
    blockchain.UseBlockbaseProvider<CoinbaseBuilder>();
    blockchain.UseParameters(new StaticNetworkParameters()
    {
        BlockTime = TimeSpan.FromSeconds(120),
        HeaderVersion = 1
    });
});
```
