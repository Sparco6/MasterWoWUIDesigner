using System.Text.Json;

namespace MasterWoW.UIStudio.Core;

public enum Tbc243Availability { SupportedInTBC, NotInTBC, Uncertain }
public enum EmulatorImplementationStatus { Implemented, Partial, Mocked, NoOp, NotImplemented }
public enum CatalogTestStatus { Covered, Partial, NotCovered }

public sealed record ApiArgument(string Name, string Type, bool Optional, string? Description);
public sealed record ApiReturnValue(string Name, string Type, string? Description);
public sealed record ApiReference(string Title, string Url, string Evidence);
public sealed record ApiCatalogEntry(
    string Name,
    string OwnerType,
    string BaseType,
    string Signature,
    IReadOnlyList<ApiArgument> Arguments,
    IReadOnlyList<ApiReturnValue> ReturnValues,
    string? IntroducedVersion,
    string? RemovedVersion,
    Tbc243Availability Tbc243Availability,
    string Documentation,
    IReadOnlyList<ApiReference> SourceReference,
    EmulatorImplementationStatus EmulatorImplementationStatus,
    CatalogTestStatus TestStatus,
    string Notes);

public sealed record ApiCatalogFile(string SchemaVersion, string Target, string Kind, DateTimeOffset GeneratedAt, IReadOnlyList<ApiCatalogEntry> Entries);

public static class ApiCatalogLoader
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
    public static ApiCatalogFile Load(string path) => JsonSerializer.Deserialize<ApiCatalogFile>(File.ReadAllText(path), Options) ?? throw new InvalidDataException($"Empty API catalog: {path}");
    public static IReadOnlyList<string> Validate(ApiCatalogFile catalog)
    {
        var errors = new List<string>();
        if (catalog.SchemaVersion != "1.0") errors.Add("Unsupported schemaVersion.");
        if (catalog.Target != "2.4.3.8606") errors.Add("Catalog target must be 2.4.3.8606.");
        foreach (var duplicate in catalog.Entries.GroupBy(x => $"{x.OwnerType}:{x.Name}", StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1)) errors.Add($"Duplicate entry: {duplicate.Key}");
        foreach (var entry in catalog.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name) || string.IsNullOrWhiteSpace(entry.OwnerType) || string.IsNullOrWhiteSpace(entry.BaseType) || string.IsNullOrWhiteSpace(entry.Signature)) errors.Add($"Incomplete identity/signature: {entry.Name}");
            if (string.IsNullOrWhiteSpace(entry.Documentation) || entry.SourceReference.Count == 0 || entry.SourceReference.Any(x => string.IsNullOrWhiteSpace(x.Url) || string.IsNullOrWhiteSpace(x.Evidence))) errors.Add($"Missing documentation/evidence: {entry.OwnerType}:{entry.Name}");
            if (entry.Tbc243Availability == Tbc243Availability.SupportedInTBC && entry.IntroducedVersion is { } v && Version.TryParse(v, out var introduced) && introduced > new Version(2, 4, 3)) errors.Add($"Contradictory supported version: {entry.OwnerType}:{entry.Name}");
        }
        return errors;
    }
}

public static class DeveloperKitApiLoader
{
    public static IReadOnlyList<ApiCatalogEntry> LoadRuntimeFunctions(string path)
    {
        if(!File.Exists(path))return Array.Empty<ApiCatalogEntry>();
        var entries=new List<ApiCatalogEntry>();var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var line in File.ReadLines(path))
        {
            var match=System.Text.RegularExpressions.Regex.Match(line,@"^\s*(?<name>[A-Za-z_]\w*)\s+\[function\]\s*$");
            if(!match.Success||!seen.Add(match.Groups["name"].Value))continue;
            var name=match.Groups["name"].Value;
            entries.Add(new(name,"Global","RuntimeObservedGlobal",name+"(...)" ,Array.Empty<ApiArgument>(),Array.Empty<ApiReturnValue>(),null,null,Tbc243Availability.SupportedInTBC,"Observed as a callable global by MasterWoW Developer Kit in the running 2.4.3 client.",new[]{new ApiReference("MasterWoW Developer Kit runtime export","local://MasterWoW-Developer-Kit/APIwithoutaddons.txt","Observed with non-development addons excluded; presence is proven, but parameters and return values were not introspected.")},EmulatorImplementationStatus.NotImplemented,CatalogTestStatus.NotCovered,"Runtime inventory only; signature is intentionally unknown."));
        }
        return entries;
    }
}
