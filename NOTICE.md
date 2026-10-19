# NOTICE — ZONTAL fork of the Tecan SiLA2 SDK

This repository is ZONTAL's fork of [SiLA2/vendors/sila_tecan](https://gitlab.com/SiLA2/vendors/sila_tecan), the
source of the `Tecan.Sila2` packages. The original work is Copyright (c) 2019, Tecan Trading AG, and licensed under the
BSD 3-Clause License (see [LICENSE](LICENSE)); the licence and copyright notices are unchanged. ZONTAL's changes are
released under the same licence.

## Base

- Upstream tag `4.4.8` on `master` (commit `adf5dba`, 2026-09-29). The branch `zontal/4.4.8` is that commit plus the
  changes below.

## Packages and version

Every package keeps its upstream id and is versioned **`4.4.8-zontal.1`**. The assembly versions are unchanged
(`4.4.0`, set in [CommonAssemblyInfo.cs](CommonAssemblyInfo.cs)); the assemblies are not strong-named, as upstream.

The packages are the ones of [NuSpec/](NuSpec): `Tecan.Sila2`, `Tecan.Sila2.AnIML`, `Tecan.Sila2.Annotations`,
`Tecan.Sila2.Client`, `Tecan.Sila2.Client.NetCore`, `Tecan.Sila2.Contracts`, `Tecan.Sila2.DynamicClient`,
`Tecan.Sila2.Features.Auth`, `Tecan.Sila2.Features.Auth.Client`, `Tecan.Sila2.Features.Cancellation`,
`Tecan.Sila2.Features.Cancellation.Client`, `Tecan.Sila2.Features.ConnectionConfiguration.Client`,
`Tecan.Sila2.Features.Locking`, `Tecan.Sila2.Features.Locking.Client`,
`Tecan.Sila2.Features.ParameterConstraintsProvider`, `Tecan.Sila2.Features.ParameterConstraintsProvider.Client`,
`Tecan.Sila2.Features.Recovery`, `Tecan.Sila2.Features.WorktableIntegration`,
`Tecan.Sila2.Features.WorktableIntegration.Client`, `Tecan.Sila2.Generator`, `Tecan.Sila2.Grpc`,
`Tecan.Sila2.NetCore`, `Tecan.Sila2.Server`, `Tecan.Sila2.Server.DryIoC`, `Tecan.Sila2.Server.NetCore`,
`Tecan.Sila2.ServerPool`, `Tecan.Sila2.ServerPool.NetCore`, and the .NET tool `Tecan.Sila2.SilaGen`. The
dependencies between them require `4.4.8-zontal.1` or later below 5.0, so a restore takes all of them from the fork.

The packages are built and published by [.github/workflows/zontal-pack.yml](.github/workflows/zontal-pack.yml) on a tag
`v4.4.8-zontal.*` to the zontal GitHub Packages feed (`https://nuget.pkg.github.com/zontal/index.json`, which requires
a token with `read:packages`) and to the static feed `https://zontal.github.io/nuget-feed/index.json`, which restores
without credentials. Map `Tecan.Sila2` and `Tecan.Sila2.*` to one of these feeds and everything else to nuget.org.
With that mapping, the tool installs with `dotnet tool install Tecan.Sila2.SilaGen --version 4.4.8-zontal.1`; where no
source mapping applies, add `--add-source https://zontal.github.io/nuget-feed/index.json` (the .NET SDK refuses
`--add-source` together with a source mapping).

## Changes: Native AOT and trimming

The fork removes the reflection-based code paths that fail in Native AOT and trimmed deployments. The changes are three
independent branches from `4.4.8`, proposed upstream in
[sila_tecan issue #32](https://gitlab.com/SiLA2/vendors/sila_tecan/-/issues/32) and published as branches of the
GitLab fork [wolfgang.colsman/sila_tecan](https://gitlab.com/wolfgang.colsman/sila_tecan):

| Branch | Change |
| --- | --- |
| [`aot/xml-feature-codec`](https://gitlab.com/wolfgang.colsman/sila_tecan/-/tree/aot/xml-feature-codec) | A handwritten XML reader and writer replaces `XmlSerializer` for Feature Definitions and the server configuration files. The public serializer APIs and the Feature Definition output are unchanged. On .NET Framework, a tab in a configuration attribute value is now escaped, so it round-trips as on .NET 8 and 10. |
| [`aot/command-line-parser`](https://gitlab.com/wolfgang.colsman/sila_tecan/-/tree/aot/command-line-parser) | The server command line (`ServerCommandLineArguments`) is parsed without CommandLineParser's reflection, with the same results, help and version output, and errors reported as CommandLineParser error types. The `[Option]` attributes and the CommandLineParser dependency stay. |
| [`aot/protobuf-generated-models`](https://gitlab.com/wolfgang.colsman/sila_tecan/-/tree/aot/protobuf-generated-models) | protobuf-net 3.2.56 → 3.4.30, compile-time protobuf-net type models for the SDK's own contracts (`FrameworkTypeModel`, the dynamic client, AnIML, and the Discovery and ConnectionConfiguration features), `RequiresUnreferencedCode`/`RequiresDynamicCode` annotations on the members that use the runtime type model, and the SilaGen option `--generated-type-model`, which emits a compile-time type model and serializers per feature (the generated code then requires C# 12). |

No public API is altered or removed. Existing code is unaffected unless SilaGen is run with `--generated-type-model`:
without the option, SilaGen generates the same code as `4.4.8`, which still compiles with C# 7.3. Consumers that pin
protobuf-net below 3.4.30 must raise that pin.

### Known residual AOT warnings

A Native AOT publish still reports warnings in code paths that these changes do not use by default:

- protobuf-net.Core's fallback to the runtime type model for types outside a compiled model;
- Common.Logging's configuration of the logger factory by type name;
- `Tecan.Sila2.AnIML`, which serializes AnIML documents with `XmlSerializer`;
- the dynamic client's `RuntimeObjectConverter`, which converts plain .NET objects to the `Any` type by reflection;
- the generic `ServiceMethodProvider<TService>` that `Tecan.Sila2.Server.NetCore` registers for the gRPC service
  registration (works at runtime in a Native AOT server).

## Other changes

Not proposed upstream: this notice, the README section, the `zontal-pack` workflow, and the repository URL of the
nuspec files, which names this repository so that each package records the commit it was built from.
