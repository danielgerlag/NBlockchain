# MongoDB Provider for NBlockchain


## Usage

```c#
services.AddBlockchain(x =>
{
    x.UseMongoDB(@"mongodb://localhost:27017", "my-blockchain-db")
        .UseTransactionRepository<ITransactionRepository, TransactionRepository>();
	...
}
```