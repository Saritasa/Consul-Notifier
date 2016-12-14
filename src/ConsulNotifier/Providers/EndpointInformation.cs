namespace ConsulNotifier.Providers
{
    /// <summary>
    /// Information of IIS endpoint(binding).
    /// </summary>
    public class EndpointInformation
    {

        /// <summary>
        /// Name of web host if specified, otherwise name of machine host.
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// IP Address of binding, usually is empty in IIS and here is ip of host.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Running port of binding.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Name of application in IIS Manager.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Name of machine.
        /// </summary>
        public string MachineName { get; set; }
    }
}