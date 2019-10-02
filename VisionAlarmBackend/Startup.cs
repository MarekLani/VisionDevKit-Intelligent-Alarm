using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using System;

[assembly: FunctionsStartup(typeof(CaptureProcessor.Startup))]
namespace CaptureProcessor
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        { 
            builder.Services.AddScoped<FaceClient>((fc) =>
            {
                var key = Environment.GetEnvironmentVariable("FaceApiKey");
                return new FaceClient(new ApiKeyServiceClientCredentials(Environment.GetEnvironmentVariable("FaceApiKey"))) { Endpoint = Environment.GetEnvironmentVariable("FaceApiEndpoint") }; 
            });
            builder.Services.AddHttpClient();

        }
    }
}
