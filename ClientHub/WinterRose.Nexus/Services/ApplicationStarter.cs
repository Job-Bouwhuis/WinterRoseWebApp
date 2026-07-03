using System;
using System.Net.Http;
using System.Threading.Tasks;
using WinterRose.DependancyInjection.Logging;
using WinterRose.Nexus.Interface;
using WinterRose.Nexus.Interface.Dialogs;
using WinterRose.Nexus.Interface.Windows;
using WinterRose.Nexus.Services;
using WinterRose.Nexus.Shared;
using WinterRose.Nexus.Utils;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;

public class ApplicationStarter(ApplicationInstaller installer,
    ClientAppRepository appRepo,
    AppServerClient server,
    IApplicationLauchManager appLauncher,
    MainThread main,
    IServiceProvider services,
    IModalDialog dialog,
    ILogger<ApplicationStarter> logger)
{
    
    private const string NEXUS_APPROVED_ARG = "--nexus-approved";
    
    public async Task Start(string? appId, bool startAfterUpdate)
    {
        if (appId is null)
        {
            await dialog.ShowAsync("Application id not specified");
            return;
        }

        var window = await main.InvokeAsync(() =>
        {
            var w = new InstallationProgressWindow(main, services);
            w.Show();
            w.ShowCheckingUpdates();
            return w;
        });
        
        AppEntry serverEntry = null!;
        bool skipVersionCheck = false;
        try
        {
            serverEntry = await server.GetAppEntryAsync(appId);
            
            if (serverEntry is null)
            {
                await installer.UninstallApplicationAsync(appId);
                await dialog.ShowAsync("The application is no longer available on Nexus, and has been deleted from your machine.");
                return;
            }
        }
        catch (HttpRequestException e) when (e.Message.Contains("No connection could be made because the target machine actively refused it"))
        {
            skipVersionCheck = true;
        }
        catch (Exception e)
        {
            Type t = e.GetType();
            dialog.ShowAsync($"An unexpected error occured while trying to check for updates: {e.Message}");
            serverEntry = null;
            skipVersionCheck = true;
        }
        
        LocalAppEntry? localEntry = appRepo.TryReadLocalAppDetails(appId);
        if(localEntry is null)
            skipVersionCheck = true;
        
        AppVersion latestVersion;
        
        if (!skipVersionCheck)
        {
            latestVersion = serverEntry.Versions.GetLatest(localEntry?.InstalledVersion.Tag ?? "")!;
           
            if (localEntry.InstalledVersion < latestVersion)
            {
                window.ShowUpdating();

                var scope = window.CreateUiScope();

                await installer.PatchApplicationAsync(appId, latestVersion, scope);

                scope.Report(1.0, "Updated");
            }
        }
        else
        {
            latestVersion = localEntry!.InstalledVersion;
            
            if (localEntry is null)
            {
                var result = await dialog.ShowAsync(new DialogRequest
                {
                    Title = "Nexus",
                    Message = "Application is not installed. Install it now?",
                    Kind = DialogKind.Question,
                    PrimaryButton = "Install",
                    SecondaryButton = "Cancel"
                });
            
                if (result.Result != DialogResultType.Yes)
                {
                    await main.InvokeAsync(window.Close);
                    return;
                }

                await main.InvokeAsync(window.ShowUpdating);

                var scope = await main.InvokeAsync(window.CreateUiScope);

                await installer.InstallFromArchiveAsync(appId, latestVersion, scope);

                scope.Report(1.0, "Installed");
            }
        }
        
        // let UI transition to "Starting"
        await main.InvokeAsync(window.ShowNoUpdates);
        
        try
        {
            appLauncher.LaunchApplication(appId, latestVersion.LaunchTarget, NEXUS_APPROVED_ARG);
        }
        catch (Exception e)
        {
            _ = dialog.ShowAsync($"Application could not be started: {e.GetType().Name}");
            logger.Error(e, "Application could not be started");
        }
    }
}