using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace WinterRose.Web.Validation.Middleware;

public class ConfigureMvcOptions : IConfigureOptions<MvcOptions>
{
    private readonly TrackingJsonInputFormatter formatter;

    public ConfigureMvcOptions(TrackingJsonInputFormatter formatter)
    {
        this.formatter = formatter;
    }

    public void Configure(MvcOptions options)
    {
        options.InputFormatters.Insert(0, formatter);
    }
}
