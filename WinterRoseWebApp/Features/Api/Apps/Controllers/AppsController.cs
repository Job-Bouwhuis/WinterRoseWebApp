using Microsoft.AspNetCore.Mvc;
using WinterRose.Web.Validation;
using WinterRoseWebApp.Features.Api.Apps.Services;
using WinterRoseWebApp.Features.FileUploads.Models;

namespace WinterRoseWebApp.Features.Api.Apps.Controllers;

[ApiController]
[Route("apps")]
public class AppsController : ControllerBase
{
    private readonly AppRepository repo;

    public AppsController(AppRepository repo)
    {
        this.repo = repo;
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
        var versions = GetVersionsAsync(appName, request.FromVersion, request.limit);
        return Ok(new
        {
            app = appName,
            versions
        });
    }

    public List<string> GetVersionsAsync(string appName, string? from, int? limit)
    {
        var allVersions = repo.GetAppEntry(appName).Versions.Select(v => v.Version);

        if (from != null)
        {
            VersionEntry vers = new() { VersionLabel = from };

            allVersions = allVersions.Where(v => v > vers.Version).ToList();
        }

        if(limit is null)
            return allVersions.Select(v => v.ToString()).ToList();

        return allVersions
            .Take(limit.Value)
            .Select(v => v.ToString())
            .ToList();
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
