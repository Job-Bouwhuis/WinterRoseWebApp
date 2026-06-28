namespace WinterRose.DependancyInjection;

using System.Net.Http;

public static class HttpClientServiceExtensions
{
    extension(ServiceBuilder builder)
    {
        public ServiceBuilder AddHttpClient()
        {
            builder.AddSingleton<HttpClient>();
            return builder;
        }
    }
}