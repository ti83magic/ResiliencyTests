using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;

var builder = Host
    .CreateDefaultBuilder(args)
    .ConfigureServices(serviceCollection =>
    {
        
        var httpClientBuilder = serviceCollection
            .AddHttpClient<ISomeApi, SomeApi>(x =>
            {
                x.BaseAddress = new Uri("https://i-do-not-exist.io/aaaaaa/");
            });

        string[] hedgeUris =
        [
            "https://nor-do-i.io/bbbbbb/",
            "https://nor-i.io/cccccc/"
        ];
        
        httpClientBuilder
            .AddStandardResilienceHandler();
        
        httpClientBuilder
            .AddStandardHedgingHandler(strategyBuilder =>
            {
                strategyBuilder.ConfigureOrderedGroups(x =>
                {
                    foreach (var uri in hedgeUris)
                    {
                        x.Groups.Add(new UriEndpointGroup
                        {
                            Endpoints = [
                                new WeightedUriEndpoint
                                {
                                    Uri = new Uri(uri),
                                    Weight = 1
                                }
                            ]
                        });
                    }
                });
            })
            .Configure(x =>
            {
                x.Hedging.MaxHedgedAttempts = hedgeUris.Length;

                x.Hedging.OnHedging = args =>
                {
                    Console.WriteLine("!!!!! OnHedging triggered.");
                    return default;
                };
                
                x.Hedging.ActionGenerator = args =>
                {
                    Console.WriteLine("!!!!! ActionGenerator triggered."); // Does not seem to trigger
                    return () => args.Callback(args.ActionContext);
                };
            });
    });

var host = builder.Build();

var factory = host.Services.GetRequiredService<IHttpClientFactory>();
var client = factory.CreateClient(nameof(ISomeApi));
var request = new HttpRequestMessage(HttpMethod.Get, "some-endpoint");

var response = await client.SendAsync(request);


host.Run();

public interface ISomeApi
{
}

public class SomeApi : ISomeApi
{
}