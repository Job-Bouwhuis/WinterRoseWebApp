using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Eto.Drawing;
using WinterRose.Applications;
using WinterRose.Applications.ApplicationInstanceLocks;
using WinterRose.CommandLine;
using WinterRose.Nexus;
using WinterRose.Configuration;
using WinterRose.DependancyInjection;
using WinterRose.DependancyInjection.Logging;
using WinterRose.Nexus.Interface;
using WinterRose.Nexus.Interface.Dialogs;
using WinterRose.Nexus.Interface.Preferences;
using WinterRose.Nexus.Interface.Windows;
using WinterRose.Nexus.Preferences;
using WinterRose.Nexus.Services;
using WinterRose.Nexus.Services.SelfUpdates;
using WinterRose.Nexus.Shared;
using WinterRose.Nexus.Uri;
using WinterRose.Recordium;
using WinterRose.Uris;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;

public class Program
{
    private static void Main(string[] args)
    {
        //Console.WriteLine("Args: " + string.Join(" ", args));
        //Console.WriteLine("Version 3");
        
        PrepareArguments(args);

        AwaitProcessFromArgument();

        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        // a workaround to force this type to be loaded into the AppDomain
        _ = typeof(ByteArrayValueProvider);

        ServiceBuilder lockServicesBuilder = new ServiceBuilder();
        lockServicesBuilder.AddApplicationMutex("winterrose.hub");
        lockServicesBuilder.AddUriForwardersOnly("winterrose.hub");
        var lockServices = lockServicesBuilder.Build();

        var mutex = lockServices.Resolve<IApplicationMutex>();

        if (!mutex.IsFirstInstance)
        {
            if (args.Length == 0)
                // if no args are provided, we will instruct the first instance to show the window.
                args = ["winterrose://show-window"];

            // we get the forwarder that was made for the OS we run on
            var forwarder = lockServices.Resolve<IUriForwarder>();
            forwarder.ForwardAsync(args[0]).GetAwaiter().GetResult();
            lockServices.Dispose();
            return;
        }

        Config config = new("config/hub.txt");

        ApplicationBuilder builder = App.CreateBuilder();

        ConfigureServices(builder, config);

        // Building the app adds loggers automatically
        using var app = builder.Build<App>();

        BuildUserPreferences(app.Services.Resolve<UserPreferences>());
        var newUpdate = CheckSelfUpdateState(args, app, lockServices);

        if (!newUpdate)
        {
            if (ProgramArguments.Exists("clean-copy"))
                app.Services.Resolve<SelfUpdateCleanup>().Clean();
            
            if (!ProgramArguments.Exists("self-update"))
            {
                app.BeginUriListener();

                if (ProgramArguments.TryGet<string>("uri", out var uri))
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(2000);
                        await app.Services.Resolve<IUriForwarder>().ForwardAsync(uri);
                    });
                }

