dotnet build Generator\Generator.csproj -c Release

Build\Generator\net472\SilaGen.exe generate-provider Framework\Discovery\SiLAService.sila.xml Framework\Discovery\SilaServiceDto.cs Framework\Discovery\SiLAServiceProvider.cs -n Tecan.Sila2.Discovery

Build\Generator\net472\SilaGen.exe generate-interface Features\Locking\LockController.sila.xml Features\Locking\ILockController.cs -n Tecan.Sila2.Locking
Build\Generator\net472\SilaGen.exe generate-provider Features\Locking\LockController.sila.xml Features\Locking\LockControlDtos.cs Features\Locking\LockControlProvider.cs -n Tecan.Sila2.Locking -s

Build\Generator\net472\SilaGen.exe generate-interface Features\Authentication\AuthenticationService.sila.xml Features\Authentication\Authentication\IAuthenticationService.cs -n Tecan.Sila2.Authentication
Build\Generator\net472\SilaGen.exe generate-provider Features\Authentication\AuthenticationService.sila.xml Features\Authentication\Authentication\AuthenticationDtos.cs Features\Authentication\Authentication\AuthenticationProvider.cs -n Tecan.Sila2.Authentication -s
Build\Generator\net472\SilaGen.exe generate-interface Features\Authentication\AuthorizationService.sila.xml Features\Authentication\Authorization\IAuthorizationService.cs -n Tecan.Sila2.Authorization
Build\Generator\net472\SilaGen.exe generate-provider Features\Authentication\AuthorizationService.sila.xml Features\Authentication\Authorization\AuthorizationDtos.cs Features\Authentication\Authorization\AuthorizationProvider.cs -n Tecan.Sila2.Authorization -s
Build\Generator\net472\SilaGen.exe generate-interface Features\Authentication\AuthorizationProviderService.sila.xml Features\Authentication\AuthorizationProvider\IAuthorizationProviderService.cs -n Tecan.Sila2.AuthorizationProvider
Build\Generator\net472\SilaGen.exe generate-provider Features\Authentication\AuthorizationProviderService.sila.xml Features\Authentication\AuthorizationProvider\AuthorizationProviderServiceDtos.cs Features\Authentication\AuthorizationProvider\AuthorizationProviderClient.cs -n Tecan.Sila2.AuthorizationProvider -c
Build\Generator\net472\SilaGen.exe generate-interface Features\Authentication\AuthorizationConfigurationService.sila.xml Features\Authentication\AuthorizationConfiguration\IAuthorizationConfigurationService.cs -n Tecan.Sila2.Authorization
Build\Generator\net472\SilaGen.exe generate-provider Features\Authentication\AuthorizationConfigurationService.sila.xml Features\Authentication\AuthorizationConfiguration\AuthorizationConfigurationServiceDtos.cs Features\Authentication\AuthorizationConfiguration\AuthorizationConfigurationServiceProvider.cs -n Tecan.Sila2.Authorization -s
Build\Generator\net472\SilaGen.exe generate-interface Features\ParameterConstraintsProvider\ParameterConstraintsProvider.sila.xml Features\ParameterConstraintsProvider\IParameterConstraintsProvider.cs -n Tecan.Sila2.ParameterConstraintsProvider
Build\Generator\net472\SilaGen.exe generate-provider Features\ParameterConstraintsProvider\ParameterConstraintsProvider.sila.xml Features\ParameterConstraintsProvider\ParameterConstraintDtos.cs Features\ParameterConstraintsProvider\ParameterConstraintsServiceProvider.cs -n Tecan.Sila2.ParameterConstraintsProvider -s

Build\Generator\net472\SilaGen.exe generate-server Build\Features\netstandard2.0\Tecan.Sila2.WorktableIntegration.dll Features\WorktableIntegration\WorktableIntegration.csproj -n Tecan.Sila2.WorktableIntegration

Build\Generator\net472\SilaGen.exe generate-interface Tests\Generator.Tests\Logging\Feature.xml Tests\Generator.Tests\Logging\ServiceReference.cs -n Tecan.Sila2.Generator.Test.TestReference
Build\Generator\net472\SilaGen.exe generate-provider Tests\Generator.Tests\Logging\Feature.xml Tests\Generator.Tests\Logging\DtoReference.cs Tests\Generator.Tests\Logging\ProviderReference.cs -n Tecan.Sila2.Generator.Test.TestReference
Build\Generator\net472\SilaGen.exe generate-feature Build\Test\net472\Tecan.Sila2Generator.Test.dll -i Tecan.Sila2.Generator.Test.Logging.ILoggingService Tests\Generator.Tests\Logging\Feature.xml

