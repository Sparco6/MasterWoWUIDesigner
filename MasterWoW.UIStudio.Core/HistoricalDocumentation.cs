using System.Text.Json;
using System.Text.RegularExpressions;

namespace MasterWoW.UIStudio.Core;

public enum HistoricalSourceStatus { Reachable, Unavailable, Cached, Disabled }
public enum KnowledgeAvailability { Supported, NotSupported, Uncertain, CustomOnly, CompatProvided }
public enum EmulationStrategy { VisualModel, MockQuery, SimulatedAction, SafeNoOp, Unsupported }
public enum EvidenceConfidence { DirectClientEvidence, CompatSourceEvidence, AddonUsageEvidence, HistoricalDocumentation, VersionInference, Unknown }

public sealed record HistoricalSourceDefinition(string Key,string Url,string Purpose);
public sealed record HistoricalSourceResult(string Key,string Url,HistoricalSourceStatus Status,DateTimeOffset CheckedAt,string? Error,int ImportedEntries);
public sealed record HistoricalApiFact(string Name,string Kind,string? Category,string? OwnerType,string? BaseType,string? Signature,IReadOnlyList<string> Arguments,IReadOnlyList<string> Returns,string? IntroducedVersion,IReadOnlyList<string> ChangedVersions,string? RemovedVersion,string Documentation,string SourceUrl,EvidenceConfidence Confidence);
public sealed record HistoricalDocumentationSnapshot(string SchemaVersion,DateTimeOffset RefreshedAt,IReadOnlyList<HistoricalSourceResult> Sources,IReadOnlyList<HistoricalApiFact> Entries);

public sealed class HistoricalApiDocumentationImporter
{
    private static readonly Regex Link=new(@"href\s*=\s*[""'](?<url>(?:https://wowwiki-archive\.fandom\.com)?/wiki/(?<slug>[^""'#?]+))[^""']*[""'][^>]*>(?<text>.*?)</a>",RegexOptions.IgnoreCase|RegexOptions.Compiled|RegexOptions.Singleline);
    private static readonly Regex Tags=new("<[^>]+>",RegexOptions.Compiled);
    public IReadOnlyList<HistoricalApiFact> ParseIndexHtml(string html,string sourceUrl,string kind)
    {
        var facts=new Dictionary<string,HistoricalApiFact>(StringComparer.OrdinalIgnoreCase);
        foreach(Match match in Link.Matches(html)){var text=System.Net.WebUtility.HtmlDecode(Tags.Replace(match.Groups["text"].Value," ")).Trim();var slug=Uri.UnescapeDataString(match.Groups["slug"].Value).Replace('_',' ');var name=text.Length>0?text:slug;if(!LooksLikeApi(name,kind))continue;var url=match.Groups["url"].Value;if(url.StartsWith('/'))url="https://wowwiki-archive.fandom.com"+url;facts.TryAdd(name,new(name,kind,null,kind=="WidgetMethod"?"Uncertain":null,null,name+"(...) ",Array.Empty<string>(),Array.Empty<string>(),null,Array.Empty<string>(),null,"Historical index link; individual-page details not yet imported.",url,EvidenceConfidence.HistoricalDocumentation));}
        return facts.Values.OrderBy(x=>x.Name).ToList();
    }
    public HistoricalDocumentationSnapshot ImportCachedMetadata(string json)
        =>JsonSerializer.Deserialize<HistoricalDocumentationSnapshot>(json,Options)??throw new InvalidDataException("Historical documentation metadata is empty.");
    public void SaveCache(HistoricalDocumentationSnapshot snapshot,string path){Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllText(path,JsonSerializer.Serialize(snapshot,Options));}
    private static bool LooksLikeApi(string name,string kind){if(name.Length is <2 or >80)return false;if(kind=="WidgetMethod")return Regex.IsMatch(name,@"^[A-Z][A-Za-z0-9_]*$");return Regex.IsMatch(name,@"^[A-Z][A-Za-z0-9_]*(?:\(\))?$");}
    private static readonly JsonSerializerOptions Options=new(){PropertyNameCaseInsensitive=true,WriteIndented=true,Converters={new System.Text.Json.Serialization.JsonStringEnumConverter()}};
}

