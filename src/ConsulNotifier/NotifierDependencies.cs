using ConsulNotifier.Providers;
using Serilog;

namespace ConsulNotifier
{
    /// <summary>
    /// Used for 'fluent' configuration,
    /// </summary>
    public class NotifierConfigurator
    {
        private NotifierContext notifierContext;

        public NotifierConfigurator(NotifierContext context)
        {
            notifierContext = context;
        }

        /// <summary>
        /// Using provided logger for log information in processing.
        /// </summary>
        public void UseLogger(ILogger logger)
        {
            notifierContext.Logger = logger;
        }

        /// <summary>
        /// Using passed provider for retrieve bindings.
        /// </summary>
        public void UseBindingProvider(IBindingProvider bindingProvider)
        {
            notifierContext.BindingProvider = bindingProvider;
        }
    }
}
