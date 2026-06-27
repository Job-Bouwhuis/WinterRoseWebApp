using Microsoft.Extensions.Logging.Console;
using WinterRose.ClientHub.Feature.InformationRelay.Services;
using WinterRose.Web.Logging;
using WinterRose.Web.Problems;
using WinterRose.Web.Utils;
using WinterRose.Web.Validation.Middleware;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        builder.Logging.UseRecordiumLogger();

        builder.Services.AddValidation();
        builder.Services.AddGracefulProblemDetails();

        builder.Services.AddHttpClient<AppServerClient>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["AppServer:BaseUrl"]
                    ?? throw new InvalidOperationException("AppServer:BaseUrl configuration is missing"));
        });


        builder.Services.AddControllers(options =>
        {
            options.OutputFormatters.Add(new WinterForgeOutputFormatter());
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}