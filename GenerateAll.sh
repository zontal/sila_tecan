#!/bin/bash

SILAGEN='./Build/Generator/netcoreapp2.1/linux-x64/SilaGen'

dotnet build Generator/Generator.csproj -f netcoreapp2.1 -c Release -r linux-x64

$SILAGEN generate-provider Framework/Discovery/SiLAService.sila.xml Framework/Discovery/SilaServiceDto.cs Framework/Discovery/SiLAServiceProvider.cs -n Tecan.Sila2.Discovery

$SILAGEN generate-interface Features/Locking/LockController.sila.xml Features/Locking/ILockController.cs -n Tecan.Sila2.Locking
$SILAGEN generate-provider Features/Locking/LockController.sila.xml Features/Locking/LockControlDtos.cs Features/Locking/LockControlProvider.cs -n Tecan.Sila2.Locking -s

$SILAGEN generate-interface Features/Authentication/AuthenticationService.sila.xml Features/Authentication/Authentication/IAuthenticationService.cs -n Tecan.Sila2.Authentication
$SILAGEN generate-provider Features/Authentication/AuthenticationService.sila.xml Features/Authentication/Authentication/AuthenticationDtos.cs Features/Authentication/Authentication/AuthenticationProvider.cs -n Tecan.Sila2.Authentication -s
$SILAGEN generate-interface Features/Authentication/AuthorizationService.sila.xml Features/Authentication/Authorization/IAuthorizationService.cs -n Tecan.Sila2.Authorization
$SILAGEN generate-provider Features/Authentication/AuthorizationService.sila.xml Features/Authentication/Authorization/AuthorizationDtos.cs Features/Authentication/Authorization/AuthorizationProvider.cs -n Tecan.Sila2.Authorization -s
$SILAGEN generate-interface Features/Authentication/AuthorizationProviderService.sila.xml Features/Authentication/AuthorizationProvider/IAuthorizationProviderService.cs -n Tecan.Sila2.AuthorizationProvider
$SILAGEN generate-provider Features/Authentication/AuthorizationProviderService.sila.xml Features/Authentication/AuthorizationProvider/AuthorizationProviderServiceDtos.cs Features/Authentication/AuthorizationProvider/AuthorizationProviderClient.cs -n Tecan.Sila2.AuthorizationProvider -c
$SILAGEN generate-interface Features/Authentication/AuthorizationConfigurationService.sila.xml Features/Authentication/AuthorizationConfiguration/IAuthorizationConfigurationService.cs -n Tecan.Sila2.Authorization
$SILAGEN generate-provider Features/Authentication/AuthorizationConfigurationService.sila.xml Features/Authentication/AuthorizationConfiguration/AuthorizationConfigurationServiceDtos.cs Features/Authentication/AuthorizationConfiguration/AuthorizationConfigurationServiceProvider.cs -n Tecan.Sila2.Authorization -s
$SILAGEN generate-interface Features/ParameterConstraintsProvider/ParameterConstraintsProvider.sila.xml Features/ParameterConstraintsProvider/IParameterConstraintsProvider.cs -n Tecan.Sila2.ParameterConstraintsProvider
$SILAGEN generate-provider Features/ParameterConstraintsProvider/ParameterConstraintsProvider.sila.xml Features/ParameterConstraintsProvider/ParameterConstraintDtos.cs Features/ParameterConstraintsProvider/ParameterConstraintsServiceProvider.cs -n Tecan.Sila2.ParameterConstraintsProvider -s
$SILAGEN generate-interface Features/Cancellation/CancelController.sila.xml Features/Cancellation/ICancellationProvider.cs -n Tecan.Sila2.Cancellation
$SILAGEN generate-provider Features/Cancellation/CancelController.sila.xml Features/Cancellation/CancellationDtos.cs Features/Cancellation/CancellationProvider.cs -n Tecan.Sila2.Cancellation -s

$SILAGEN generate-server Build/Features/netstandard2.0/Tecan.Sila2.WorktableIntegration.dll Features/WorktableIntegration/WorktableIntegration.csproj -n Tecan.Sila2.WorktableIntegration -v 1.0

