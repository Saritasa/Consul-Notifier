using System.Collections.Generic;
using Newtonsoft.Json;

namespace ConsulNotifier.ConsulDtos
{
    /// <summary>
    /// Service part of <see cref="ConsulAgentRegistryDTO"/>.
    /// </summary>
    public class ConsulServiceDTO
    {
        /// <summary>
        /// Id of service, must be unique.
        /// </summary>
        [JsonProperty("ID")]
        public string Id { get; set; }

        /// <summary>
        /// Name of service for consul.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Tags of service.
        /// </summary>
        public IEnumerable<string> Tags { get; set; }

        /// <summary>
        /// Address of current host.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Current port of running service.
        /// </summary>
        public int Port { get; set; }
    }
}
