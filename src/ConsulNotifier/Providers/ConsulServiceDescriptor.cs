namespace ConsulNotifier.Providers
{
    /// <summary>
    /// Descriptor with information about service in consul.
    /// </summary>
    public class ConsulServiceDescriptor
    {
        /// <summary>
        /// Name of service
        /// </summary>
        /// <remarks>
        /// For example *saritasa-hosting-80*
        /// </remarks>
        public string ServiceName { get; set; }

        /// <summary>
        /// Host name of consul service.
        /// </summary>
        /// <remarks>
        /// For example *saritasa-hosting.com*
        /// </remarks>
        public string Host { get; set; }
    }
}