namespace DinkCompiler;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Hash-based incremental build cache stored in <project>/.dink-cache/.
// Each cached item has a .hash sidecar.  IsCurrent() returns true on a hit
// and atomically updates the stored hash on a miss, so the caller never has
// to call a separate "store hash" method.
class DinkCache
{
    readonly string _dir;

    public static DinkCache ForProject(string projectFolder)
        => new(Path.Combine(projectFolder, ".dink-cache"));

    DinkCache(string dir)
    {
        _dir = dir;
        Directory.CreateDirectory(dir);
    }

    // Returns true if the stored hash matches. On a miss, overwrites the
    // stored hash so the next call (with the same hash) will be a hit.
    public bool IsCurrent(string key, string hash)
    {
        var file = Path.Combine(_dir, key + ".hash");
        try
        {
            if (File.Exists(file) && File.ReadAllText(file) == hash)
                return true;
        }
        catch { }
        try { File.WriteAllText(file, hash); } catch { }
        return false;
    }

    public void Save(string key, string data)
    {
        try { File.WriteAllText(Path.Combine(_dir, key + ".dat"), data); } catch { }
    }

    public string? Load(string key)
    {
        var file = Path.Combine(_dir, key + ".dat");
        try { return File.Exists(file) ? File.ReadAllText(file) : null; }
        catch { return null; }
    }

    // SHA-256 of the concatenated byte contents of the given files (sorted
    // by path so order doesn't matter).
    public static string HashFiles(IEnumerable<string> paths)
    {
        using var sha = SHA256.Create();
        foreach (var path in paths.OrderBy(p => p, StringComparer.Ordinal))
        {
            try
            {
                var bytes = File.ReadAllBytes(path);
                sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
            }
            catch { }
        }
        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha.Hash!);
    }

    public static string HashString(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes);
    }

    // SHA-256 of the concatenated UTF-8 bytes of the given strings (in the
    // order supplied - sort before calling if order independence is needed).
    public static string HashStrings(IEnumerable<string> parts)
    {
        using var sha = SHA256.Create();
        foreach (var part in parts)
        {
            var bytes = Encoding.UTF8.GetBytes(part);
            sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }
        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha.Hash!);
    }
}
