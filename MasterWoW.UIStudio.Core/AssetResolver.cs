using System.Collections.Concurrent;

namespace MasterWoW.UIStudio.Core;

public sealed class AssetResolver
{
    private static readonly string[] NativeExtensions = { ".blp", ".tga" };
    private static readonly string[] ConvenienceExtensions = { ".png", ".jpg", ".jpeg", ".dds" };
    private readonly Dictionary<string, InterfaceFileRecord> _byWowPath;
    public AssetResolver(IEnumerable<InterfaceFileRecord> files) => _byWowPath = files.Where(x => x.Kind is InterfaceFileKind.Blp or InterfaceFileKind.Tga or InterfaceFileKind.Png or InterfaceFileKind.Jpeg or InterfaceFileKind.Dds).GroupBy(x => Normalize(x.WowPath), StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

    public ResolvedAsset Resolve(string requestedPath, bool includeDesignerFormats = true)
    {
        var normalized = Normalize(requestedPath); var attempts = new List<string>();
        foreach (var candidate in Candidates(normalized, includeDesignerFormats)) { attempts.Add(candidate); if (_byWowPath.TryGetValue(candidate, out var found)) return new(requestedPath, normalized, found, attempts); }
        return new(requestedPath, normalized, null, attempts);
    }
    private static IEnumerable<string> Candidates(string path, bool convenience)
    {
        if (Path.HasExtension(path)) { yield return path; yield break; }
        foreach (var extension in NativeExtensions) yield return path + extension;
        if (convenience) foreach (var extension in ConvenienceExtensions) yield return path + extension;
    }
    public static string Normalize(string path)
    {
        var value = path.Trim().Replace('/', '\\'); while (value.Contains("\\\\", StringComparison.Ordinal)) value = value.Replace("\\\\", "\\", StringComparison.Ordinal);
        value = value.TrimStart('\\'); if (!value.StartsWith("Interface\\", StringComparison.OrdinalIgnoreCase) && !value.StartsWith("Fonts\\", StringComparison.OrdinalIgnoreCase)) value = "Interface\\" + value;
        return value;
    }
    public static string? ResolveFromInterfaceFolder(string interfaceRoot,string requestedPath)
    {
        var normalized=Normalize(requestedPath);var relative=normalized.StartsWith("Interface\\",StringComparison.OrdinalIgnoreCase)?normalized[10..]:normalized;var basePath=normalized.StartsWith("Fonts\\",StringComparison.OrdinalIgnoreCase)?Path.Combine(Directory.GetParent(interfaceRoot)?.FullName??interfaceRoot,relative):Path.Combine(interfaceRoot,relative);
        foreach(var candidate in Candidates(basePath,true))if(File.Exists(candidate))return Path.GetFullPath(candidate);return null;
    }
}

public sealed class TextureCache
{
    private sealed record Entry(DecodedTexture Texture, long Cost, DateTimeOffset LastAccess);
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    public long MaximumBytes { get; set; } = 256L * 1024 * 1024;
    public async Task<DecodedTexture> GetOrDecodeAsync(string path, Func<string, CancellationToken, Task<DecodedTexture>> decoder, CancellationToken cancellationToken = default)
    {
        var info = new FileInfo(path); var key = $"{path}|{info.Length}|{info.LastWriteTimeUtc.Ticks}";
        if (_entries.TryGetValue(key, out var cached)) { _entries[key] = cached with { LastAccess = DateTimeOffset.UtcNow }; return cached.Texture; }
        var texture = await decoder(path, cancellationToken).ConfigureAwait(false); _entries[key] = new(texture, texture.Rgba32.LongLength, DateTimeOffset.UtcNow); Trim(); return texture;
    }
    public void Clear() => _entries.Clear();
    private void Trim() { var total = _entries.Values.Sum(x => x.Cost); foreach (var item in _entries.OrderBy(x => x.Value.LastAccess)) { if (total <= MaximumBytes) break; if (_entries.TryRemove(item.Key, out var removed)) total -= removed.Cost; } }
}