Build\Generator\net472\SilaGen.exe generate-interface Tests\Generator.Tests\SilaService\Feature.xml Tests\Generator.Tests\SilaService\ServiceReference.cs -n Tecan.Sila2.Generator.Test.TestReference
Build\Generator\net472\SilaGen.exe generate-provider Tests\Generator.Tests\SilaService\Feature.xml Tests\Generator.Tests\SilaService\DtoReference.cs Tests\Generator.Tests\SilaService\ProviderReference.cs -n Tecan.Sila2.Generator.Test.TestReference
Build\Generator\net472\SilaGen.exe generate-feature Build\Test\net472\Tecan.Sila2Generator.Test.dll -i Tecan.Sila2.Generator.Test.SilaService.ISiLAService Tests\Generator.Tests\SilaService\Feature.xml

Build\Generator\net472\SilaGen.exe generate-interface Tests\Generator.Tests\TemperatureController\Feature.xml Tests\Generator.Tests\TemperatureController\ServiceReference.cs -n Tecan.Sila2.Generator.Test.TestReference
Build\Generator\net472\SilaGen.exe generate-provider Tests\Generator.Tests\TemperatureController\Feature.xml Tests\Generator.Tests\TemperatureController\DtoReference.cs Tests\Generator.Tests\TemperatureController\ProviderReference.cs -n Tecan.Sila2.Generator.Test.TestReference
Build\Generator\net472\SilaGen.exe generate-feature Build\Test\net472\Tecan.Sila2Generator.Test.dll -i Tecan.Sila2.Generator.Test.TemperatureController.ITemperatureController Tests\Generator.Tests\TemperatureController\Feature.xml

Build\Generator\net472\SilaGen.exe generate-interface Tests\Generator.Tests\Dictionary\Feature.xml Tests\Generator.Tests\Dictionary\ServiceReference.cs -n Tecan.Sila2.Generator.Test.TestReference
Build\Generator\net472\SilaGen.exe generate-provider Tests\Generator.Tests\Dictionary\Feature.xml Tests\Generator.Tests\Dictionary\DtoReference.cs Tests\Generator.Tests\Dictionary\ProviderReference.cs -n Tecan.Sila2.Generator.Test.TestReference
Build\Generator\net472\SilaGen.exe generate-feature Build\Test\net472\Tecan.Sila2Generator.Test.dll -i Tecan.Sila2.Generator.Test.Dictionary.IDictionaryService Tests\Generator.Tests\Dictionary\Feature.xml

Build\Generator\net472\SilaGen.exe generate-interface Tests\Generator.Tests\InlineObservable\Feature.xml Tests\Generator.Tests\InlineObservable\ServiceReference.cs -n Tecan.Sila2.Generator.Test.TestReference
Build\Generator\net472\SilaGen.exe generate-feature Build\Test\net472\Tecan.Sila2Generator.Test.dll -i Tecan.Sila2.Generator.Test.TestReference.IInlineObservableService Tests\Generator.Tests\InlineObservable\Feature.xml

Build\Generator\net472\SilaGen.exe generate-server Build\Test\Features\ParameterDefaultsProvider\net472\Tecan.Sila2.ParameterDefaultsProvider.Tests.dll Tests\Features\ParameterDefaultsProvider.Tests\ParameterDefaultsProvider.Tests.csproj -n Tecan.Sila2.ParameterDefaultsProvider.Tests

Build\Generator\net472\SilaGen.exe generate-server Build\Test\TestServer\net472\TestServer.exe Tests\TestServer\TestServer.csproj -n Tecan.Sila2.IntegrationTests
Build\Generator\net472\SilaGen.exe generate-client Tests\TestServer\TestServer.sila.xml Tests\IntegrationTests\IntegrationTests.csproj -n Tecan.Sila2.IntegrationTests

Build\Generator\net472\SilaGen.exe generate-client Features\Authentication\AuthenticationService.sila.xml Features\Authentication.Client\Authentication.Client.csproj -n Tecan.Sila2.Authentication
Build\Generator\net472\SilaGen.exe generate-client Features\Locking\LockController.sila.xml Features\Locking.Client\Locking.Client.csproj -n Tecan.Sila2.Locking
Build\Generator\net472\SilaGen.exe generate-client Features\ParameterConstraintsProvider\ParameterConstraintsProvider.sila.xml Features\ParameterConstraintsProvider.Client\ParameterConstraintsProvider.Client.csproj -n Tecan.Sila2.ParameterConstraintsProvider
Build\Generator\net472\SilaGen.exe generate-client Features\WorktableIntegration\WorktableService.sila.xml Features\WorktableIntegration.Client\WorktableIntegration.Client.csproj -n Tecan.Sila2.WorktableIntegration
