namespace WinterRose.DependancyInjection;

using System.Net.Http;

public static class HttpClientServiceExtensions
{
    extension(IServiceBuilder builder)
    {
        public IServiceBuilder AddHttpClient()
        {
            builder.AddSingleton<HttpClient>();
            return builder;
        }
    }
}