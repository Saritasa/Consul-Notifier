using System.Net.Http;
using ConsulNotifier.Providers;
using Serilog;

namespace ConsulNotifier
{
    /// <summary>
    /// Context for support fluent configuration.
    /// </summary>
    public class NotifierContext
    {
        /// <summary>
        /// Provided logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Bindings provider contract.
        /// </summary>
        public IBindingProvider BindingProvider { get; set; }

        /// <summary>
        /// Used to send http requests.
        /// </summary>
        public HttpClient HttpClient { get; set; }
    }
}
