using System.Collections.Generic;

namespace ConsulNotifier.Providers
{
    /// <summary>
    /// Bindings provider.
    /// </summary>
    public interface IBindingProvider
    {
        /// <summary>
        /// Returning information about endpoints.
        /// </summary>
        IEnumerable<EndpointInformation> GetRunningEndpointsInformation();
    }
}