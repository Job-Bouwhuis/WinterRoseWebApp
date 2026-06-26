using Microsoft.AspNetCore.Mvc;
using WinterRose.ClientHub.Feature.InformationRelay.Services;
using WinterRoseWebApp.Features.FileUploads.Models;

namespace WinterRose.ClientHub.Feature.InformationRelay.Controllers;

[ApiController]
public class RelayController : ControllerBase
{
    private readonly AppServerClient appServerClient;

    public RelayController(AppServerClient appServerClient)
    {
        this.appServerClient = appServerClient;
    }

    [HttpGet("apps/summaries")]
    public async Task<ActionResult<List<AppSummary>>> GetAppSummaries()
    {
        var result = await appServerClient.GetAppSummariesAsync();
        return Ok(result);
    }

    [HttpGet("apps")]
    public async Task<ActionResult<List<AppEntry>>> GetAppEntries()
    {
        var result = await appServerClient.GetAppEntriesAsync();
        return Ok(result);
    }
}
