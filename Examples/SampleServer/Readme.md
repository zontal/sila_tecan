# Example dotnet core SiLA2-Server

This Example consists of a dummy SiLA2-Server-Implementation, that showcases the usage of the sila_tecan Framework. It consists of a static and a dynamic property as well as of a simple an an obervable command. The later showcases SiLA's complete functionality including both final and intermediate results and the usage of data structures as return types.

## Running the Example

To run the example simply start the *SilaService*-project:

```bash
$mporr:~/sila_tecan/Examples/SampleServer/SampleService$
dotnet run
```

Or optionally specify the interface and port:

```bash
$mporr:~/sila_tecan/Examples/SampleServer/SampleService$
dotnet run -- -i 10.42.1.173 -p 40123
```

Now the server can be used by any appropriate SiLA2-client. For example use the [SiLA-Browser](https://gitlab.com/SiLA2/sila_base/-/wikis/SiLA-Browser-Quickstart#run-the-sila-2-browser)
(Please note that the SiLA-Browser is currently not able to display the (Intermediate)Results of observable commands correctly).

## Remarks

For detailed instructions on how to write a SiLA2-Server in sila_tecan please refer to the [wiki (Tutorial Create a SiLA2 Server)](https://gitlab.com/SiLA2/vendors/sila_tecan/-/wikis/Tutorial-Create-a-SiLA2-Server).

In addition note some differences in this example from the principles shown in the Tutorial:

1. This Server only consists of two Projects
    1. The *SampleImplementation* contains both the annotated Interface (see *ISampleService.cs*) and the server's implementation code (see *SampleImplementation.cs*)
    2. The *SampleService* contains of the auto generated glue-code (dtos and provider) and runs as an executable starting the Bootstrapper
2. This implementation uses platform independent dotnet core
    1. To do server generation ("glue code": dtos and provider) on linux machines use the *Generator*-project from the sila_tecan git repository
    * Example: assuming sila_tecan was cloned into user's home dir and we want to build the SampleServer:

        ```bash
        $mporr:~/sila_tecan/Examples/SampleServer/SampleImplementation$
        dotnet build

        $mporr:~/sila_tecan/Examples/SampleServer/$
        cd ../../../Generator/

        $mporr:~/sila_tecan/Generator$
        dotnet run -f netcoreapp2.1 -- generate-server -r SampleService ../Examples/SampleServer/SampleImplementation/bin/Debug/netstandard2.0/SampleImplementation.dll ../Examples/SampleServer/SampleService/
        ```

    2. Please note the additional required parameter *root namespace (-r)* when using dotnet core!
    This parameter needs to be set to the main's namespace of the project the dtos and provider are added to (in this case it's thw *SampleService*-project with the root namespace *SampleService*)
3. The projects are designed in a way that they use the locally cloned git repo when it exists in the parent folder. Otherwise they will install the Sila.Tecan.* Packages from NuGet.org