                app.Services.Resolve<AppTray>().Initialize(new Bitmap("Icons\\NexusLogo.bmp"));
            }

            app.Run();
            lockServices.Dispose();
        }
    }

    private static bool CheckSelfUpdateState(string[] args, App app, IServiceProvider lockServices)
    {
        bool newUpdate = false;
        if (ProgramArguments.Exists("self-update"))
        {
            var selfUpdater = app.Services.Resolve<SelfUpdater>();
            selfUpdater.InstallUpdate().ContinueWith(async t =>
            {
                if (t.IsFaulted)
                {
                    IEnumerable<string> messages = t.Exception.InnerExceptions.Select(e => e.Message);
                    await app.Services.Resolve<IModalDialog>().ShowAsync($"Exceptions: " + string.Join(Environment.NewLine, messages));
                    app.Stop();
                    return;
                }
                
                app.Services.Resolve<SelfStarter>().Start();
                app.Stop();
            });
        }
        else if (newUpdate = CheckNexusUpdates(ref args, app.Services))
        {
            SelfUpdateStarter selfUpdateStarter = app.Services.Resolve<SelfUpdateStarter>();
            lockServices.Dispose();
            selfUpdateStarter.StartSelfUpdate(args);
            app.Stop();
        }

        return newUpdate;
    }

    private static void ConfigureServices(ApplicationBuilder builder, Config config)
    {
        // The Http client to talk to the Nexus Registry
        builder.Services.AddHttpClient()
            .Configure<HttpClient>(client =>
            {
                client.BaseAddress = new Uri(config.Get<string>("ServerUri"));
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/winterforge")
                );
            });

        builder.Services.AddSingleton<WindowBase, ApplicationStoreWindow>();
        builder.Services.AddSingleton<WindowBase, TestWindow>();
        builder.Services.AddSingleton<WindowBase, LibraryWindow>();
        builder.Services.AddSingleton<WindowBase, UserPreferencesWindow>();

        builder.Services.AddSingleton<UserPreferences>();

        builder.Services.AddSingleton<IApplicationLaunchManager, ApplicationLaunchManager>();
        // DI resolves only the one for the OS the app runs on
        builder.Services.AddSingleton<IApplicationLauncher, WindowsApplicationStarter>();
        builder.Services.AddSingleton<IApplicationLauncher, LinuxApplicationStarter>();
        builder.Services.AddSingleton<ApplicationStarter>();

        builder.Services.AddSingleton<IModalDialog, EtoModalDialog>();

        builder.Services.AddSingleton<AppServerClient>();
        builder.Services.AddSingleton<EtoShell>();
        builder.Services.AddSingleton<UiManager>();
        builder.Services.AddApplicationMutex("winterrose.hub");

        // consuming services request a IUriHandler[] and get all we register here
        builder.Services.AddSingleton<IUriHandler, URiLoggerHandler>();
        builder.Services.AddSingleton<IUriHandler, ShowWindowUriHandler>();
        builder.Services.AddSingleton<IUriHandler, ShutdownUriHandler>();
        builder.Services.AddSingleton<IUriHandler, ApplicationUpdateUriHandler>();

        builder.Services.AddSingleton<MainThread>();

        builder.Services.AddSingleton<ApplicationInstaller>();
        builder.Services.AddSingleton<ClientAppRepository>();

        builder.Services.AddSingleton(typeof(IAsyncEventQueue<>), typeof(AsyncEventQueue<>));

        builder.Services.AddSingleton<AppTray>();

        // adds linux and windows variants of the Uri services which the DI container resolves for whatever OS the
        // app runs on
        builder.Services.AddUriListener("winterrose.hub", "winterrose", "WinterRose Hub");

        builder.Services.AddSingleton<SelfUpdateStarter>();
        builder.Services.AddSingleton<SelfUpdateChecker>();
        builder.Services.AddSingleton<SelfUpdater>();
    }

    private static void AwaitProcessFromArgument()
    {
        if (ProgramArguments.TryGet("processId", out int processId))
        {
            Log l = new("Nexus");
            l.Info($"Arguments indicate a need to wait for a process with id {processId} to exit before continuing.");
            try
            {
                System.Diagnostics.Process.GetProcessById(processId).WaitForExit();
            }
            catch (ArgumentException e) when (e.Message.Contains("not running"))
            {
            }
            
            l.Info("Success, the process is no longer running!");
        }
    }

    private static void PrepareArguments(string[] args)
    {
        var argBuilder = ProgramArguments.CreateBuilder();

        var uriRuleBuilder = argBuilder.Value<string>("uri", position: 0, optional: true);
        uriRuleBuilder.Bound<string>(
            predicate: val =>
                val.StartsWith("winterrose://"), // TODO: make sure the Nexus app uses "wrnexus://" instead
            messageFactory: s => "First argument 'uri' must start with 'winterrose://'");

        argBuilder.Forward("forward");

        argBuilder.Flag("self-update");
        argBuilder.Value<string>("original-path");
        argBuilder.Value<int>("processId");
        argBuilder.Flag("clean-copy");
        argBuilder.Build();

        ProgramArguments.Parse(args);
    }

    static bool CheckNexusUpdates(ref string[] args, IServiceProvider services)
    {
        var logger = services.Resolve<ILogger<NexusClient>>();
        logger.Info("Checking Nexus updates...");

        if (services.Resolve<SelfUpdateChecker>().Check())
        {
            logger.Info("Nexus has an update!");
            return true;
        }

        logger.Info("Nexus has no updates.");
        return false;
    }

    static void BuildUserPreferences(UserPreferences prefs)
    {
        prefs.Register(new PreferenceOption<bool>(
            "Close Nexus right after starting an app",
            "Closes the Nexus client immediately after launching an application",
            "Behavior",
            false));

        // past here its demo options, they have a high chance to not actually going to be used in the eventual app

        prefs.Register(new PreferenceOption<bool>(
            "Start apps in background mode",
            "If enabled, launched apps will be started without bringing their window to focus",
            "Behavior",
            true));


        prefs.Register(new PreferenceOption<string>(
            "Accent color name",
            "Name of the current accent theme used across Nexus UI",
            "Appearance",
            "Purple"));

        prefs.Register(new PreferenceOption<int>(
            "UI density scale",
            "Controls spacing and compactness of UI elements",
            "Appearance",
            10,
            false,
            ControlHint.Slider,
            minValue: 0,
            maxValue: 20));

        prefs.Register(new PreferenceOption<bool>(
            "Enable animated transitions",
            "Smooth animations between UI states such as tab switching and dialogs",
            "Appearance",
            true));


        prefs.Register(new PreferenceOption<int>(
            "Max background download threads",
            "Limits how many concurrent downloads Nexus can perform",
            "Performance",
            4,
            false,
            ControlHint.NumericUpDown,
            minValue: 1,
            maxValue: 16));

        prefs.Register(new PreferenceOption<bool>(
            "Enable aggressive caching",
            "Caches application metadata more aggressively to reduce network calls",
            "Performance",
            true));


        prefs.Register(new PreferenceOption<bool>(
            "Enable notifications",
            "Master toggle for all Nexus notifications",
            "Notifications",
            true));

        prefs.Register(new PreferenceOption<int>(
            "Notification display time (seconds)",
            "How long notifications stay visible",
            "Notifications",
            5,
            false,
            ControlHint.Slider,
            minValue: 1,
            maxValue: 20));

        // =========================================================
        // DEBUG
        // =========================================================

        prefs.Register(new PreferenceOption<bool>(
            "Enable debug overlay",
            "Shows internal diagnostics overlay for developers",
            "Debug",
            false));

        prefs.Register(new PreferenceOption<string>(
            "Log verbosity level",
            "Controls how much internal logging is produced",
            "Debug",
            "Info"));

        // =========================================================
        // ENUM DEMO (normal enum)
        // =========================================================

        prefs.Register(new PreferenceOption<AppLaunchMode>(
            "Default app launch mode",
            "Determines how applications are started by default",
            "Behavior",
            AppLaunchMode.Normal));

        // =========================================================
        // FLAGS ENUM DEMO (multi-select UI)
        // =========================================================

        prefs.Register(new PreferenceOption<LaunchFlags>(
            "Launch flags",
            "Advanced launch behavior modifiers",
            "Behavior",
            LaunchFlags.None));

        // =========================================================
        // OS-SPECIFIC OPTION (Linux only demo)
        // =========================================================

        prefs.Register(new PreferenceOption<bool>(
            "Use system window decorations (Linux)",
            "Lets the OS draw window borders instead of Nexus styling",
            "Appearance",
            true,
            allowedOs: new[] { "linux" }));
    }
}

public enum AppLaunchMode
{
    Normal,
    Admin,
    Isolated
}

[Flags]
public enum LaunchFlags
{
    None = 0,
    DisableGpuAcceleration = 1,
    ForceSingleInstance = 2,
    EnableLogging = 4,
    SandboxMode = 8
}