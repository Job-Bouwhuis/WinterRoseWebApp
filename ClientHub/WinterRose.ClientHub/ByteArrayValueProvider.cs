using System;
using WinterRose.WinterForgeSerializing;
using WinterRose.WinterForgeSerializing.Workers;

namespace WinterRose.ClientHub;

public class ByteArrayValueProvider : CustomValueProvider<byte[]>
{
    public override byte[]? CreateObject(object value, WinterForgeVM executor)
    {
        if (value is byte[] byteArray)
        {
            return byteArray;
        }
        if (value is string str)
        {
            if (str.StartsWith('"') && str.EndsWith('"'))
                str = str[1..^1];
            return Convert.FromBase64String(str);
        }
        throw new InvalidOperationException($"Cannot create byte[] from value of type {value.GetType().FullName}");

    }

    public override object CreateString(byte[] obj, ObjectSerializer serializer)
    {
        return Convert.ToBase64String(obj);
    }
}
