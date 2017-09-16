# Digital currency sample for NBlockchain

This example demonstrates how to implement a very basic digital currency with NBlockchain.
(This does not follow the flexible input/output locking script scheme that Bitcoin uses but it just meant to illustrate an application of NBlockchin)

## Define our instruction types

The first thing we will do is define the schema of the instructions we want to store in our blockchain. 
Each block in the blockchain holds a collection of atomic transactions and each transaction is a collection of instructions.

```c#
public abstract class ValueInstruction : Instruction
{
    public int Amount { get; set; }

    public override ICollection<byte[]> ExtractSignableElements()
    {
        return new List<byte[]>() { BitConverter.GetBytes(Amount) };
    }
}

public class TransferInstruction : ValueInstruction
{
    public string Message { get; set; }

    public byte[] Destination { get; set; }

    public override ICollection<byte[]> ExtractSignableElements()
    {
        var result = base.ExtractSignableElements();
        result.Add(Destination);
        return result;
    }
}

public class CoinbaseInstruction : ValueInstruction
{
}
```

In this case we want two types of instructions
 * A normal value transfer instruction which is used to send tokens to someone.
 * A coinbase instruction that is created per block by the mining node

## Implement a repository to query our instructions for balance

Now we need to implement a repository to run queries against our defined instructions.
This can be done by extending `InstructionRepository` which gives us access the the block store (if MongoDB is used as the persistence store, then you would extend `MongoInstructionRepository`, see [sample](Repositories/Mongo/CustomMongoInstructionRepository.cs))

```c#
public interface ICustomInstructionRepository
{
    decimal GetAccountBalance(string address);
}

public class CustomInstructionRepository : InstructionRepository, ICustomInstructionRepository
{
    private readonly IAddressEncoder _addressEncoder;

    public CustomInstructionRepository(ILoggerFactory loggerFactory, IDataConnection dataConnection, IAddressEncoder addressEncoder)
        : base(loggerFactory, dataConnection)
    {
        _addressEncoder = addressEncoder;
    }

    public decimal GetAccountBalance(string address)
    {
        var publicKeyHash = _addressEncoder.ExtractPublicKeyHash(address);

        var totalOut = Instructions
            .Find(Query.EQ("Statistics.PublicKeyHash", publicKeyHash))
            .Select(x => x.Entity)
            .OfType<ValueInstruction>()
            .Sum(x => x.Amount);


        var totalIn = Instructions
            .Find(Query.EQ("Entity.Destination", publicKeyHash))
            .Select(x => x.Entity)
            .OfType<TransferInstruction>()
            .Sum(x => x.Amount);

        return (totalIn - totalOut);
    }
}

```

## Define the rules for our instructions

Now we want to define a rule that you cannot spend more than the balance of your account.

```c#
public class BalanceRule : ITransactionRule
{
    private readonly ICustomInstructionRepository _txnRepo;
    private readonly IAddressEncoder _addressEncoder;

    public BalanceRule(ICustomInstructionRepository txnRepo, IAddressEncoder addressEncoder)
    {
        _txnRepo = txnRepo;
        _addressEncoder = addressEncoder;
    }
        
    public int Validate(Transaction transaction, ICollection<Transaction> siblings)
    {
        if (transaction.Instructions.OfType<TransferInstruction>().Any(x => x.Amount < 0))
            return 1;
            
        foreach (var instruction in transaction.Instructions.OfType<TransferInstruction>())
        {
            var sourceAddr = _addressEncoder.EncodeAddress(instruction.PublicKey, 0);
            var balance = _txnRepo.GetAccountBalance(sourceAddr);
            if (instruction.Amount > balance)
                return 2;
        }

        return 0;
    }
}    
```

## Define how the base transaction per block is built

Now we define how the coinbase transaction is built (by the miners).
In this case, we will have a static block reward of 50

```c#
public class CoinbaseBuilder : BlockbaseTransactionBuilder
{
    ...

    protected override ICollection<Instruction> BuildInstructions(KeyPair builderKeys, ICollection<Transaction> transactions)
    {
        var result = new List<Instruction>();
        var instruction = new CoinbaseInstruction
        {
            Amount = -50,
            PublicKey = builderKeys.PublicKey
        };

        SignatureService.SignInstruction(instruction, builderKeys.PrivateKey);
        result.Add(instruction);

        return result;
    }
}
```

## Configure our node

When we configure the IoC container for our blockchain node, we have several options
In this case we chose
 * To use the built-in LiteDb as the database (using node.db as the datafile)
 * Register our custome instruction repository that we use in our rule definitions
 * To use the Tcp peer network and listen on port 500
 * Use the multicast peer discovery protocol (to find other peers on the LAN)
 * Added our Instruction types that we defined earlier
 * Added our Instruction rules
 * Added our Block rules
 * Set the block time to 120 seconds

```c#
IServiceCollection services = new ServiceCollection();
services.AddBlockchain(x =>
{
    x.UseDataConnection("node.db");
    x.UseInstructionRepository<ICustomInstructionRepository, CustomInstructionRepository>();
    x.UseTcpPeerNetwork(port);
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
```

If you wanted to use MongoDB as the persistence store, then the config would look something like this

```c#
IServiceCollection services = new ServiceCollection();
services.AddBlockchain(x =>
{
    x.UseTcpPeerNetwork(port);
    x.UseMongoDB(@"mongodb://localhost:27017", db)
        .UseInstructionRepository<ICustomInstructionRepository, CustomMongoInstructionRepository>();
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
```
