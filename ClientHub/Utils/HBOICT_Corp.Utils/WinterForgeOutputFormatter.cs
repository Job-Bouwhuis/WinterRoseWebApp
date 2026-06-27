using Microsoft.AspNetCore.Mvc.Formatters;
using WinterRose.WinterForgeSerializing;

namespace WinterRose.Web.Utils;

public sealed class WinterForgeOutputFormatter : IOutputFormatter
{
    private const string CONTENT_TYPE = "application/winterforge";

    public bool CanWriteResult(OutputFormatterCanWriteContext context) => true;

    public async Task WriteAsync(OutputFormatterWriteContext context)
    {
        context.HttpContext.Response.ContentType = CONTENT_TYPE;

        MemoryStream mem = new MemoryStream();
        WinterForge.SerializeToStream(context.Object!, mem, TargetFormat.FormattedHumanReadable);
        mem.Position = 0;

        await mem.CopyToAsync(context.HttpContext.Response.Body);
    }
}
