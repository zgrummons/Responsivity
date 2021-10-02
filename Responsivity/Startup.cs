using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]
public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        //Add configuration. 
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("host.settings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.email.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        builder.Services.AddSingleton(configuration.Get<Config>());
        builder.Services.AddSingleton<IDictionary<string, App>>(new Dictionary<string, App>());
        // Register your email service here
    }
}
