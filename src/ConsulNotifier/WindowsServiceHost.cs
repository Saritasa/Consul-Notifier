using System;
using System.Configuration;
using System.Threading.Tasks;
using ConsulNotifier.Providers;
using Serilog;
using Topshelf;

namespace ConsulNotifier
{
    /// <summary>
    /// Main entry point for windows service host.
    /// </summary>
    public class WindowsServiceHost : ServiceControl
    {
        private ILogger _logger;
        private Notifier _notifier;
        private volatile bool _isStopping;
        private TimeSpan _timeSpanBetweenNotifications;

        /// <summary>
        /// Debug level mark key.
        /// </summary>
        public const string IsDebugLevelKey = "IsDebugLevel";

        /// <summary>
        /// Key of configuration value for interval of consul's notification.
        /// </summary>
        public const string TimeSpanBetweenNotificationConfigKey = "TimeSpanBetweenNotification";

        /// <summary>
        /// Main entry point for start of windows service.
        /// </summary>
        public bool Start(HostControl hostControl)
        {
            _isStopping = false;

            try
            {
                Configure();
                var notifierLogger = _logger.ForContext<Notifier>();
                _notifier = new Notifier(notifierLogger, new IISBindingProvider(), new ConsulServicesProvider(notifierLogger));

                _logger.Debug("Service starting.");

                Task.Factory.StartNew(async () =>
                {
                    while (!_isStopping)
                    {
                        try
                        {
                            _logger.Debug("Starting notify.");
                            await _notifier.NotifyAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Exception occured during notify.");
                        }
                        finally
                        {
                            await Task.Delay(_timeSpanBetweenNotifications);
                        }
                    }
                });

                _logger.Debug("Service started.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to start service.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Entry point for service stop.
        /// </summary>
        public bool Stop(HostControl hostControl)
        {
            _logger.Debug("Stopping service.");
            _isStopping = true;
            _logger.Debug("Service stopped.");
            return true;
        }

        /// <summary>
        /// If debug key specified, we starting write logs to file
        /// </summary>
        /// <returns></returns>
        private bool IsDebugLevel()
        {
            var value = ConfigurationManager.AppSettings[IsDebugLevelKey];
            return !string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Configuring timespan between notification.
        /// </summary>
        private bool TryGetTimeSpanBetweenNotification(out TimeSpan timeSpan)
        {
            timeSpan = default(TimeSpan);
            var value = ConfigurationManager.AppSettings[TimeSpanBetweenNotificationConfigKey];
            if (string.IsNullOrWhiteSpace(value))
            {
                _logger.Error("Please configure timespan in seconds between notify.");

                return false;
            }

#pragma warning disable IDE0018 // Inline variable declaration
            var seconds = 0;
#pragma warning restore IDE0018 // Inline variable declaration
            if (!int.TryParse(value, out seconds))
            {
                _logger.Error("Please configure timespan in seconds between notify using int32 value.");

                return false;
            }

            timeSpan = TimeSpan.FromSeconds(seconds);

            return true;
        }

        /// <summary>
        /// Configure logging.
        /// </summary>
        private void Configure()
        {
            var messageTemplate = "[{Timestamp:HH:mm:ss} {Level}] {SourceContext:l} {Message}{NewLine}{Exception}";

            var loggerConfiguration = new LoggerConfiguration()
                .WriteTo.LiterateConsole(outputTemplate: messageTemplate)
                .WriteTo.EventLog("Consul Notifier", manageEventSource: true, outputTemplate: messageTemplate);

            if (IsDebugLevel())
            {
                loggerConfiguration.WriteTo.RollingFile("log.txt");
            }

            _logger = loggerConfiguration
                .MinimumLevel.Verbose()
                .CreateLogger()
                .ForContext<WindowsServiceHost>();

            if (!TryGetTimeSpanBetweenNotification(out _timeSpanBetweenNotifications))
            {
                throw new ConfigurationErrorsException("Can't configure timespan for notifications interval. " +
                                                       "Please configure it in seconds using " +
                                                       $"'{TimeSpanBetweenNotificationConfigKey}' as key " +
                                                       "in app or web config");
            }
        }
    }
}