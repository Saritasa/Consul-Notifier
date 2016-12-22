using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsulNotifier.ConsulDtos;
using ConsulNotifier.Providers;
using Newtonsoft.Json;
using Serilog;

namespace ConsulNotifier
{
    /// <summary>
    /// Consul notifier.
    /// </summary>
    public class Notifier
    {
        /// <summary>
        /// local provider for notify remote consul.
        /// </summary>
        private IBindingProvider _bindingProvider;

        /// <summary>
        /// Remove consul services provider.
        /// </summary>
        private ConsulServicesProvider _consulServiceProvider;
        private ILogger _logger;
        private string _consulEndpoint;

        /// <summary>
        /// Ctor.
        /// </summary>
        public Notifier(ILogger logger, IBindingProvider bindingProvider, ConsulServicesProvider consulServiceProvider)
        {
            _bindingProvider = bindingProvider;
            _consulServiceProvider = consulServiceProvider;
            _logger = logger.ForContext<Notifier>();

            InitializeConfiguraiton();
        }

        private void InitializeConfiguraiton()
        {
            var consulEndpoint = ConfigurationManager.AppSettings[ConsulServicesProvider.ConsulEndpointConfigurationKey];

            if (string.IsNullOrWhiteSpace(consulEndpoint))
            {
                throw new ConfigurationErrorsException(
                    "Please configure Consul endpoint in app config. Example: ConsulEndpoint='www.consul.com:80'");
            }

            _consulEndpoint = consulEndpoint;
        }

        /// <summary>
        /// Name of current machin node.
        /// </summary>
        private string NodeName() => Dns.GetHostName();

        /// <summary>
        /// Notifiyng consul about new services.
        /// </summary>
        /// <returns></returns>
        public async Task NotifyAsync()
        {
            var consulServices = await _consulServiceProvider.RetrieiveCurrentServicesDescriptorsAsync();
            var consulNodeServiceNames = await _consulServiceProvider.RetrieveServiceNamesFromNodeAsync(NodeName());
            var currentEndpoints = _bindingProvider.GetRunningEndpointsInformation();
            var newServices = new List<EndpointInformation>();

            var visitedServiceIds = new List<string>();
            var serviceIdsForDeregister = new List<string>();

            _logger.Information("Existing hostnames {@hostNames}", consulServices);

            foreach (var endpointInformation in currentEndpoints)
            {
                var serviceId = $"{endpointInformation.HostName}-{endpointInformation.Port}";

                // checking up name of host and port
                if (!consulServices.Any(x => x.ServiceName == serviceId))
                {
                    _logger.Information("Adding host to new hostnames - {@hostName}", endpointInformation);

                    newServices.Add(endpointInformation);
                }
                else
                {
                    _logger.Information("Endpoint already exist - {@endoint}", endpointInformation);
                }

                visitedServiceIds.Add(serviceId);
            }

            foreach (var consulNodeServiceId in consulNodeServiceNames)
            {
                if (!visitedServiceIds.Contains(consulNodeServiceId))
                {
                    serviceIdsForDeregister.Add(consulNodeServiceId);
                }
            }

            await DeregisterServicesAsync(serviceIdsForDeregister);
            await SendNewServicesAsync(newServices);
        }

        private async Task DeregisterServicesAsync(List<string> serviceIdsForDeregister)
        {
            if (serviceIdsForDeregister.Any())
            {
                var httpClient = new HttpClient();

                foreach (var serviceId in serviceIdsForDeregister)
                {
                    var uri = new Uri($"{_consulEndpoint}/v1/agent/service/deregister/{serviceId}");
                    var response = await httpClient.GetAsync(uri);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        _logger.Warning($"An error occured while deregistering service {serviceId}, response - {@response}", response);
                    }
                    else
                    {
                        _logger.Information($"Successfully deregistered service {serviceId} from node {NodeName()}");
                    }
                }
            }
            else
            {
                _logger.Information("Nothing new to deregister.");
            }
        }

        /// <summary>
        /// Preparing dtos to send.
        /// </summary>
        private List<ConsulServiceDTO> PrepareDtos(List<EndpointInformation> newHostInformations)
        {
            var newRegistryDtos = new List<ConsulServiceDTO>();
            foreach (var newHostInformation in newHostInformations)
            {
                var newRegistryDto = new ConsulServiceDTO
                {
                    Address = newHostInformation.Address,
                    Port = newHostInformation.Port,
                    Id = $"{newHostInformation.ApplicationName}-{newHostInformation.Port}",
                    Name = $"{newHostInformation.ApplicationName}-{newHostInformation.Port}",
                    Tags = new[]
                        {
                            $"traefik.frontend.rule=Host:{newHostInformation.HostName}",
                            "traefik.tags=app",
                            "traefik.backend.loadbalancer=drr"
                        }
                };

                newRegistryDtos.Add(newRegistryDto);
            }

            return newRegistryDtos;
        }

        /// <summary>
        /// Sending new hostnames.
        /// </summary>
        private async Task SendNewServicesAsync(List<EndpointInformation> newHostInformations)
        {
            var dtos = PrepareDtos(newHostInformations);
            var uri = new Uri($"{_consulEndpoint}/v1/agent/service/register");
            var httpClient = new HttpClient();

            if (!dtos.Any())
            {
                _logger.Information("Nothing new to send.");
                return;
            }

            _logger.Information("Trying to send new services.");

            try
            {
                foreach (var registryDto in dtos)
                {
                    _logger.Information("Sending {@dto}", registryDto);
                    var serializedDto = JsonConvert.SerializeObject(registryDto);
                    var stringContent = new StringContent(serializedDto, Encoding.UTF8, "application/json");

                    var response = await httpClient.PutAsync(uri, stringContent, CancellationToken.None);
                    _logger.Information("Sended {@dto}", registryDto);

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        _logger.Warning("An error occured while sending new service, response - {@response}", response);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Can't send messages");
            }
        }
    }
}