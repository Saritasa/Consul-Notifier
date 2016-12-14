using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace ConsulNotifier.Providers
{
    /// <summary>
    /// Retrieving information about <see cref="ConsulServiceDescriptor"/>
    /// which contains existing services data.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// ```C#
    /// var consulServicesProvider = new ConsulServicesProvider(_logger);
    /// var descriptors = consulServicesProvider.RetrieiveCurrentServiceDescriptors();
    /// ```
    /// </remarks>
    public class ConsulServicesProvider
    {
        private Regex _ruleRegex = new Regex(@"^(?<ruleName>.+)\=(?<ruleValue>.+)$",
            RegexOptions.Singleline | RegexOptions.Compiled);

        private Regex _hostRegex = new Regex(@"^Host\:(?<value>.+)$", RegexOptions.Singleline | RegexOptions.Compiled);

        private const string FrontendRuleKey = "traefik.frontend.rule";

        /// <summary>
        /// Configuration **key** which **must** be stored with value in **application config**.
        /// </summary>
        public const string ConsulEndpointConfigurationKey = "ConsulEndpoint";
        private string _consulEndpoint;
        private ILogger _logger;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <exception cref="ConfigurationErrorsException">Throws when application has wrong configuration.</exception>
        public ConsulServicesProvider(ILogger logger)
        {
            _logger = logger.ForContext<ConsulServicesProvider>();

            InitConfiguration();
        }

        /// <summary>
        /// Initialization of configuration.
        /// </summary>
        /// <exception cref="ConfigurationErrorsException"></exception>
        private void InitConfiguration()
        {
            var consulEndpointConfigurationValue = ConfigurationManager.AppSettings[ConsulEndpointConfigurationKey];
            if (string.IsNullOrWhiteSpace(consulEndpointConfigurationValue))
            {
                throw new ConfigurationErrorsException(
                    "Please configure Consul endpoint in app config. Example: ConsulEndpoint='www.consul.com:80'");
            }

            _consulEndpoint = consulEndpointConfigurationValue;
        }

        /// <summary>
        /// Trying to get host tag value from consul response.
        /// </summary>
        /// <param name="tag">Tag value.</param>
        /// <param name="host">Out host</param>
        /// <returns></returns>
        private bool TryGetHostTag(string tag, out string host)
        {
            host = string.Empty;

            if (_ruleRegex.IsMatch(tag))
            {
                var ruleMatch = _ruleRegex.Match(tag);
                var ruleNameGroup = ruleMatch.Groups["ruleName"];
                var ruleValueGroup = ruleMatch.Groups["ruleValue"];
                var ruleName = string.Empty;
                var ruleValue = string.Empty;

                if (ruleNameGroup.Success)
                {
                    ruleName = ruleNameGroup.Value;
                }
                if (ruleValueGroup.Success)
                {
                    ruleValue = ruleValueGroup.Value;
                }

                if (!string.IsNullOrWhiteSpace(ruleName) && !string.IsNullOrWhiteSpace(ruleValue))
                {
                    if (ruleName != FrontendRuleKey && !_hostRegex.IsMatch(ruleValue))
                    {
                        return false;
                    }

                    var hostNameMatch = _hostRegex.Match(ruleValue);
                    var valueGroup = hostNameMatch.Groups["value"];
                    if (valueGroup.Success)
                    {
                        host = valueGroup.Value;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieving services from consul endpoint.
        /// </summary>
        /// <remarks>
        /// 
        /// Sample of response -
        /// 
        /// ```javascript
        /// {
        ///     "agent-instance-4500": ["udp"],
        ///     "agent-instance-500": ["udp"],
        ///     "amcaregivers-backend-develop": ["traefik.frontend.rule=Host:amcaregivers-backend.saritasa-hosting.com", "traefik.tags=app", "traefik.backend.loadbalancer=drr"]
        /// }
        /// ```
        /// </remarks>
        public async Task<IEnumerable<ConsulServiceDescriptor>> RetrieiveCurrentServicesDescriptorsAsync()
        {
            var services = await RetrieveCurrentServicesAsync();
            var set = new HashSet<ConsulServiceDescriptor>();

            foreach (var service in services)
            {
                var serviceName = service.Name;
                foreach (var tags in service)
                {
                    foreach (var tag in tags)
                    {
                        var jValue = tag as JValue;
                        if (jValue == null)
                        {
                            continue;
                        }

                        var host = string.Empty;
                        if (TryGetHostTag(jValue.Value.ToString(), out host))
                        {
                            set.Add(new ConsulServiceDescriptor
                            {
                                ServiceName = serviceName,
                                Host = host
                            });
                        }
                    }
                }
            }

            return set;
        }

        /// <summary>
        /// Returning enumeration of strings like 'helios.umbrella.com-80' where numbers is port of running application
        /// </summary>
        /// <param name="nodeName">Name of node, typicaly name of host machine where running this service.</param>
        public async Task<IEnumerable<string>> RetrieveServiceNamesFromNodeAsync(string nodeName)
        {
            if (string.IsNullOrWhiteSpace(nodeName))
            {
                throw new ArgumentNullException(nameof(nodeName));
            }

            var result = await RetrieveServicesFromNodeAsync(nodeName);

            if (result?.Services == null)
            {
                return Enumerable.Empty<string>();
            }

            var serviceNames = new List<string>();

            foreach (var service in result.Services)
            {
                foreach (var _service in service)
                {
                    var serviceName = (string)_service.Service;
                    if (string.IsNullOrWhiteSpace(serviceName))
                    {
                        continue;
                    }

                    serviceNames.Add(serviceName);
                }
            }

            return serviceNames;
        }

        /// <summary>
        /// Retrieving services details and node from specified node.
        /// </summary>
        /// <param name="nodeName">Name of node.</param>
        private async Task<dynamic> RetrieveServicesFromNodeAsync(string nodeName)
        {
            var httpClient = new HttpClient();
            var uriBuilder = new UriBuilder($"{_consulEndpoint}v1/catalog/node/{nodeName}");
            var responseMessage = await httpClient.GetAsync(uriBuilder.Uri);

            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException();
            }

            var rawJson = await responseMessage.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<dynamic>(rawJson);
        }

        /// <summary>
        /// Retrieving all current services from consul.
        /// </summary>
        private async Task<dynamic> RetrieveCurrentServicesAsync()
        {
            var httpClient = new HttpClient();
            var uriBuilder = new UriBuilder($"{_consulEndpoint}v1/catalog/services");
            var responseMessage = await httpClient.GetAsync(uriBuilder.Uri);

            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException();
            }

            var rawJson = await responseMessage.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<dynamic>(rawJson);
        }
    }
}