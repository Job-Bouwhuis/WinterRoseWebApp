using System.Collections.Generic;
using System.Linq;
using WinterRose.Nexus.Shared;

namespace WinterRose.Nexus.Utils;

public static class VersionListExtensions
{
    extension(List<AppVersion> versions)
    {
        public AppVersion? GetLatest(string tagBranch)
        {
            return versions.Where(v => v.Tag == tagBranch)
                .OrderByDescending(v => v)
                .FirstOrDefault();
        }
    }
}