public sealed record UnifiedApiKnowledge(string Name,string Kind,string OwnerType,KnowledgeAvailability StockTbc,KnowledgeAvailability MasterWow,EmulationStrategy Strategy,EmulatorImplementationStatus Implementation,bool ObservedInClient,int AddonFiles,bool ProvidedByCompat,IReadOnlyList<ApiReference> HistoricalSources,IReadOnlyList<string> Evidence,bool Conflict);
public sealed class UnifiedApiKnowledgeBuilder
{
    public IReadOnlyList<UnifiedApiKnowledge> Build(IEnumerable<ApiCatalogEntry> catalog,AddonCorpusIndex corpus,TbcClientIndex? client,HistoricalDocumentationSnapshot? historical)
    {
        var native=catalog.GroupBy(x=>x.Name,StringComparer.OrdinalIgnoreCase).ToDictionary(x=>x.Key,x=>x.First(),StringComparer.OrdinalIgnoreCase);var addon=corpus.WidgetMethods.Concat(corpus.GlobalApis).GroupBy(x=>x.Name,StringComparer.OrdinalIgnoreCase).ToDictionary(x=>x.Key,x=>x.Max(y=>y.DistinctFiles),StringComparer.OrdinalIgnoreCase);var compat=corpus.CompatibilityApis.Select(x=>x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);var observed=(client?.ApiUsage.Select(x=>x.Name)??Array.Empty<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);var history=(historical?.Entries??Array.Empty<HistoricalApiFact>()).GroupBy(x=>x.Name,StringComparer.OrdinalIgnoreCase).ToDictionary(x=>x.Key,x=>x.ToList(),StringComparer.OrdinalIgnoreCase);
        var names=native.Keys.Concat(addon.Keys).Concat(compat).Concat(observed).Concat(history.Keys).Distinct(StringComparer.OrdinalIgnoreCase);return names.Select(name=>{native.TryGetValue(name,out var api);addon.TryGetValue(name,out var files);history.TryGetValue(name,out var docs);var supplied=compat.Contains(name);var stock=api?.Tbc243Availability switch{Tbc243Availability.SupportedInTBC=>KnowledgeAvailability.Supported,Tbc243Availability.NotInTBC=>KnowledgeAvailability.NotSupported,_=>KnowledgeAvailability.Uncertain};var master=supplied?KnowledgeAvailability.CompatProvided:observed.Contains(name)&&stock==KnowledgeAvailability.NotSupported?KnowledgeAvailability.CustomOnly:stock;var strategy=Strategy(api,name);var refs=(docs??new()).Select(x=>new ApiReference("WoWWiki Archive",x.SourceUrl,x.Documentation)).ToList();var conflict=observed.Contains(name)&&stock==KnowledgeAvailability.NotSupported;var evidence=new List<string>();if(observed.Contains(name))evidence.Add("Observed in extracted client");if(files>0)evidence.Add($"Observed in {files} addon files");if(supplied)evidence.Add("Provided by Compat.lua");if(refs.Count>0)evidence.Add("Historical documentation metadata");return new UnifiedApiKnowledge(name,api?.OwnerType=="Global"?"GlobalApi":"WidgetMethod",api?.OwnerType??"Unknown",stock,master,strategy,api?.EmulatorImplementationStatus??EmulatorImplementationStatus.NotImplemented,observed.Contains(name),files,supplied,refs,evidence,conflict);}).OrderBy(x=>x.Name).ToList();
    }
    private static EmulationStrategy Strategy(ApiCatalogEntry? api,string name)=>name switch{"CreateFrame"=>EmulationStrategy.VisualModel,"GetItemInfo" or "GetItemIcon" or "GetSpellInfo" or "UnitName" or "UnitLevel"=>EmulationStrategy.MockQuery,"SendChatMessage"=>EmulationStrategy.SimulatedAction,"PlaySound"=>EmulationStrategy.SafeNoOp,_=>api?.EmulatorImplementationStatus==EmulatorImplementationStatus.Implemented?EmulationStrategy.VisualModel:EmulationStrategy.Unsupported};
}
