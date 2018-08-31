using System;
using ConsulNotifier.Providers;

namespace ConsulNotifier
{
    public class NotifierFactory
    {
        public static Notifier Create(Action<NotifierConfigurator> dependenciesResolver)
        {
            var context = new NotifierContext();
            var dependencies = new NotifierConfigurator(context);
            dependenciesResolver(dependencies);
            return new Notifier(context.Logger, context.BindingProvider, new ConsulServicesProvider(context.Logger));
        }
    }
}
