namespace WinterRose.Uris;

public static class UriContextParser
{
    public static UriContext Parse(string uriString)
    {
        Uri uri = new Uri(uriString);

        string command = uri.Host; 
        UriQuery query = new UriQuery();

        string queryString = uri.Query;

        if (!string.IsNullOrWhiteSpace(queryString))
        {
            string trimmed = queryString.TrimStart('?');
            string[] pairs = trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries);

            foreach (string pair in pairs)
            {
                string[] kv = pair.Split('=', 2);
                string key = kv[0];
                string value = kv.Length > 1 ? kv[1] : "";
                query[key] = Uri.UnescapeDataString(value);
            }
        }

        return new UriContext(command, query);
    }
}