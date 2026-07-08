using System.Security.Cryptography;

namespace TeacherForm;

internal sealed class SubmissionReceiver
{
    private readonly string _rootPath;
    private readonly Dictionary<string, FileStream> _openFiles = new();

    public SubmissionReceiver(string rootPath)
    {
        _rootPath = rootPath;
        Directory.CreateDirectory(_rootPath);
    }

    public string Start(string sessionId, string studentId, string fileName)
    {
        string safeFile = MakeSafePathPart(fileName);
        string path = Path.Combine(_rootPath, safeFile);
        _openFiles[Key(sessionId, studentId, fileName)] = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        return path;
    }

    public async Task AppendAsync(string sessionId, string studentId, string fileName, byte[] chunk, CancellationToken cancellationToken)
    {
        string key = Key(sessionId, studentId, fileName);
        if (!_openFiles.TryGetValue(key, out FileStream? stream))
        {
            throw new InvalidOperationException("Submission was not started.");
        }

        await stream.WriteAsync(chunk, cancellationToken);
    }

    public async Task<string> CompleteAsync(string sessionId, string studentId, string fileName, string expectedHash)
    {
        string key = Key(sessionId, studentId, fileName);
        if (!_openFiles.Remove(key, out FileStream? stream))
        {
            throw new InvalidOperationException("Submission was not started.");
        }

        string path = stream.Name;
        await stream.DisposeAsync();

        string actualHash = await ComputeSha256Async(path);
        if (!string.IsNullOrWhiteSpace(expectedHash) &&
            !actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Submission hash mismatch. Expected {expectedHash}, got {actualHash}.");
        }

        return path;
    }

    public void AbortStudent(string sessionId, string studentId)
    {
        string prefix = $"{sessionId}|{studentId}|";
        foreach (string key in _openFiles.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToList())
        {
            if (_openFiles.Remove(key, out FileStream? stream))
            {
                stream.Dispose();
            }
        }
    }

    private static async Task<string> ComputeSha256Async(string path)
    {
        await using FileStream stream = File.OpenRead(path);
        byte[] hash = await SHA256.HashDataAsync(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Key(string sessionId, string studentId, string fileName)
    {
        return $"{sessionId}|{studentId}|{fileName}";
    }

    private static string MakeSafePathPart(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(c, '_');
        }

        return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
    }
}
