# Digital currency sample for NBlockchain

This example demonstrates how to implement a very basic digital currency with NBlockchain.
(This does not follow the input/output aggregation model that Bitcoin uses but it just meant to illustrate an application of NBlockchin)

## Define our transactions types

The first thing we will do is define the schema of the transactions we want to store in our blockchain.

```c#
public abstract class ValueTransaction : BlockTransaction
{
    public int Amount { get; set; }
}

[TransactionType("txn-v1")]
public class TransferTransaction : ValueTransaction
{
    public string Message { get; set; }
    public string Destination { get; set; }
}

[TransactionType("coinbase-v1")]
public class CoinbaseTransaction : ValueTransaction
{
}
```

In this case we want two types of transactions
 * A normal value transfer transaction which is used to send tokens to someone.
 * A coinbase transaction that is created per block by the mining node

## Implement a repository to query our transactions

Now we need to implement a repository to run queries against our defined transactions.
This can be done by extending `MongoTransactionRepository` which gives us access the the block store (if MongoDB is used as the persistence store)

```c#
public class TransactionRepository : MongoTransactionRepository, ITransactionRepository
{
    public TransactionRepository(IMongoDatabase database)
        : base(database)
    {
    }	    

    public decimal GetAccountBalance(string account)
    {
        var totalOut = 0;
        var totalIn = 0;

        var outQry = Blocks.Aggregate()
            .Unwind(x => x.Transactions)
            .Match(new BsonDocument("Transactions.Originator", account))
            .Group(new BsonDocument { { "_id", BsonNull.Value }, { "sum", new BsonDocument("$sum", "$Transactions.Transaction.Amount") } })
            .SingleOrDefault();

        if (outQry != null)
        {
            if (outQry.TryGetValue("sum", out var bOut))
                totalOut = bOut.AsInt32;
        }

        var inQry = Blocks.Aggregate()
            .Unwind(x => x.Transactions)
            .Match(new BsonDocument("Transactions.Transaction.Destination", account))
            .Group(new BsonDocument { { "_id", BsonNull.Value }, { "sum", new BsonDocument("$sum", "$Transactions.Transaction.Amount") } })
            .SingleOrDefault();

        if (inQry != null)
        {
            if (inQry.TryGetValue("sum", out var bIn))
                totalIn = bIn.AsInt32;
        }

        return (totalIn - totalOut);
    }

}
```

## Define the rules for our transactions

Now we want to define a rule that you cannot spend more than the balance of your wallet.

```c#
public class BalanceRule : TransactionRule<TransferTransaction>
{
    private readonly ITransactionRepository _txnRepo;

    public BalanceRule(ITransactionRepository txnRepo)
    {
        _txnRepo = txnRepo;
    }

    protected override int Validate(TransactionEnvelope envelope, TransferTransaction transaction, ICollection<TransactionEnvelope> siblings)
    {
        if (transaction.Amount < 0)
            return 1;

        var balance = _txnRepo.GetAccountBalance(envelope.Originator);
        if (transaction.Amount > balance)
            return 2;

        return 0;
    }
}    
```

## Define how the base transaction per block is built

Now we define how the coinbase transaction is built (by the miners).
In this case, we will have a static block reward of 50

```c#
public class CoinbaseBuilder : BlockbaseTransactionBuilder<CoinbaseTransaction>
{
    public CoinbaseBuilder(IAddressEncoder addressEncoder, ISignatureService signatureService) 
        : base(addressEncoder, signatureService)
    {
    }

    protected override CoinbaseTransaction BuildBaseTransaction(ICollection<TransactionEnvelope> transactions)
    {
        return new CoinbaseTransaction()
        {
            Amount = -50
        };
    }
}
```

## Configure our node

When we configure the IoC container for our blockchain node, we have several options
In this case we chose
 * To use MongoDB as the database
 * To use the Tcp peer network and listen on port 500
 * Use the multicast peer discovery protocol (to find other peers on the LAN)
 * Added our Transaction types that we defined earlier
 * Added our Transaction rules
 * Set the block time to 10 seconds

```c#
IServiceCollection services = new ServiceCollection();
services.AddBlockchain(x =>
{
    x.UseMongoDB(@"mongodb://localhost:27017", "my-currency-db")
        .UseTransactionRepository<ITransactionRepository, TransactionRepository>();
    x.UseTcpPeerNetwork(500);
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
```
