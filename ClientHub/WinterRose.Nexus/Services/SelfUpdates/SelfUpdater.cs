using System;
using System.IO;
using System.Threading.Tasks;
using WinterRose.CommandLine;
using WinterRose.DependancyInjection.Logging;
using WinterRose.Nexus.Interface;
using WinterRose.Nexus.Interface.Dialogs;
using WinterRose.Nexus.Interface.Windows;
using WinterRose.Nexus.Shared;
using WinterRose.Nexus.Utils;
using WinterRose.ProgressKeeping;
using WinterRose.WinterForgeSerializing;
using IServiceProvider = WinterRose.DependancyInjection.IServiceProvider;

namespace WinterRose.Nexus.Services.SelfUpdates;

public class SelfUpdater(
    AppServerClient server,
    ApplicationInstaller installer,
    MainThread main,
    IServiceProvider services,
    ILogger<SelfUpdater> logger,
    IModalDialog dialog)
{
    public async Task InstallUpdate()
    {
        string originalPath = ProgramArguments.Get<string>("original-path");
        string originalDirectory = Path.GetDirectoryName(originalPath);
        string[] forwarded = ProgramArguments.Get<string[]>("forward");

        var progressWindow = new InstallationProgressWindow(main, services, "Nexus");
        try
        {
            progressWindow.Show();
            progressWindow.ShowUpdating();

            var progress = progressWindow.CreateUiScope();

            logger.Info("Beginning Nexus self update...");

            AppEntry nexusEntry;
            try
            {
                nexusEntry = await server.GetAppEntryAsync(NexusClient.NexusAppId);
            }
            catch
            {
                await dialog.ShowAsync("The Nexus Registry is unavailable at the moment. Please wait until it is");
                logger.Error("Nexus registry unavailable.");
                // returning here will cause the original at this time non affected original Nexus client
                // to be started again and clean up this copy
                // Since it will also not be able to reach the Nexus Registry, it wont ask to update again
                return;
            }

            logger.Info("Retrieving current version...");

            object? versionres = null!;
            try
            {
                versionres =
                    WinterForge.DeserializeFromHumanReadableFile(Path.Combine(originalDirectory, "NexusVersion.wf"));
            }
            catch
            {
            }

            using (ApplicationInstallerContext.PushAppRoot(Path.GetDirectoryName(originalPath)))
            {
                string releaseBranch = "";
                if (versionres is LocalAppEntry currentVersion)
                {
                    releaseBranch = currentVersion.InstalledVersion.Tag;
                    var latest = nexusEntry.Versions.GetLatest(releaseBranch);
                    if (latest is null)
                    {
                        logger.Warning($"Release branch {releaseBranch} not found.");
                        var result = await dialog.ShowAsync(new DialogRequest()
                        {
                            AllowCancel = false,
                            Kind = DialogKind.Question,
                            Message =
                                $"The Nexus version {releaseBranch} could not be found. Do you wish to install the stable release?",
                            PrimaryButton = "yes",
                            SecondaryButton = "no",
                            Title = "Nexus installation",
                        });
                        if (result.Result is DialogResultType.Yes)
                            latest = nexusEntry.Versions.GetLatest("");
                        else
                        {
                            await progress.ReportAsync(1, "User cancelled the installation", ReportStatus.Info);
                            logger.Error("User did not want to install the stable release.");
                            await dialog.ShowAsync("Nexus could not be updated, and will now close.");
                            Environment.Exit(0);
                        }
                    }

                    try
                    {
                        await installer.ApplyDiffStreamSpecificVersionsAsync(
                            NexusClient.NexusAppId,
                            latest,
                            currentVersion.InstalledVersion,
                            originalDirectory,
                            progress);
                    }
                    catch (Exception ex)
                    {
                        await progress.ReportAsync(1, "Installation failed: " + ex.Message, ReportStatus.Error);
                    }

                    await progress.ReportAsync(1, "Done", ReportStatus.Success);

                    WriteNexusAppInfo(currentVersion, latest);
                }
                else
                {
                    logger.Error(
                        "The installation config of Nexus is wrong. Attempting to reinstall the Nexus client on the latest release branch");
                    await progress.ReportAsync(0,
                        "The installation config of Nexus is wrong. Attempting to reinstall the Nexus client on the latest release branch",
                        ReportStatus.Error);

                    NexusClient.SafeDeleteDirectory(new DirectoryInfo(originalDirectory));

                    AppVersion latest = nexusEntry.Versions.GetLatest("")!;

                    await installer.InstallFromArchiveAsync(NexusClient.NexusAppId, latest, progress.CreateChild(0.98));

                    WriteNexusAppInfo(null, latest);

                    logger.Info("Nexus re-installation complete.");
                    await progress.ReportAsync(1, "Done", ReportStatus.Success);
                }
            }
        }
        finally
        {
            await Task.Delay(500);
            progressWindow.Close();
        }
    }

    private void WriteNexusAppInfo(LocalAppEntry? localEntry, AppVersion latest)
    {
        string originalPath = ProgramArguments.Get<string>("original-path")!;
        string installPath = Path.GetDirectoryName(originalPath)!;

        if (localEntry is null)
        {
            AppEntry entry = server.GetAppEntryAsync(NexusClient.NexusAppId).GetAwaiter().GetResult();
            localEntry = new LocalAppEntry(
                entry.AppId,
                entry.DisplayName,
                latest,
                installPath,
                entry.Publisher,
                entry.Tags.ToArray(),
                entry.LongDescription,
                entry.ShortDescription
            );
        }
        else
        {
            localEntry.InstalledVersion = latest;
        }

        WinterForge.SerializeToFile(localEntry, Path.Combine(installPath, "NexusVersion.wf"),
            TargetFormat.FormattedHumanReadable);
    }
}