using Common.Logging;
using Tecan.Sila2.Discovery;

LogManager.Adapter = new Common.Logging.Simple.DebugLoggerFactoryAdapter();

var connector = new ServerConnector(new DiscoveryExecutionManager());
var discovery = new ServerDiscovery(connector);
var servers = discovery.GetServers(TimeSpan.FromSeconds(10), nic => true);

foreach(var server in servers)
{
    Console.WriteLine(server.Config.Name);
	foreach (var feature in server.Features)
	{
		Console.WriteLine($"- {feature.DisplayName}");
	}
}