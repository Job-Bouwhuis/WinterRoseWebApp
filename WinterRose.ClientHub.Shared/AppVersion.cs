namespace WinterRose.WebServer.Features.FileUploads.Models;

public enum VersionStringFormat
{
    FolderSafe,
    UrlSafe,
    HumanReadable
}

public class AppVersion : IComparable<AppVersion>
{
    public string VersionLabel { get; init; } = "";

    public ushort Major { get; init; }
    public ushort Minor { get; init; }
    public ushort Patch { get; init; }

    public string Tag { get; init; } = "";

    public DateTime UploadedAt { get; init; }

    public AppVersion() { } // Serialization
    public AppVersion(string versionLabel, DateTime uploadedAt) : this(versionLabel) => UploadedAt = uploadedAt;

    public AppVersion(string versionLabel)
    {
        VersionLabel = versionLabel;

        Parse(versionLabel, out ushort major, out ushort minor, out ushort patch, out string tag);

        Major = major;
        Minor = minor;
        Patch = patch;
        Tag = tag;
    }


    private static void Parse(
        string input,
        out ushort major,
        out ushort minor,
        out ushort patch,
        out string tag)
    {
        major = 0;
        minor = 0;
        patch = 0;

        if (string.IsNullOrWhiteSpace(input))
            throw new FormatException("Version string cannot be empty.");

        string[] mainAndTag = input.Split('-', 2, StringSplitOptions.None);

        string versionPart = mainAndTag[0];
        tag = mainAndTag.Length > 1 ? mainAndTag[1] : "";

        string[] versionParts = versionPart.Split('_');

        if (versionParts.Length != 3)
            throw new FormatException($"Invalid version format: {input}");

        if (!ushort.TryParse(versionParts[0], out major) ||
            !ushort.TryParse(versionParts[1], out minor) ||
            !ushort.TryParse(versionParts[2], out patch))
        {
            throw new FormatException($"Invalid version numbers: {input}");
        }

        if (!IsValidTag(tag))
            throw new FormatException($"Invalid tag format: {tag}");
    }

    private static bool IsValidTag(string tag)
    {
        if (string.IsNullOrEmpty(tag))
            return true;

        foreach (char c in tag)
        {
            bool isValid =
                (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9') ||
                c == '_' ||
                c == '-';

            if (!isValid)
                return false;
        }

        return true;
    }

    public string ToString(VersionStringFormat format)
    {
        string baseVersion = $"{Major}_{Minor}_{Patch}";

        string tagPart = string.IsNullOrEmpty(Tag)
            ? ""
            : $"-{Tag}";

        return format switch
        {
            VersionStringFormat.FolderSafe => $"{baseVersion}{tagPart}",
            VersionStringFormat.UrlSafe => $"{baseVersion}{tagPart}",
            VersionStringFormat.HumanReadable =>
                string.IsNullOrEmpty(Tag)
                    ? $"{Major}.{Minor}.{Patch}"
                    : $"{Major}.{Minor}.{Patch} ({Tag})",
            _ => $"{baseVersion}{tagPart}"
        };
    }

    public override string ToString()
        => ToString(VersionStringFormat.HumanReadable);

    public int CompareTo(AppVersion? other)
    {
        if (other is null)
            return 1;

        bool thisHasTag = !string.IsNullOrEmpty(Tag);
        bool otherHasTag = !string.IsNullOrEmpty(other.Tag);

        // 1. RELEASE vs TAGGED FIRST (release wins)
        if (!thisHasTag && otherHasTag)
            return 1;

        if (thisHasTag && !otherHasTag)
            return -1;

        // 2. BOTH HAVE TAGS -> compare tag branch
        if (thisHasTag && otherHasTag)
        {
            int tagCompare = string.Compare(Tag, other.Tag, StringComparison.OrdinalIgnoreCase);

            if (tagCompare != 0)
                return tagCompare;
        }

        // 3. SAME BRANCH (or both release) -> compare version numbers
        int majorCompare = Major.CompareTo(other.Major);
        if (majorCompare != 0)
            return majorCompare;

        int minorCompare = Minor.CompareTo(other.Minor);
        if (minorCompare != 0)
            return minorCompare;

        return Patch.CompareTo(other.Patch);
    }

    public static bool operator ==(AppVersion? a, AppVersion? b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a is null || b is null)
            return false;

        return a.CompareTo(b) == 0;
    }

    public static bool operator !=(AppVersion? a, AppVersion? b)
        => !(a == b);

    public static bool operator <(AppVersion a, AppVersion b)
        => a.CompareTo(b) < 0;

    public static bool operator >(AppVersion a, AppVersion b)
        => a.CompareTo(b) > 0;

    public static bool operator <=(AppVersion a, AppVersion b)
        => a.CompareTo(b) <= 0;

    public static bool operator >=(AppVersion a, AppVersion b)
        => a.CompareTo(b) >= 0;

    public override bool Equals(object? obj)
        => obj is AppVersion other && this == other;

    public override int GetHashCode()
        => HashCode.Combine(Major, Minor, Patch, Tag?.ToLowerInvariant());
}