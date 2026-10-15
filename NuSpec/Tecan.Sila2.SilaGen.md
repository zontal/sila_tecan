# The code generator of the Tecan SiLA2 SDK as .NET Tool

## Why should I use this package

Use this package to install SilaGen as a .NET Tool.

## Trimming and Native AOT

By default, SilaGen generates the same code as previous versions: the generated data transfer objects are serialized through the reflection-based protobuf-net runtime model, and the generated code compiles with the C# language version that the project already uses (C# 7.3 and later).

Use the option `--generated-type-model` with `generate-provider`, `generate-server` or `generate-client` if the project that contains the generated code is published with trimming or Native AOT. With this option, the generated data transfer objects include a compile-time protobuf-net type model per feature, and the generated providers and clients pass serializers based on this model to the SDK, so that no reflection-based serialization is needed at runtime.
The model is implemented by the protobuf-net source generator, which requires C# 12 or later in the project that contains the generated code (`<LangVersion>12</LangVersion>` or later; the default for .NET 8 and later, but not for .NET Framework or .NET Standard projects).

## Where can I get more information?

|||
| ---------------| ----------------------------------------------------------- |
| SiLA Homepage  | [https://sila-standard.com](https://sila-standard.com)      |
| Chat group     | [Join the group on Slack](https://sila-standard.org/slack)|
| Maintainer     | Georg Hinkel ([georg.hinkel@hs-rm.de](mailto:georg.hinkel@hs-rm.de)) of [Rhein-Main University of Applied Sciences](http://www.hs-rm.de)|
| Vulnerability Policy | https://sila-standard.com/vulnerability_policy |

The wiki includes quick start guides and tutorials as an easy entry point: [wiki](https://gitlab.com/SiLA2/vendors/sila_tecan/wikis/home)