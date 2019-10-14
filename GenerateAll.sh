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

$SILAGEN generate-server Build/Features/netstandard2.0/Tecan.Sila2.WorktableIntegration.dll Features/WorktableIntegration/WorktableIntegration.csproj -n Tecan.Sila2.WorktableIntegration -r Tecan.Sila2.WorktableIntegration

$SILAGEN generate-interface Tests/Generator.Tests/Logging/Feature.xml Tests/Generator.Tests/Logging/ServiceReference.cs -n Tecan.Sila2.Generator.Test.TestReference
$SILAGEN generate-provider Tests/Generator.Tests/Logging/Feature.xml Tests/Generator.Tests/Logging/DtoReference.cs Tests/Generator.Tests/Logging/ProviderReference.cs -n Tecan.Sila2.Generator.Test.TestReference
$SILAGEN generate-feature Build/Test/netcoreapp2.1/Tecan.Sila2Generator.Test.dll -i Tecan.Sila2.Generator.Test.Logging.ILoggingService Tests/Generator.Tests/Logging/Feature.xml

$SILAGEN generate-interface Tests/Generator.Tests/SilaService/Feature.xml Tests/Generator.Tests/SilaService/ServiceReference.cs -n Tecan.Sila2.Generator.Test.TestReference
$SILAGEN generate-provider Tests/Generator.Tests/SilaService/Feature.xml Tests/Generator.Tests/SilaService/DtoReference.cs Tests/Generator.Tests/SilaService/ProviderReference.cs -n Tecan.Sila2.Generator.Test.TestReference
$SILAGEN generate-feature Build/Test/netcoreapp2.1/Tecan.Sila2Generator.Test.dll -i Tecan.Sila2.Generator.Test.SilaService.ISiLAService Tests/Generator.Tests/SilaService/Feature.xml

$SILAGEN generate-interface Tests/Generator.Tests/TemperatureController/Feature.xml Tests/Generator.Tests/TemperatureController/ServiceReference.cs -n Tecan.Sila2.Generator.Test.TestReference
$SILAGEN generate-provider Tests/Generator.Tests/TemperatureController/Feature.xml Tests/Generator.Tests/TemperatureController/DtoReference.cs Tests/Generator.Tests/TemperatureController/ProviderReference.cs -n Tecan.Sila2.Generator.Test.TestReference
$SILAGEN generate-feature Build/Test/netcoreapp2.1/Tecan.Sila2Generator.Test.dll -i Tecan.Sila2.Generator.Test.TemperatureController.ITemperatureController Tests/Generator.Tests/TemperatureController/Feature.xml
