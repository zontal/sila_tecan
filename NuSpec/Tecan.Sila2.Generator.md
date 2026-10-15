# The code generator of the Tecan SiLA2 SDK for customization purposes

## Why should I use this package

Use this package if you want to customize SilaGen with your own extensions.

The generators produce code that serializes data transfer objects through the reflection-based protobuf-net runtime model unless `GeneratorSettings.UseGeneratedTypeModel` is set (command line option `--generated-type-model`), in which case they add a compile-time protobuf-net type model per feature for trimming and Native AOT; that code requires C# 12 or later.

## Where can I get more information?

|||
| ---------------| ----------------------------------------------------------- |
| SiLA Homepage  | [https://sila-standard.com](https://sila-standard.com)      |
| Chat group     | [Join the group on Slack](https://sila-standard.org/slack)|
| Maintainer     | Georg Hinkel ([georg.hinkel@hs-rm.de](mailto:georg.hinkel@hs-rm.de)) of [Rhein-Main University of Applied Sciences](http://www.hs-rm.de)|
| Vulnerability Policy | https://sila-standard.com/vulnerability_policy |

The wiki includes quick start guides and tutorials as an easy entry point: [wiki](https://gitlab.com/SiLA2/vendors/sila_tecan/wikis/home)