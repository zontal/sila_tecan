# The code generator of the Tecan SiLA2 SDK as .NET Tool

## Why should I use this package

Use this package to install SilaGen as a .NET Tool.

## Trimming and Native AOT

The generated data transfer objects include a compile-time protobuf-net type model per feature and the generated providers and clients pass serializers based on this model to the SDK, so the generated code is compatible with trimming and Native AOT.
The project that contains the generated code needs C# 12 or later (`<LangVersion>12</LangVersion>`, the default for .NET 8 and later). Use the option `--runtime-type-model` to generate code that uses the reflection-based protobuf-net runtime model instead, as previous versions of SilaGen did.

## Where can I get more information?

|||
| ---------------| ----------------------------------------------------------- |
| SiLA Homepage  | [https://sila-standard.com](https://sila-standard.com)      |
| Chat group     | [Join the group on Slack](https://sila-standard.org/slack)|
| Maintainer     | Georg Hinkel ([georg.hinkel@hs-rm.de](mailto:georg.hinkel@hs-rm.de)) of [Rhein-Main University of Applied Sciences](http://www.hs-rm.de)|
| Vulnerability Policy | https://sila-standard.com/vulnerability_policy |

The wiki includes quick start guides and tutorials as an easy entry point: [wiki](https://gitlab.com/SiLA2/vendors/sila_tecan/wikis/home)