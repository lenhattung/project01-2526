namespace StudentForm;

internal sealed class DistributedFileReceiver
{
    private readonly string _rootPath;
    private readonly Dictionary<string, FileStream> _openFiles = new();

    public DistributedFileReceiver(string rootPath)
    {
        _rootPath = rootPath;
        Directory.CreateDirectory(_rootPath);
    }

    public string Start(string fileName)
    {
        string safeFile = MakeSafeFileName(fileName);
        string path = Path.Combine(_rootPath, safeFile);
        _openFiles[fileName] = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        return path;
    }

    public Task AppendAsync(string fileName, byte[] chunk, CancellationToken cancellationToken)
    {
        if (!_openFiles.TryGetValue(fileName, out FileStream? stream))
        {
            throw new InvalidOperationException("Distributed file was not started.");
        }

        return stream.WriteAsync(chunk, cancellationToken).AsTask();
    }

    public async Task<string> CompleteAsync(string fileName)
    {
        if (!_openFiles.Remove(fileName, out FileStream? stream))
        {
            throw new InvalidOperationException("Distributed file was not started.");
        }

        string path = stream.Name;
        await stream.DisposeAsync();
        return TryExtractZip(path);
    }

    private string TryExtractZip(string path)
    {
        if (!string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        try
        {
            string extractFolder = Path.Combine(
                Path.GetDirectoryName(path) ?? _rootPath,
                Path.GetFileNameWithoutExtension(path));

            if (Directory.Exists(extractFolder))
            {
                Directory.Delete(extractFolder, recursive: true);
            }

            System.IO.Compression.ZipFile.ExtractToDirectory(path, extractFolder);
            return extractFolder;
        }
        catch
        {
            return path;
        }
    }

    private static string MakeSafeFileName(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(c, '_');
        }

        return string.IsNullOrWhiteSpace(value) ? "teacher-file.bin" : value.Trim();
    }
}
