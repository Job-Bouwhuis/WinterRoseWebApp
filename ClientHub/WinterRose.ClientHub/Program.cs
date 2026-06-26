using WinterRose.ClientHub.Feature.InformationRelay.Services;
using WinterRose.Web.Problems;
using WinterRose.Web.Validation.Middleware;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddValidation();
        builder.Services.AddGracefulProblemDetails();

        builder.Services.AddHttpClient<AppServerClient>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["AppServer:BaseUrl"]
                    ?? throw new InvalidOperationException("AppServer:BaseUrl configuration is missing"));
        });

        builder.Services.AddControllers();

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