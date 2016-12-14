using System.Collections.Generic;
using ConsulNotifier.Extensions;
using Microsoft.Web.Administration;

namespace ConsulNotifier.Providers
{
    /// <inheritdoc cref="IBindingProvider"/>
    public class IISBindingProvider : IBindingProvider
    {
        /// <inheritdoc cref="IBindingProvider.GetRunningEndpointsInformation"/>
        public IEnumerable<EndpointInformation> GetRunningEndpointsInformation()
        {
            var serverManager = new ServerManager();

            return serverManager.GetRunningEndpointsInformation();
        }
    }
}