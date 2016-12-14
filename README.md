Consul Notifier
============

The application as a windows service for notifying Consul about changes
of running sites on running host.

Service running as Local System for access to IIS Management subsystem.

## Current features ##

 1. Notify consul about new binding by determine Service Id(format: hostname-port).
 2. Notify consul about stop or remove bindings when site is stopped. 
 3. Adding custom tags to consul service(currently is hardcoded).
 4. Configurable interval between notification of consul through application configuration value using configuration/appSettings/add[@key='TimeSpanBetweenNotification'] value in seconds.
 5. Consul endpoint determining by configuration value using configuration/appSettings/add[@key='ConsulEndpoint'].
 6. Type of site can be only www. Traefik doesn't support nowadays other services like FTP.

## Things need to be done ##
 1. Real-time notification about launching/stopping sites.
 2. Determine type of site(ftp, www).
 3. Configurable tags for registration of service in Consul.
 4. More flexible comparing of host by information from Consul.
 5. Log level segregation.
 6. Async execution control (e.g. service sending new registrations, but previous request is not ended).

## Used frameworks/technologies ##
  1. [Topshelf](http://topshelf-project.com/)
  2. [Serilog](serilog.net)
  3. [Microsoft.Web.Administration](https://msdn.microsoft.com/en-us/library/microsoft.web.administration(v=vs.90).aspx)

## Usage ##

Specify "ConsulEndpoint" in application config, example -

```xml
<add key="ConsulEndpoint" value="http://localhost:8500/"/>
```

Specify "TimeSpanBetweenNotification" value in seconds for application config - example

```xml
<add key="TimeSpanBetweenNotification" value="90" />
```

### Contributors ###
Saritasa LLC

### License ###
Licensed under [MIT](https://opensource.org/licenses/MIT).