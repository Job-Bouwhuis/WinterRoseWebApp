using WinterRose.WinterForgeSerializing;

namespace WinterRose.Nexus.Shared;

public static class HttpClientExtensions
{
    extension (HttpClient client)
    {
        public async Task<T> GetFromWinterForge<T>(string requestUri)
        {
            var response = await client.GetAsync(requestUri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            
            using MemoryStream mem = new MemoryStream();
            using MemoryStream mem2 = new MemoryStream();
            await response.Content.CopyToAsync(mem).ConfigureAwait(false);
            mem.Position = 0;
            WinterForge.ConvertFromStreamToStream(mem, mem2, TargetFormat.Optimized);
            mem2.Position = 0;
            
            object? result = WinterForge.DeserializeFromStream(mem2);
            if(result is null or Nothing)
                throw new InvalidDataException("The response content returned no data");

            if(result is T typedResult)
                return typedResult;

            throw new InvalidDataException($"The response content could not be deserialized to the expected type {typeof(T).FullName}");
        }
    }
}
