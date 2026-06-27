using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using WinterRose.WinterForgeSerializing;

namespace WinterRose.Web.Utils;

public sealed class WinterForgeInputFormatter : InputFormatter
{
    private const string CONTENT_TYPE = "application/winterforge";

    public WinterForgeInputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(CONTENT_TYPE));
    }

    protected override bool CanReadType(Type type)
    {
        return true;
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(
        InputFormatterContext context)
    {
        var request = context.HttpContext.Request;

        var obj = WinterForge.DeserializeFromHumanReadableStream(request.Body);

        if(obj is null or Nothing)
            return await InputFormatterResult.FailureAsync();
        if (obj.GetType() != context.ModelType)
            return await InputFormatterResult.FailureAsync();

        return await InputFormatterResult.SuccessAsync(obj);
    }
}