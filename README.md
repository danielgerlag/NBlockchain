# NBlockchain

NBlockchain is a .NET standard library for building blockchain applications.

**This project is currently in alpha status and any contributions are welcome.**

The idea is that the developer would only need to focus on the data and rules for a blockchain and not worry about having to build all the infrastructure to facilitate a blockchain.

The developer would need to
 * Define the schema of data / transactions they would like to store on the blockchain
 * Define the rules for a valid transaction
 * Select or create an appropriate local database
 * Select or create an appropriate network implementation
 * Select or create one or more peer discovery implementations

Beyond this, it is meant to be highly customizable, you can switch out the default services for
 * Address encoding
 * Signing
 * Hashing algorithm
 * Block verification
 * Block consensus method (eg. proof of work, etc...)

## Installation

Using Nuget package console
```
PM> Install-Package NBlockchain -Version 0.5.0-alpha
```
Using .NET CLI
```
dotnet add package NBlockchain --version 0.5.0-alpha
```

## Samples
 * [Digital Currency](Samples/DigitalCurrency)

## Local database stores
 * LiteDB (Default built-in)
 * [MongoDB](Providers/NBlockchain.MongoDB)

## Networking implementations
 * In memory (mostly for testing & demo purposes)
 * Tcp sockets

## Peer discovery implementations
 * Static (from a config file, etc...)
 * Multicast (for finding peers on the local network)
 * More to come....

## Key features
* Automatic chain fork detection and resolution
* Open, flexible transaction schema
* Customizable transaction level rules
* Customizable block level rules
* Peer discovery
* Proof of work management

## Documentation
https://github.com/danielgerlag/NBlockchain/tree/master/doc

## Outstanding items for v1 
 * NAT traversal
 * More peer discovery options
 * Integration tests

## Authors
 * **Daniel Gerlag** - daniel@gerlag.ca

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
