namespace Tecan.Sila2.Discovery
{
    /// <summary>
    /// Constant parameters required for constructing a SiLA service and discovering
    /// 
    /// Mainly consists of values for parameters required to construct the Service Instance Name
    /// as per the spec (https://tools.ietf.org/html/rfc6763#section-4.1)
    /// </summary>
    public static class DiscoveryConstants
    {
        /// <summary>
        /// Portion of the service instance name consisting of DNS labels
        /// </summary>
        public const string ServiceName = "_sila._tcp";

        /// <summary>
        /// Specifies the DNS subdomain within which those names are registered
        /// </summary>
        public const string ServiceDomainName = "local";

        /// <summary>
        /// Pattern which is expected to exist in a the instance name portion of 
        /// the SiLA service
        /// 
        /// TODO: any discovered _sila.tcp.local service must also be checked for the service instance name and validate that it contains a GUID i.e. must match the InstanceNamePattern which should be some form of REGEX pattern
        /// </summary>
        public const string InstanceNamePattern = null;

    }
}
