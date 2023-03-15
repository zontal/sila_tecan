# Tecan SiLA2 SDK
This repository contains an alternative implementation of the SiLA 2 standard using .NET. It is aimed to enable a fast development process for SiLA2 Servers where developers start with a .NET interface as contract and generate all necessary code to publish an implementation of this contract by means of an automated code generator. In the opposite direction, a
.NET interface can be generated from a feature description, together with an implementation of this interface that fulfills the interface by calling a SiLA2 Server.

It is intended to provide the content for all operating systems that support the .NET Core or .NET Framework platform. The contents of the repository are open-source and are not limited to Tecan. Besides the reference to the Tecan logging contracts, no Tecan-specific code is involved.

|||
| ---------------| ----------------------------------------------------------- |
| SiLA Homepage  | [https://sila-standard.com](https://sila-standard.com)      |
| Chat group     | [Join the group on Slack](https://sila-standard.org/slack)|
| Maintainer     | Georg Hinkel ([georg.hinkel@hs-rm.de](mailto:georg.hinkel@hs-rm.de)) of [Rhein-Main University of Applied Sciences](http://www.hs-rm.de)|


## Wiki
The wiki includes quick start guides and tutorials as an easy entry point: [wiki](https://gitlab.com/SiLA2/vendors/sila_tecan/wikis/home)

## Status
The Tecan SiLA2 SDK covers all features of SiLA2 1.0 such as binary transfer, any types, nested data types, metadata and many more. The repository also offers a dynamic client where developers can access functionality of a server without knowing this functionality at compile-time.
The functionality of the Tecan SiLA2 SDK is covered by a range of automated unit tests and integration tests that are partially integrated into the CI build.

New commits to the Tecan SiLA2 SDK are automatically checked by means of automated unit tests. If they succeed, NuGet packages are automatically pushed to the official NuGet feed.

For more general information about the standard, we refer to the [sila_base](https://gitlab.com/SiLA2/sila_base) repository.

## Cloning
Clone the repository either with HTTPs or SSH (choose the appropriate link in the clone button)
```bash
git clone https://gitlab.com/SiLA2/vendors/sila_tecan.git
```

As an entry point, refer to `Examples/HelloSila.sln`.

## Contributing

To get involved, read our [contributing docs](https://gitlab.com/SiLA2/sila_base/blob/master/CONTRIBUTING.md) in [sila_base](https://gitlab.com/SiLA2/sila_base).

### C# Style Guide

Please follow the [general C# coding guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/).

## Getting Started

If you would like to get started with this repository then please refer to the [Tutorial how to create a SiLA2 Server](https://gitlab.com/SiLA2/vendors/sila_tecan/-/wikis/Tutorial-Create-a-SiLA2-Server).

### Using the Library
If you would like to use the latest release of the project just add the nuget repository to your feed and 
add the library as a dependecy to your project, as described in the [Tutorial how to create a SiLA2 Server](https://gitlab.com/SiLA2/vendors/sila_tecan/-/wikis/Tutorial-Create-a-SiLA2-Server).

Note that if your server (or client) requires support of the Any Type, please do not forget to also add a reference to the dynamic client package.

## Development Guidelines

### Testing and Debugging
You can just execute the tests locally using dotnet test.

### Driver Development targeting .NET Framework
The Tecan SiLA2 SDK is built using .NET standard which is the base layer libraries that can be used 
by both the .NET core and .NET framework runtimes. Therefore (unless otherwise necessary) 
your base libraries should be built for .NET standard such that .NET core and .NET framework 
applications can use them.

In most cases, one might want to use Tecan SiLA2 SDK to develop a Device driver which requires 
interaction with the instrument via a .NET Framework API. In this case your Device driver 
application should be built against a .NET Framwork.

## Issues and Bugs
If you see an issue please feel free to file a bug on the project [list of issues](https://gitlab.com/SiLA2/vendors/sila_tecan/issues)

## License
This code is licensed under the [New BSD License](https://choosealicense.com/licenses/bsd-3-clause/)
