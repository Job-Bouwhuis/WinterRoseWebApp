namespace WinterRoseWebApp.Features.FileUploads.Endpoints;

public static class FileDownloads
{
    extension(WebApplication app)
    {
        public void AddDiffFileDownloads()
        {
            app.MapGet("/api/uploads/diff/download", (string file) =>
            {
                // Basic path traversal guard: resolve and confirm it sits under Uploads
                var uploadsRoot = Path.GetFullPath("Uploads");          // adjust to match your root
                var resolved = Path.GetFullPath(file);

                if (!resolved.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
                    return Results.Forbid();

                if (!File.Exists(resolved))
                    return Results.NotFound();

                var fileName = Path.GetFileName(resolved);
                var stream = File.OpenRead(resolved);

                return Results.File(stream, "application/octet-stream", fileName);
            });

        }
    }
}
