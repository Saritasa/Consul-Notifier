using Topshelf;

namespace ConsulNotifier
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = HostFactory.New(x =>
            {
                // Service description
                x.SetServiceName("ConsulNotifierService");
                x.SetDisplayName("Consul Notifier Service");
                x.SetDescription("Notify Consul application about new sites in current machine.");

                // Service settings
                x.EnableShutdown();
                x.StartAutomatically();
                x.RunAsLocalSystem();

                // Service recovery options
                x.EnableServiceRecovery(r =>
                {
                    r.RestartService(0);
                    r.OnCrashOnly();
                });

                // Service contract, only one per service
                x.Service<WindowsServiceHost>(s =>
                {
                    s.ConstructUsing(() => new WindowsServiceHost());
                    s.WhenStarted((svc, control) => svc.Start(control));
                    s.WhenStopped((svc, control) => svc.Stop(control));
                    s.WhenPaused((svc, control) => svc.Stop(control));
                    s.WhenContinued((svc, control) => svc.Start(control));
                });
            });

            host.Run();
        }
    }
}
