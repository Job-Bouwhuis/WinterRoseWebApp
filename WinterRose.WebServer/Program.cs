using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WinterRose.Web.Logging;
using WinterRose.Web.Utils;
using WinterRose.WebServer.Data;
using WinterRose.WebServer.Features;
using WinterRose.WebServer.Features.Account;
using WinterRose.WebServer.Features.Api.Apps.Services;
using WinterRose.WebServer.Features.Dashboard.Services;
using WinterRose.WebServer.Features.FileUploads.Endpoints;
using WinterRose.WebServer.Features.FileUploads.Services;

namespace WinterRose.WebServer;

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

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<IdentityRedirectManager>();
        builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services.AddSingleton<AppRepository>();

        builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
        builder.Services.AddScoped<DashboardDataService>();
        builder.Services.AddSingleton<UploadQueue>();
        builder.Services.AddSingleton<UploadService>();

        builder.Services.AddHostedService<DiffCreatorService>();

        builder.Services.AddSingleton<AppDiffService>();
        

        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = long.MaxValue;
        });

        builder.Services.AddControllers(options =>
        {
            options.OutputFormatters.Add(new WinterForgeOutputFormatter());
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage(new DeveloperExceptionPageOptions()
            {
                FileProvider = app.Services.GetRequiredService<IWebHostEnvironment>().ContentRootFileProvider,
                SourceCodeLineCount = 11
            });
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapAdditionalIdentityEndpoints();

        app.MapControllers();

        app.AddDiffFileDownloads();

        app.Run();
    }
}