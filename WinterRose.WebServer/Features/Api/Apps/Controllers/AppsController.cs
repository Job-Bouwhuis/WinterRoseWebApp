using Microsoft.AspNetCore.Mvc;
using WinterRose.Web.Utils;
using WinterRose.Web.Validation;
using WinterRose.WebServer.Features.Api.Apps.Services;
using WinterRose.WebServer.Features.FileUploads.Models;
using WinterRose.WebServer.Features.FileUploads.Services;

namespace WinterRose.WebServer.Features.Api.Apps.Controllers;

[ApiController]
[Route("apps")]
[UseWinterForge]
public class AppsController : ControllerBase
{
    private readonly AppRepository repo;
    private readonly AppDiffService diffService;

    public AppsController(AppRepository repo, AppDiffService diffService)
    {
        this.repo = repo;
        this.diffService = diffService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAppsAsync()
    {
        var res = await repo.GetAppEntries();
        return Ok(res);
    }

    [HttpGet("{appName}/versions")]
    public IActionResult GetVersions(
        string appName,
        [FromQuery] GetVersionsRequest request)
    {
        var versions = repo.GetVersions(appName, request.FromVersion, request.limit);
        return Ok(new
        {
            app = appName,
            versions
        });
    }
    
    [HttpGet("{appName}/versions/{version}/file")]
    public IActionResult GetVersionFile(
        string appName,
        string version,
        [FromQuery] string path)
    {
        var versionEntry = new AppVersion(version);

        var stream = repo.OpenVersionFile(
            appName,
            versionEntry,
            path);

        return File(stream, "application/octet-stream", enableRangeProcessing: true);
    }
    
    [HttpGet("{appName}/versions/{version}/archive")]
    public IActionResult GetVersionArchive(
        string appName,
        string version)
    {
        var versionEntry = new AppVersion(version);

        var stream = repo.OpenVersionArchive(appName, versionEntry);

        return File(stream, "application/zip", $"{appName}_{version}.zip", enableRangeProcessing: true);
    }
    
    [HttpGet("{appName}/diff")]
    public async Task<IActionResult> GetDiff(
        string appName,
        [FromQuery] string from,
        [FromQuery] string to)
    {
        var fromVersion = new AppVersion(from);
        var toVersion = new AppVersion(to);

        var stream = await diffService.OpenDiffStreamAsync(appName, fromVersion, toVersion);

        return File(stream, "application/octet-stream", enableRangeProcessing: true);
    }

    public class GetVersionsRequest : IValidationDefinition<GetVersionsRequest>
    {
        public string? FromVersion { get; set; }
        public int? limit { get; set; } = 50;

        public void Define(IValidation<GetVersionsRequest> validator)
        {
            validator.RuleFor(x => x.limit).GreaterThan(0);
        }
    }
}
