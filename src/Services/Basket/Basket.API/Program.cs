﻿using Basket.API.Infrastructure.Middlewares;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using System;
using System.IO;

namespace Microsoft.eShopOnContainers.Services.Basket.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseFailing(options =>
                {
                    options.ConfigPath = "/Failing";
                })
                .UseHealthChecks("/hc")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((builderContext, config) =>
                {
                    var builtConfig = config.Build();
                    
                    var configurationBuilder = new ConfigurationBuilder();

                    if (Convert.ToBoolean(builtConfig["UseVault"]))
                    {
                        configurationBuilder.AddAzureKeyVault(
                            $"https://{builtConfig["Vault:Name"]}.vault.azure.net/",
                            builtConfig["Vault:ClientId"],
                            builtConfig["Vault:ClientSecret"]);
                    }

                    configurationBuilder.AddEnvironmentVariables();

                    config.AddConfiguration(configurationBuilder.Build());
                })
                .ConfigureLogging((hostingContext, builder) =>
                {
                    builder.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    builder.AddConsole();
                    builder.AddDebug();
                })
                .UseApplicationInsights()
                .UseSerilog((builderContext, config) =>
                {
                    config
                        .MinimumLevel.Information()
                        .Enrich.FromLogContext();                        

                    if (builderContext.Configuration["UseElasticSearch"] == Boolean.TrueString)
                    {
                        config.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(builderContext.Configuration["ElasticSearchSvcUrl"]))
                        {
                            MinimumLogEventLevel = LogEventLevel.Verbose,
                            AutoRegisterTemplate = true
                        })
                        .WriteTo.Console()
                        .CreateLogger();
                    }
                    else
                    {
                        config.WriteTo.Console();
                    }
                })
                .Build();
    }
}
