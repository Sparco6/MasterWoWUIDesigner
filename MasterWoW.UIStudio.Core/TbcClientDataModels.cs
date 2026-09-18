namespace MasterWoW.UIStudio.Core;

public enum ClientEnvironment { InGame, Glue, Shared }
public enum InterfaceFileKind { Lua, Xml, Blp, Tga, Font, Png, Jpeg, Dds, Other }
public enum ScanMode { Incremental, FullRebuild }

public sealed record TbcClientProfile(string Name, string TargetVersion, string InterfaceRoot, string CacheRoot, string? AssetRoot = null, string? CompatibilityNotes = null);
public sealed record InterfaceFileRecord(string DiskPath, string RelativePath, string WowPath, InterfaceFileKind Kind, ClientEnvironment Environment, long Size, DateTimeOffset LastWriteUtc, string Extension);
public sealed record TextureMetadata(string Format, int Width, int Height, string Compression, int AlphaDepth, int MipCount, bool HasAlpha, string Orientation);
public sealed record TextureAsset(InterfaceFileRecord File, TextureMetadata? Metadata, IReadOnlyList<string> UsedBy);
public sealed record ScanDiagnostic(string Severity, string Category, string Message, string? File = null, int? Line = null);
public sealed record ApiUsageRecord(string Name, int UsageCount, IReadOnlyList<string> SourceFiles, IReadOnlyList<SourceOccurrence> Occurrences);
public sealed record SourceOccurrence(string File, int Line, string Context);
public sealed record EventUsageRecord(string Name, int UsageCount, IReadOnlyList<string> SourceFiles);
public sealed record GlobalRecord(string Name, string Kind, ClientEnvironment Environment, IReadOnlyList<string> SourceFiles);
public sealed record XmlAnchorDefinition(string Point, string? RelativeTo, string? RelativePoint, double X, double Y);
public sealed record XmlRegionDefinition(string Name, string Type, string? Layer, string? File, string? Text, double? Width = null, double? Height = null, IReadOnlyList<XmlAnchorDefinition>? Anchors = null, bool Hidden = false, bool SetAllPoints = false, string? Inherits = null, IReadOnlyList<double>? TexCoords = null);
public sealed record XmlTemplateDefinition(string Name, string Type, bool Virtual, IReadOnlyList<string> Inherits, ClientEnvironment Environment, string DefinedIn, double? Width, double? Height, IReadOnlyList<XmlAnchorDefinition> Anchors, IReadOnlyList<XmlRegionDefinition> Regions, IReadOnlyDictionary<string,string> Scripts, IReadOnlyList<string> Children, IReadOnlyList<string> ResolvedInheritance, string? ParentName = null, string? BackdropBackground = null, string? BackdropEdge = null, string? NormalTexture = null, bool? Hidden = null, bool SetAllPoints = false, string? Text = null, string? DeclaredParent = null);
public sealed record InterfaceDependency(string SourceFile, string TargetFile, string Kind, int Order);
public sealed record ScanMetadata(string ProfileName, string TargetVersion, string InterfaceRoot, DateTimeOffset StartedAt, DateTimeOffset CompletedAt, ScanMode Mode, int LuaFiles, int XmlFiles, int BlpFiles, int TgaFiles, int FontFiles, int Templates, int Frames, int ObservedApis, int Events, int MissingAssets, int Warnings, int Errors);

public sealed class TbcClientIndex
{
    public TbcClientProfile Profile { get; init; } = null!;
    public List<InterfaceFileRecord> Files { get; init; } = new();
    public List<TextureAsset> Textures { get; init; } = new();
    public List<XmlTemplateDefinition> Templates { get; init; } = new();
    public List<ApiUsageRecord> ApiUsage { get; init; } = new();
    public List<EventUsageRecord> Events { get; init; } = new();
    public List<GlobalRecord> Globals { get; init; } = new();
    public List<InterfaceDependency> Dependencies { get; init; } = new();
    public List<ScanDiagnostic> Diagnostics { get; init; } = new();
    public ScanMetadata? Metadata { get; set; }
}

public sealed record ScanProgress(string Phase, int Processed, int? Total, string? CurrentFile);
public sealed record ResolvedAsset(string RequestedWowPath, string NormalizedWowPath, InterfaceFileRecord? File, IReadOnlyList<string> AttemptedPaths)
{
    public bool Found => File is not null;
}

public sealed record DecodedTexture(int Width, int Height, byte[] Rgba32, TextureMetadata Metadata);