$SILAGEN generate-interface Tests/Generator.Tests/Logging/Feature.xml Tests/Generator.Tests/Logging/ServiceReference.cs -n Tecan.Sila2.Generator.Test.TestReference
$SILAGEN generate-provider Tests/Generator.Tests/Logging/Feature.xml Tests/Generator.Tests/Logging/DtoReference.cs Tests/Generator.Tests/Logging/ProviderReference.cs -n Tecan.Sila2.Generator.Test.TestReference
$SILAGEN generate-feature Build/Test/netcoreapp2.1/Tecan.Sila2Generator.Test.dll -i Tecan.Sila2.Generator.Test.Logging.ILoggingService Tests/Generator.Tests/Logging/Feature.xml

$SILAGEN generate-interface Tests/Generator.Tests/SilaService/Feature.xml Tests/Generator.Tests/SilaService/ServiceReference.cs -n Tecan.Sila2.Generator.Test.TestReference
$SILAGEN generate-provider Tests/Generator.Tests/SilaService/Feature.xml Tests/Generator.Tests/SilaService/DtoReference.cs Tests/Generator.Tests/SilaService/ProviderReference.cs -n Tecan.Sila2.Generator.Test.TestReference
$SILAGEN generate-feature Build/Test/netcoreapp2.1/Tecan.Sila2Generator.Test.dll -i Tecan.Sila2.Generator.Test.SilaService.ISiLAService Tests/Generator.Tests/SilaService/Feature.xml

$SILAGEN generate-interface Tests/Generator.Tests/TemperatureController/Feature.xml Tests/Generator.Tests/TemperatureController/ServiceReference.cs -n Tecan.Sila2.Generator.Test.TestReference
$SILAGEN generate-provider Tests/Generator.Tests/TemperatureController/Feature.xml Tests/Generator.Tests/TemperatureController/DtoReference.cs Tests/Generator.Tests/TemperatureController/ProviderReference.cs -n Tecan.Sila2.Generator.Test.TestReference
$SILAGEN generate-feature Build/Test/netcoreapp2.1/Tecan.Sila2Generator.Test.dll -i Tecan.Sila2.Generator.Test.TemperatureController.ITemperatureController Tests/Generator.Tests/TemperatureController/Feature.xml

$SILAGEN generate-interface Tests/Generator.Tests/Dictionary/Feature.xml Tests/Generator.Tests/Dictionary/ServiceReference.cs -n Tecan.Sila2.Generator.Test.TestReference
$SILAGEN generate-provider Tests/Generator.Tests/Dictionary/Feature.xml Tests/Generator.Tests/Dictionary/DtoReference.cs Tests/Generator.Tests/Dictionary/ProviderReference.cs -n Tecan.Sila2.Generator.Test.TestReference
$SILAGEN generate-feature Build/Test/netcoreapp2.1/Tecan.Sila2Generator.Test.dll -i Tecan.Sila2.Generator.Test.Dictionary.IDictionaryService Tests/Generator.Tests/Dictionary/Feature.xml

$SILAGEN generate-interface Tests/Generator.Tests/InlineObservable/Feature.xml Tests/Generator.Tests/InlineObservable/ServiceReference.cs -n Tecan.Sila2.Generator.Test.TestReference
$SILAGEN generate-feature Build/Test/net472/Tecan.Sila2Generator.Test.dll -i Tecan.Sila2.Generator.Test.TestReference.IInlineObservableService Tests/Generator.Tests/InlineObservable/Feature.xml

$SILAGEN generate-server Build/Test/Features/ParameterDefaultsProvider/netcoreapp2.1/Tecan.Sila2.ParameterDefaultsProvider.Tests.dll Tests/Features/ParameterDefaultsProvider.Tests/ParameterDefaultsProvider.Tests.csproj -n Tecan.Sila2.ParameterDefaultsProvider.Tests

$SILAGEN generate-server Build/Test/TestServer/netcoreapp2.1/TestServer.exe Tests/TestServer/TestServer.csproj -n Tecan.Sila2.IntegrationTests
$SILAGEN generate-client Tests/TestServer/TestServer.sila.xml Tests/IntegrationTests/IntegrationTests.csproj -n Tecan.Sila2.IntegrationTests

