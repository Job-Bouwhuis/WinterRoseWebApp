using System.Buffers;
using Microsoft.AspNetCore.Mvc;
using WinterRose.Nexus.Registry.Features.FileUploads.Pages;
using WinterRose.Nexus.Registry.Features.FileUploads.Services;
using WinterRose.WinterForgeSerializing;

namespace WinterRose.Nexus.Registry.Features.Api.Apps.Controllers;

[ApiController]
[Route("versions/event")]
public sealed class NewVersionNotifier(IAsyncQueue<UploadCompletedEvent> queue, ILogger<NewVersionNotifier> logger) : ControllerBase
{
    [HttpGet("{appId}")]
    public async Task Get(string appId)
    {
        Response.Headers.ContentType = "text/event-stream";

        logger.LogInformation("A request for new version notifications received for app {AppId}", appId);

        await queue.SubscribeAsync(async (ev, token) =>
        {
            logger.LogInformation("A new version notification received for app {AppId} version {Version}", ev.BasePath, ev.AppVersion.ToString());
            if (ev.Name != appId)
                return;

            try
            {
                using var mem = new MemoryStream();
                WinterForge.SerializeToStream(ev.AppVersion, mem, TargetFormat.FormattedHumanReadable);
                mem.Position = 0;
                await mem.CopyToAsync(Response.Body, token);
                Response.BodyWriter.Write<byte>([255]);
                await Response.Body.FlushAsync(token);
            }
            catch (Exception e)
            {
                logger.LogError(e, "failed to serialize event");
            }
        }, CancellationToken.None);
    }
}