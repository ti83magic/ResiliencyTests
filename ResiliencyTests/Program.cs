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
        
        httpClientBuilder
            .AddStandardResilienceHandler();

        httpClientBuilder
            .AddStandardHedgingHandler(strategyBuilder =>
            {
                strategyBuilder.ConfigureOrderedGroups(x =>
                {
                    x.Groups.Add(new UriEndpointGroup
                    {
                        Endpoints =
                        [
                            new WeightedUriEndpoint
                            {
                                Uri = new Uri("https://nor-do-i.io/bbbbbb/"),
                                Weight = 1
                            }
                        ]
                    });
                });
            });
    });

var host = builder.Build();

var factory = host.Services.GetRequiredService<IHttpClientFactory>();
var client = factory.CreateClient(nameof(ISomeApi));
var request = new HttpRequestMessage(HttpMethod.Get, "some-endpoint");

await client.SendAsync(request);

host.Run();

public interface ISomeApi
{
}

public class SomeApi : ISomeApi
{
}