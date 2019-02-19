using System;
using ConsulNotifier.Providers;

namespace ConsulNotifier
{
    /// <summary>
    /// Factory for creating <see cref="Notifier"/>.
    /// </summary>
    public class NotifierFactory
    {
        /// <summary>
        /// Factory method for <see cref="Notifier"/>
        /// </summary>
        public static Notifier Create(Action<NotifierConfigurator> dependenciesResolver)
        {
            var context = new NotifierContext();
            var dependencies = new NotifierConfigurator(context);
            dependenciesResolver(dependencies);
            return new Notifier(context.Logger, context.BindingProvider, new ConsulServiceProvider(context.Logger, context.HttpClient));
        }
    }
}