$SILAGEN generate-client Features/Authentication/AuthenticationService.sila.xml Features/Authentication.Client/Authentication.Client.csproj -n Tecan.Sila2.Authentication
$SILAGEN generate-client Features/Authentication/AuthorizationConfigurationService.sila.xml Features/Authentication.Client/Authentication.Client.csproj -n Tecan.Sila2.Authorization
$SILAGEN generate-client Features/Authentication/AuthorizationProviderService.sila.xml Features/Authentication.Client/Authentication.Client.csproj -n Tecan.Sila2.Authorization
$SILAGEN generate-client Features/Locking/LockController.sila.xml Features/Locking.Client/Locking.Client.csproj -n Tecan.Sila2.Locking
$SILAGEN generate-client Features/ParameterConstraintsProvider/ParameterConstraintsProvider.sila.xml Features/ParameterConstraintsProvider.Client/ParameterConstraintsProvider.Client.csproj -n Tecan.Sila2.ParameterConstraintsProvider
$SILAGEN generate-client Features/WorktableIntegration/WorktableService.sila.xml Features/WorktableIntegration.Client/WorktableIntegration.Client.csproj -n Tecan.Sila2.WorktableIntegration
$SILAGEN generate-client Features/Cancellation/CancelController.sila.xml Features/Cancellation.Client/Cancellation.Client.csproj -n Tecan.Sila2.Cancellation

$SILAGEN generate-client Interoperability/InteropServer/ComplexDataTypeTest.sila.xml Interoperability/InteropClient/InteropClient.csproj -n Tecan.Sila2.Interop.Client
$SILAGEN generate-client Interoperability/InteropServer/DataTypeTest.sila.xml Interoperability/InteropClient/InteropClient.csproj -n Tecan.Sila2.Interop.Client
$SILAGEN generate-client Interoperability/InteropServer/ObservableCommandTest.sila.xml Interoperability/InteropClient/InteropClient.csproj -n Tecan.Sila2.Interop.Client
$SILAGEN generate-client Interoperability/InteropServer/ObservablePropertyTest.sila.xml Interoperability/InteropClient/InteropClient.csproj -n Tecan.Sila2.Interop.Client
$SILAGEN generate-client Interoperability/InteropServer/ParameterConstraintsTest.sila.xml Interoperability/InteropClient/InteropClient.csproj -n Tecan.Sila2.Interop.Client

$SILAGEN generate-interface -n Tecan.Sila2.Interop.Server Interoperability/InteropServer/ComplexDataTypeTest.sila.xml Interoperability/InteropServer/ComplexDataTypeTest/Interface.cs
$SILAGEN generate-interface -n Tecan.Sila2.Interop.Server Interoperability/InteropServer/DataTypeTest.sila.xml Interoperability/InteropServer/DataTypeTest/Interface.cs
$SILAGEN generate-interface -n Tecan.Sila2.Interop.Server Interoperability/InteropServer/ObservableCommandTest.sila.xml Interoperability/InteropServer/ObservableCommandTest/Interface.cs
$SILAGEN generate-interface -n Tecan.Sila2.Interop.Server Interoperability/InteropServer/ObservablePropertyTest.sila.xml Interoperability/InteropServer/ObservablePropertyTest/Interface.cs
$SILAGEN generate-interface -n Tecan.Sila2.Interop.Server Interoperability/InteropServer/ParameterConstraintsTest.sila.xml Interoperability/InteropServer/ParameterConstraintsTest/Interface.cs

$SILAGEN generate-provider -n Tecan.Sila2.Interop.Server Interoperability/InteropServer/ComplexDataTypeTest.sila.xml Interoperability/InteropServer/ComplexDataTypeTest/Dtos.cs Interoperability/InteropServer/ComplexDataTypeTest/Provider.cs -s
$SILAGEN generate-provider -n Tecan.Sila2.Interop.Server Interoperability/InteropServer/DataTypeTest.sila.xml Interoperability/InteropServer/DataTypeTest/Dtos.cs Interoperability/InteropServer/DataTypeTest/Provider.cs -s
$SILAGEN generate-provider -n Tecan.Sila2.Interop.Server Interoperability/InteropServer/ObservableCommandTest.sila.xml Interoperability/InteropServer/ObservableCommandTest/Dtos.cs Interoperability/InteropServer/ObservableCommandTest/Provider.cs -s
$SILAGEN generate-provider -n Tecan.Sila2.Interop.Server Interoperability/InteropServer/ObservablePropertyTest.sila.xml Interoperability/InteropServer/ObservablePropertyTest/Dtos.cs Interoperability/InteropServer/ObservablePropertyTest/Provider.cs -s
$SILAGEN generate-provider -n Tecan.Sila2.Interop.Server Interoperability/InteropServer/ParameterConstraintsTest.sila.xml Interoperability/InteropServer/ParameterConstraintsTest/Dtos.cs Interoperability/InteropServer/ParameterConstraintsTest/Provider.cs -s
