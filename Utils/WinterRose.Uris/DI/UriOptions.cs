using System.Data;

namespace WinterRose.Uris;

public class UriOptions
{
    public string AppId
    {
        get
        {
            if (field is null)
                throw new NoNullAllowedException(
                    $"App ID in type UriOptions can not be null. Please configure UriOptions");
            return field;
        }
        set;
    }

    public string Scheme   
    {
        get
        {
            if (field is null)
                throw new NoNullAllowedException(
                    $"Scheme in type UriOptions can not be null. Please configure UriOptions");
            return field;
        }
        set;
    }

    public string DisplayName    
    {
        get
        {
            if (field is null)
                throw new NoNullAllowedException(
                    $"DisplayName in type UriOptions can not be null. Please configure UriOptions");
            return field;
        }
        set;
    }
}