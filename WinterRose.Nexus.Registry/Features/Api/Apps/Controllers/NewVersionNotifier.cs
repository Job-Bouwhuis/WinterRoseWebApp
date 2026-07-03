using System.Buffers;
using Microsoft.AspNetCore.Mvc;
using WinterRose.Nexus.Registry.Features.FileUploads.Models;
using WinterRose.Nexus.Registry.Features.FileUploads.Pages;
using WinterRose.Nexus.Registry.Features.FileUploads.Services;
using WinterRose.Nexus.Shared;
using WinterRose.WinterForgeSerializing;

namespace WinterRose.Nexus.Registry.Features.Api.Apps.Controllers;

[ApiController]
[Route("versions/event")]
public sealed class NewVersionNotifier(IAsyncEventQueue<UploadCompletedEvent> eventQueue, ILogger<NewVersionNotifier> logger) : ControllerBase
{
    [HttpGet("{appId}/{tag}")]
    public async Task Get(string appId, string tag, CancellationToken ct)
    {
        Response.Headers.ContentType = "text/event-stream";

        logger.LogInformation("A request for new version notifications received for app {AppId}", appId);

        await eventQueue.SubscribeAsync(async (ev, token) =>
        {
            if (ev.Name != appId)
                return;

            if (tag != "*" && ev.AppVersion.Tag != tag)
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
        }, ct);
    }
}