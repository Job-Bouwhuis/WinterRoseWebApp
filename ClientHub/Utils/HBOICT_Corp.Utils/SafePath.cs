using System.Text;

namespace WinterRose.Web.Utils;

/// <summary>
/// Provides utility methods for safely combining and sanitizing file paths.
/// </summary>
public static class SafePath
{
    /// <summary>
    /// Combines the provided path segments into a single path, ensuring that invalid characters are replaced with underscores and that the resulting path is well-formed.
    /// </summary>
    /// <param name="parts"></param>
    /// <returns></returns>
    public static string Combine(params ReadOnlySpan<string> parts)
    {
        if (parts.Length == 0)
            return string.Empty;

        var invalidChars = Path.GetInvalidFileNameChars();
        var separator = Path.DirectorySeparatorChar;

        var builder = new StringBuilder();

        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];

            if (string.IsNullOrWhiteSpace(part))
                continue;

            bool hasDriveRoot = IsWindowsDriveRoot(part, out string? driveRoot, out string remainder);

            if (hasDriveRoot)
            {
                if (builder.Length > 0)
                    builder.Append(separator);

                if (builder.Length == 0)
                {
                    builder.Append(driveRoot);
                }
                else
                {
                    builder.Append(separator);
                    builder.Append(driveRoot);
                }

                part = remainder;
            }

            var segments = part.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

            if (!hasDriveRoot)
                builder.Append(separator);

            for (int j = 0; j < segments.Length; j++)
            {
                var segment = Sanitize(segments[j], invalidChars);

                builder.Append(segment);

                if (builder.Length > 0 && j != segments.Length - 1)
                    builder.Append(separator);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Checks if the input string starts with a Windows drive root (e.g., "C:\") and separates it from the remainder of the path.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="driveRoot"></param>
    /// <param name="remainder"></param>
    /// <returns></returns>
    private static bool IsWindowsDriveRoot(string input, out string? driveRoot, out string remainder)
    {
        driveRoot = null;
        remainder = input;

        if (input.Length >= 3 &&
            char.IsLetter(input[0]) &&
            input[1] == ':' &&
            (input[2] == '\\' || input[2] == '/'))
        {
            driveRoot = input[..3];
            remainder = input[3..];
            return true;
        }

        return false;
    }

    private static string Sanitize(string input, char[] invalidChars)
    {
        var builder = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            if (Array.IndexOf(invalidChars, c) >= 0)
            {
                builder.Append('_');
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
