using WinterRose.WinterForgeSerializing;

namespace WinterRose.Web;

public static class HttpClientExtensions
{
    extension (HttpClient client)
    {
        public async Task<T> GetFromWinterForge<T>(string requestUri)
        {
            var response = await client.GetAsync(requestUri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            object? result = WinterForge.DeserializeFromHumanReadableStream(response.Content.ReadAsStream());
            if(result is null or Nothing)
                throw new InvalidDataException("The response content returned no data");

            if(result is T typedResult)
                return typedResult;

            throw new InvalidDataException($"The response content could not be deserialized to the expected type {typeof(T).FullName}");
        }
    }
}
