using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConsulNotifier.Providers;
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
        private ConsulServiceProvider _consulServiceProvider;
        private ILogger _logger;

        /// <summary>
        /// Ctor.
        /// </summary>
        public Notifier(ILogger logger, IBindingProvider bindingProvider, ConsulServiceProvider consulServiceProvider)
        {
            _bindingProvider = bindingProvider;
            _consulServiceProvider = consulServiceProvider;
            _logger = logger.ForContext<Notifier>();
        }

        /// <summary>
        /// Notifiyng consul about new services.
        /// </summary>
        /// <returns></returns>
        public async Task NotifyAsync()
        {
            var consulServices = await _consulServiceProvider.RetrieiveCurrentServicesDescriptorsAsync();
            var consulNodeServiceNames = await _consulServiceProvider.RetrieveServiceNamesFromNodeAsync();
            var currentEndpoints = _bindingProvider.GetRunningEndpointsInformation(_logger);
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
                await this._consulServiceProvider.DeregisterServicesAsync(serviceIdsForDeregister);
            }
            else
            {
                _logger.Information("Nothing new to deregister.");
            }
        }

        /// <summary>
        /// Sending new hostnames.
        /// </summary>
        private async Task SendNewServicesAsync(List<EndpointInformation> newHostInformations)
        {
            if (newHostInformations.Any())
            {
                await this._consulServiceProvider.SendNewServicesAsync(newHostInformations);
            }
            else
            {
                _logger.Information("Nothing new to register.");
            }
        }
    }
}