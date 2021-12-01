using Client.GreetingService;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Tecan.Sila2;
using Tecan.Sila2.Client;
using Tecan.Sila2.Client.ExecutionManagement;
using Tecan.Sila2.Discovery;
using Tecan.Sila2.DynamicClient;
using Tecan.Sila2.Locking;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            // by default, SiLA2 uses trusted connections, but as we are to lazy to setup certificates, we create a custom connector that accepts plaintext
            var connector = new PlaintextConnector(new DiscoveryExecutionManager());
            var discovery = new ServerDiscovery(connector);
            var executionManagerFactory = new ExecutionManagerFactory(new IClientRequestInterceptor[] { new LockingInterceptor() } );
            // we simply collect all servers that respond within 10 seconds. Other options exist if you know which server you are looking for.
            var servers = discovery.GetServers(TimeSpan.FromSeconds(10), nic => true );
            foreach (var s in servers)
            {
                Console.WriteLine($"Found server {s.Config.Name} ({s.Info.Type})");
            }

            var server = servers.First();
            // first, we use a strongly typed client
            var greetingClient = new GreetingServiceClient(server.Channel, executionManagerFactory.CreateExecutionManager(server));

            Console.WriteLine(greetingClient.SayHello("Client"));
            
            // lets go into an exception
            try
            {
                greetingClient.SayHello("Demo1");
            }
            catch (Exception exception)
            {
                // Note that the SDK automatically converts the SiLA2 error into a strongly typed UnfriendlyNameException
                Console.WriteLine(exception);
            }

            // lets demonstrate the usage of complex data structures
            var testStructure = greetingClient.EchoStructure(new DemoStructure( 42, 23));
            Console.WriteLine($"Echoed demo: X={testStructure.X}, Y={testStructure.Y}");

            // lets pass a stream and check that it was transferred correctly
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("This is a test string that is passed via SiLA2 as binary data")))
            {
                var resultStream = greetingClient.EchoStream(stream);
                using (var reader = new StreamReader(resultStream))
                {
                    Console.WriteLine(reader.ReadToEnd());
                }
            }

            // Lets query the greeting with the dynamic client
            var feature = server.Features.First(f => f.Identifier == "GreetingService");
            var featureContext = new FeatureContext(feature, server, executionManagerFactory.CreateExecutionManager(server));
            var command = feature.Items.OfType<FeatureCommand>().First();
            var commandClient = new NonObservableCommandClient(command, featureContext);

            var request = commandClient.CreateRequest();
            request.Value = new DynamicObject
            {
                Elements =
                {
                    new DynamicObjectProperty(command.Parameter[0])
                    {
                        Value = "Test"
                    }
                }
            };
            var response = commandClient.Invoke(request);
            Console.WriteLine(((DynamicObject)response.Value).Elements[0].Value);
        }
    }
}
