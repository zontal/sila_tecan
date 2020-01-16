using System.IO;
using Microsoft.Extensions.Configuration;
using Common.Logging;
using Common.Logging.Configuration;

namespace Tecan.Sila2.Logging
{
    public static class Logging
    {
        /// <summary>Configures the applications logging, to consume Common.Logging abstractins as specified by the config file</summary>
        public static void SetupCommonLogging(string configFile = "logging.config")
        {
            var config = LoadConfigXml(configFile);
            var logConfiguration = new LogConfiguration();
            config.GetSection("common:logging").Bind(logConfiguration);
            LogManager.Configure(logConfiguration);
        }

        private static string GetAssemblyPath()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }


        private static IConfiguration LoadConfigXml(string fileName)
        {
            return new ConfigurationBuilder()
                .SetBasePath(GetAssemblyPath())
                .AddXmlFile(fileName, optional: true, reloadOnChange: false)
                .Build();
        }

    }
}
