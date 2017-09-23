# MongoDB Provider for NBlockchain

## Installation

Using Nuget package console
```
PM> Install-Package NBlockchain.MongoDB -Version 0.5.0-alpha
```
Using .NET CLI
```
dotnet add package NBlockchain.MongoDB --version 0.5.0-alpha
```

## Usage

```c#
services.AddBlockchain(blockchain =>
{       
    blockchain.UseMongoDB(@"mongodb://localhost:27017", "my-blockchain-db")
        .UseInstructionRepository<ICustomInstructionRepository, CustomMongoInstructionRepository>();
	...
}
```
