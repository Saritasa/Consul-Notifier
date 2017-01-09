using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using ConsulNotifier.Providers;
using Microsoft.Web.Administration;
using Serilog;

namespace ConsulNotifier.Extensions
{
    /// <summary>
    /// Extensions for <see cref="ServerManager"/>.
    /// </summary>
    public static class ServerManagerExtensions
    {
        private const string BindingSchemaName = "binding";
        public const string NotifyConsulFlagConfigurationKey = "NotifyConsul";

        /// <summary>
        /// Getting endpoint information from IIS.
        /// </summary>
        public static IEnumerable<EndpointInformation> GetRunningEndpointsInformation(this ServerManager @this, ILogger logger)
        {
            var sites = @this.Sites;

            var hostIp = string.Empty;
            var machineName = Dns.GetHostName();
            var host = Dns.GetHostEntry(machineName);

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    hostIp = ip.ToString();
                }
            }

            foreach (var site in sites)
            {
                if (!IsValidSiteForNotification(site))
                {
                    logger.Warning("Site is not valid for notification(e.g. not configured notification propagation on configuration of site) {@site}", site);
                    continue;
                }

                foreach (var siteBinding in site.Bindings)
                {
                    if (siteBinding.Schema.Name != BindingSchemaName)
                    {
                        logger.Warning("Binding scheme is not valid, must be {@schemaName}, but {@siteSchema}", BindingSchemaName, siteBinding.Schema.Name);
                        continue;
                    }

                    var endpointInfo = new EndpointInformation
                    {
                        HostName = string.IsNullOrWhiteSpace(siteBinding.Host) ? machineName : siteBinding.Host,
                        Address = siteBinding.EndPoint.Address.GetAddressBytes().All(x => x == 0) ? hostIp : siteBinding.EndPoint.Address.ToString(),
                        Port = siteBinding.EndPoint.Port,
                        ApplicationName = site.Name,
                        MachineName = machineName
                    };

                    logger.Information("Retrieved from IIS endpoint information {@endpoint}", endpointInfo);

                    yield return endpointInfo;
                }
            }
        }

        private static bool IsValidSiteForNotification(Site site)
        {
            var configuration = site.GetWebConfiguration();
            var notifyConsulFlagRaw = string.Empty;
            var notifyConsulFlag = false;

            if (site.State != ObjectState.Started)
            {
                return false;
            }

            try
            {
                var section = configuration.GetSection("appSettings");
                var keys = section.GetCollection()
                    .ToDictionary(x => x.GetAttribute("key"), x => x.GetAttribute("value"));

                foreach (var kvp in keys)
                {
                    if (NotifyConsulFlagConfigurationKey == (string)kvp.Key.Value)
                    {
                        notifyConsulFlagRaw = (string)kvp.Value.Value;
                    }
                }

                if (bool.TryParse(notifyConsulFlagRaw, out notifyConsulFlag) && notifyConsulFlag)
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }
    }
}