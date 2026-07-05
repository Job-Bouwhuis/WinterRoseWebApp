namespace WinterRose.FileManagement;

public static class ParallelDirectoryTransfer
{
    extension(Directory)
    {
        public static void MoveCrossDrive(string sourcePath, string targetPath)
            => MoveCrossDriveInternal(sourcePath, targetPath);
    }

    private static void MoveCrossDriveInternal(string sourcePath, string targetPath)
    {
        if (!Directory.Exists(sourcePath))
            throw new DirectoryNotFoundException(sourcePath);

        Directory.CreateDirectory(targetPath);

        List<string> files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories).ToList();

        SemaphoreSlim semaphore = new SemaphoreSlim(Environment.ProcessorCount);

        List<Task> tasks = new List<Task>(files.Count);

        foreach (string file in files)
        {
            string relativePath = Path.GetRelativePath(sourcePath, file);
            string destinationFile = Path.Combine(targetPath, relativePath);

            string? directory = Path.GetDirectoryName(destinationFile);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using FileStream sourceStream =
                File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            using FileStream destinationStream = File.Create(destinationFile);

            sourceStream.CopyTo(destinationStream);
        }

        DeleteDirectoryRecursive(sourcePath);
    }

    private static void DeleteDirectoryRecursive(string path)
    {
        foreach (string directory in Directory.GetDirectories(path))
        {
            DeleteDirectoryRecursive(directory);
        }

        foreach (string file in Directory.GetFiles(path))
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        Directory.Delete(path, false);
    }
}