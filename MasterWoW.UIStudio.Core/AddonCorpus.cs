using System.Text.RegularExpressions;

namespace MasterWoW.UIStudio.Core;

public enum SourceClassification { AioClient, AioServer, NormalAddon, FrameXmlLua, FrameXmlXml, GlueXmlLua, GlueXmlXml, CustomClientLua, CompatibilityLayer, UnknownLua, Toc }
public enum LuaExecutionContext { AutoDetect, AddonClient, AioClient, FrameXmlInGame, GlueXml, CustomClient, CompatibilityLayer, StaticAnalysisOnly }
public enum ApiEvidenceKind { NativeTbc, CompatibilityApi, DesignerMock, UnsupportedLaterApi, Unknown, CustomNative, CustomCompat, CustomUnknown }

public sealed record TocMetadata(string Path, IReadOnlyDictionary<string,string> Headers, IReadOnlyList<string> OrderedFiles, IReadOnlyList<string> Dependencies, IReadOnlyList<string> OptionalDependencies, IReadOnlyList<string> SavedVariables, IReadOnlyList<string> SavedVariablesPerCharacter);
public sealed record CorpusFile(string Path, string RelativePath, string SourceRoot, SourceClassification Classification, LuaExecutionContext DetectedContext, long Size, IReadOnlyList<string> Reasons);
public sealed record CorpusUsage(string Name, int Count, int DistinctFiles, int DistinctSources, IReadOnlyList<SourceOccurrence> Occurrences, double PriorityScore, bool InitializationCritical, string Category);
public sealed record AioHandlerRecord(string ModuleExpression, string HandlerName, string File, int Line, IReadOnlyList<string> AccessedFields);
public sealed record CompatibilityApiRecord(string Name, string Kind, string File, int Line, string Evidence, bool ConditionalFallback);
public sealed class AddonCorpusIndex
{
    public List<string> ScannedRoots { get; }=new(); public List<CorpusFile> Files { get; }=new(); public List<TocMetadata> Tocs { get; }=new(); public List<CorpusUsage> WidgetMethods { get; }=new(); public List<CorpusUsage> GlobalApis { get; }=new(); public List<CorpusUsage> Templates { get; }=new(); public List<CorpusUsage> Events { get; }=new(); public List<CorpusUsage> FrameTypes { get; }=new(); public List<AioHandlerRecord> AioHandlers { get; }=new(); public List<CompatibilityApiRecord> CompatibilityApis { get; }=new(); public List<ScanDiagnostic> Diagnostics { get; }=new();
}

public sealed class TocParser
{
    public TocMetadata Parse(string path)
    {
        var headers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);var ordered=new List<string>();foreach(var raw in File.ReadLines(path)){var line=raw.Trim();if(line.Length==0)continue;if(line.StartsWith("##")){var colon=line.IndexOf(':');if(colon>2)headers[line[2..colon].Trim()]=line[(colon+1)..].Trim();continue;}if(line.StartsWith("#"))continue;ordered.Add(line.Replace('/','\\'));}
        return new(path,headers,ordered,Split("Dependencies","RequiredDeps"),Split("OptionalDeps"),Split("SavedVariables"),Split("SavedVariablesPerCharacter"));
        List<string> Split(params string[] keys){foreach(var key in keys)if(headers.TryGetValue(key,out var value))return value.Split(new[]{',',' '},StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).ToList();return new();}
    }
}

public sealed class AddonCorpusScanner
{
    private static readonly Dictionary<string,int[]> LineIndexes=new(ReferenceEqualityComparer.Instance);
    private static readonly Regex MethodCall=new(@"(?m)(?<owner>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\s*:\s*(?<name>[A-Za-z_]\w*)\s*\(",RegexOptions.Compiled);
    private static readonly Regex GlobalCall=new(@"(?m)(?<![.:\w])(?<name>[A-Za-z_]\w*)\s*\(",RegexOptions.Compiled);
    private static readonly Regex CreateFrameCall=new(@"CreateFrame\s*\(\s*[\""'](?<type>[A-Za-z_]\w*)[\""'](?:\s*,[^,\)]*){0,2}(?:\s*,\s*[\""'](?<template>[^\""']+)[\""'])?",RegexOptions.Compiled);
    private static readonly Regex RegisterEventCall=new(@"RegisterEvent\s*\(\s*[\""'](?<event>[A-Z][A-Z0-9_]+)[\""']",RegexOptions.Compiled);
    private static readonly Regex AioHandlers=new(@"(?m)(?<var>[A-Za-z_]\w*)\s*=\s*AIO\.AddHandlers\s*\(\s*(?<module>[^,\)]+)",RegexOptions.Compiled);
    private static readonly Regex HandlerAssignment=new(@"(?m)(?:function\s+)?(?<var>[A-Za-z_]\w*)[\.:](?<handler>[A-Za-z_]\w*)\s*(?:=\s*function)?\s*\(",RegexOptions.Compiled);
    private static readonly Regex FieldAccess=new(@"\b(?:data|payload|snapshot|response)\.(?<field>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)",RegexOptions.Compiled|RegexOptions.IgnoreCase);
    private static readonly Regex CompatPolyfill=new(@"(?m)_AIO_POLYFILLS\.(?<name>[A-Za-z_]\w*)\s*=|if\s+type\s*\(\s*(?<guard>[A-Za-z_]\w*)\s*\)\s*~=\s*[\""']function[\""']|^\s*function\s+(?<global>[A-Za-z_]\w*)\s*\(",RegexOptions.Compiled);
    private static readonly HashSet<string> LuaKeywords=new(StringComparer.Ordinal){"if","elseif","for","while","function","return","and","or","not","type","pairs","ipairs","tostring","tonumber","select","unpack","error","pcall","xpcall","assert","require","next","setmetatable","getmetatable","rawget","rawset"};
    private static readonly HashSet<string> Critical=new(StringComparer.OrdinalIgnoreCase){"CreateFrame","SetPoint","SetWidth","SetHeight","SetSize","SetParent","CreateTexture","CreateFontString","SetBackdrop","AddAddon","AddHandlers"};

    public IReadOnlyList<string> DiscoverWorkspaceSources(string workspaceRoot)
    {
        var skip=new HashSet<string>(StringComparer.OrdinalIgnoreCase){".git","MasterWoW.UIStudio","MasterWoW-Project","audit_work","audit-tools","DatabaseDump","TBCClientEdit","WeakAuras2-TBC","ZDocumentations","tools"};
        return Directory.EnumerateDirectories(workspaceRoot).Where(d=>!skip.Contains(Path.GetFileName(d))).Where(d=>Directory.EnumerateFiles(d,"*.lua",SearchOption.AllDirectories).Take(1).Any()||Directory.EnumerateFiles(d,"*.toc",SearchOption.AllDirectories).Take(1).Any()).OrderBy(x=>x).ToList();
    }
    public async Task<AddonCorpusIndex> ScanAsync(IEnumerable<string> roots,CancellationToken ct=default,IProgress<ScanProgress>? progress=null)
    {
        var index=new AddonCorpusIndex();var rootList=roots.Select(Path.GetFullPath).Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();index.ScannedRoots.AddRange(rootList);var paths=rootList.SelectMany(root=>Directory.EnumerateFiles(root,"*",SearchOption.AllDirectories).Where(p=>Path.GetExtension(p) is ".lua" or ".xml" or ".toc").Select(p=>(root,p))).ToList();var usages=new UsageCollector();var processed=0;
        foreach(var (root,path) in paths){ct.ThrowIfCancellationRequested();try{var ext=Path.GetExtension(path).ToLowerInvariant();var source=await File.ReadAllTextAsync(path,ct);var classification=Classify(path,source,ext,out var reasons);var context=ContextFor(classification);var record=new CorpusFile(path,Path.GetRelativePath(root,path),root,classification,context,new FileInfo(path).Length,reasons);index.Files.Add(record);if(ext==".toc"){index.Tocs.Add(new TocParser().Parse(path));continue;}if(ext==".lua"&&classification!=SourceClassification.AioServer)ScanLua(record,source,index,usages);else if(ext==".xml")ScanXml(record,source,usages);}catch(Exception ex){index.Diagnostics.Add(new("Warning","Corpus scan",ex.Message,path));}if(++processed%100==0)progress?.Report(new("Scanning addon corpus",processed,paths.Count,path));}
        usages.CopyTo(index);return index;
    }
    public static SourceClassification Classify(string path,string source,string extension,out IReadOnlyList<string> reasons)
    {
        var why=new List<string>();var normalized=path.Replace('/','\\');if(extension==".toc"){why.Add("TOC manifest");reasons=why;return SourceClassification.Toc;}if(normalized.Contains("\\GlueXML\\",StringComparison.OrdinalIgnoreCase)){why.Add("Located under GlueXML");reasons=why;return extension==".xml"?SourceClassification.GlueXmlXml:SourceClassification.GlueXmlLua;}if(normalized.Contains("\\FrameXML\\",StringComparison.OrdinalIgnoreCase)){why.Add("Located under FrameXML");if(Path.GetFileName(path).Contains("Compat",StringComparison.OrdinalIgnoreCase)||source.Contains("_AIO_POLYFILLS")){why.Add("Defines compatibility fallbacks");reasons=why;return SourceClassification.CompatibilityLayer;}reasons=why;return extension==".xml"?SourceClassification.FrameXmlXml:SourceClassification.FrameXmlLua;}if(extension==".xml"){why.Add("Addon XML outside client trees");reasons=why;return SourceClassification.NormalAddon;}var aio=source.Contains("AIO.AddAddon")||source.Contains("AIO.AddHandlers");var server=source.Contains("RegisterPlayerEvent")||Regex.IsMatch(source,@"AIO\.Handle\s*\(\s*player\b",RegexOptions.IgnoreCase);if(aio){why.Add("Uses AIO registration");if(server){why.Add("Uses server/player APIs");reasons=why;return SourceClassification.AioServer;}why.Add("No server-only marker");reasons=why;return SourceClassification.AioClient;}if(Path.GetFileName(path).Contains("Compat",StringComparison.OrdinalIgnoreCase)||source.Contains("polyfill",StringComparison.OrdinalIgnoreCase)){why.Add("Compatibility naming/content");reasons=why;return SourceClassification.CompatibilityLayer;}if(source.Contains("CreateFrame")||source.Contains("RegisterEvent")){why.Add("Creates or registers UI frames");reasons=why;return SourceClassification.NormalAddon;}why.Add("No decisive runtime marker");reasons=why;return SourceClassification.UnknownLua;
    }
    private static LuaExecutionContext ContextFor(SourceClassification c)=>c switch{SourceClassification.AioClient=>LuaExecutionContext.AioClient,SourceClassification.FrameXmlLua or SourceClassification.FrameXmlXml=>LuaExecutionContext.FrameXmlInGame,SourceClassification.GlueXmlLua or SourceClassification.GlueXmlXml=>LuaExecutionContext.GlueXml,SourceClassification.CustomClientLua=>LuaExecutionContext.CustomClient,SourceClassification.CompatibilityLayer=>LuaExecutionContext.CompatibilityLayer,SourceClassification.AioServer=>LuaExecutionContext.StaticAnalysisOnly,_=>LuaExecutionContext.AddonClient};
    private static void ScanLua(CorpusFile file,string source,AddonCorpusIndex index,UsageCollector usages)
    {
        foreach(Match m in MethodCall.Matches(source))usages.Add("Widget",m.Groups["name"].Value,file,source,m);foreach(Match m in GlobalCall.Matches(source)){var name=m.Groups["name"].Value;if(!LuaKeywords.Contains(name))usages.Add("Global",name,file,source,m);}foreach(Match m in CreateFrameCall.Matches(source)){usages.AddValue("FrameType",m.Groups["type"].Value,file,source,m);if(m.Groups["template"].Success)foreach(var t in m.Groups["template"].Value.Split(','))usages.AddValue("Template",t.Trim(),file,source,m);}foreach(Match m in RegisterEventCall.Matches(source))usages.AddValue("Event",m.Groups["event"].Value,file,source,m);
        var handlerVars=AioHandlers.Matches(source).Cast<Match>().ToDictionary(m=>m.Groups["var"].Value,m=>m.Groups["module"].Value.Trim(),StringComparer.Ordinal);foreach(Match m in HandlerAssignment.Matches(source))if(handlerVars.TryGetValue(m.Groups["var"].Value,out var module)){var line=Line(source,m.Index);var bodyEnd=Math.Min(source.Length,m.Index+4000);var fields=FieldAccess.Matches(source[m.Index..bodyEnd]).Select(x=>x.Groups["field"].Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();index.AioHandlers.Add(new(module,m.Groups["handler"].Value,file.Path,line,fields));}
        if(file.Classification==SourceClassification.CompatibilityLayer)foreach(Match m in CompatPolyfill.Matches(source)){var group=m.Groups["name"].Success?m.Groups["name"]:m.Groups["guard"].Success?m.Groups["guard"]:m.Groups["global"];if(group.Success)index.CompatibilityApis.Add(new(group.Value,m.Groups["name"].Success?"WidgetPolyfill":"GlobalPolyfill",file.Path,Line(source,m.Index),m.Value.Trim(),m.Groups["guard"].Success));}
    }
    private static void ScanXml(CorpusFile file,string source,UsageCollector usages){foreach(Match m in Regex.Matches(source,@"(?i)<(?<type>Frame|Button|CheckButton|EditBox|ScrollFrame|Slider|StatusBar|Texture|FontString)\b"))usages.AddValue("FrameType",m.Groups["type"].Value,file,source,m);foreach(Match m in Regex.Matches(source,@"(?i)\binherits\s*=\s*[\""'](?<template>[^\""']+)[\""']"))foreach(var t in m.Groups["template"].Value.Split(','))usages.AddValue("Template",t.Trim(),file,source,m);foreach(Match m in Regex.Matches(source,@"(?i)<OnEvent>.*?RegisterEvent\s*\(\s*[\""'](?<event>[A-Z0-9_]+)",RegexOptions.Singleline))usages.AddValue("Event",m.Groups["event"].Value,file,source,m);}
    private static int Line(string source,int index)
    {
        if(!LineIndexes.TryGetValue(source,out var starts)){var list=new List<int>{0};for(var i=0;i<source.Length;i++)if(source[i]=='\n')list.Add(i+1);starts=list.ToArray();LineIndexes[source]=starts;}
        var found=Array.BinarySearch(starts,index);return found>=0?found+1:~found;
    }
    private sealed class UsageCollector
    {
        private readonly Dictionary<(string Category,string Name),List<(CorpusFile File,SourceOccurrence Occurrence)>> _data=new();
        public void Add(string category,string name,CorpusFile file,string source,Match m)=>AddValue(category,name,file,source,m);
        public void AddValue(string category,string name,CorpusFile file,string source,Match m){if(string.IsNullOrWhiteSpace(name))return;var line=Line(source,m.Index);var start=Math.Max(0,source.LastIndexOf('\n',Math.Max(0,m.Index-1))+1);var end=source.IndexOf('\n',m.Index);if(end<0)end=source.Length;var key=(category,name);if(!_data.TryGetValue(key,out var list))_data[key]=list=new();list.Add((file,new(file.Path,line,source[start..end].Trim())));}
        public void CopyTo(AddonCorpusIndex index){foreach(var group in _data){var files=group.Value.Select(x=>x.File.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count();var sources=group.Value.Select(x=>x.File.SourceRoot).Distinct(StringComparer.OrdinalIgnoreCase).Count();var critical=Critical.Contains(group.Key.Name);var score=group.Value.Count*Math.Max(1,files)*Math.Max(1,sources)*(critical?3:1);var usage=new CorpusUsage(group.Key.Name,group.Value.Count,files,sources,group.Value.Select(x=>x.Occurrence).ToList(),score,critical,group.Key.Category);(group.Key.Category switch{"Widget"=>index.WidgetMethods,"Global"=>index.GlobalApis,"Template"=>index.Templates,"Event"=>index.Events,"FrameType"=>index.FrameTypes,_=>index.GlobalApis}).Add(usage);}index.WidgetMethods.Sort((a,b)=>b.PriorityScore.CompareTo(a.PriorityScore));index.GlobalApis.Sort((a,b)=>b.PriorityScore.CompareTo(a.PriorityScore));index.Templates.Sort((a,b)=>b.PriorityScore.CompareTo(a.PriorityScore));index.Events.Sort((a,b)=>b.PriorityScore.CompareTo(a.PriorityScore));index.FrameTypes.Sort((a,b)=>b.PriorityScore.CompareTo(a.PriorityScore));}
    }
}

public sealed record EffectiveApiRecord(string Name, Tbc243Availability NativeTbc, bool ProvidedByCompat, EmulatorImplementationStatus Emulator, string EffectiveSupport, IReadOnlyList<string> Evidence);
public sealed class EffectiveApiRegistry
{
    public IReadOnlyList<EffectiveApiRecord> Merge(IEnumerable<ApiCatalogEntry> native,AddonCorpusIndex corpus)
    {
        var compat=corpus.CompatibilityApis.GroupBy(x=>x.Name,StringComparer.OrdinalIgnoreCase).ToDictionary(x=>x.Key,x=>x.ToList(),StringComparer.OrdinalIgnoreCase);var emulator=native.ToDictionary(x=>x.Name,x=>x,StringComparer.OrdinalIgnoreCase);var names=native.Select(x=>x.Name).Concat(compat.Keys).Concat(corpus.WidgetMethods.Select(x=>x.Name)).Concat(corpus.GlobalApis.Select(x=>x.Name)).Distinct(StringComparer.OrdinalIgnoreCase);
        return names.Select(name=>{emulator.TryGetValue(name,out var api);var hasCompat=compat.TryGetValue(name,out var c);var implementation=api?.EmulatorImplementationStatus??EmulatorImplementationStatus.NotImplemented;var nativeStatus=api?.Tbc243Availability??Tbc243Availability.Uncertain;var effective=implementation==EmulatorImplementationStatus.Implemented?"Supported":hasCompat&&implementation is EmulatorImplementationStatus.Partial or EmulatorImplementationStatus.Mocked?"Partial":hasCompat?"Available through compatibility layer":nativeStatus==Tbc243Availability.NotInTBC?"Unsupported":"Unknown/Not implemented";var evidence=(c??new()).Select(x=>$"Compat: {Path.GetFileName(x.File)}:{x.Line}").ToList();return new EffectiveApiRecord(name,nativeStatus,hasCompat,implementation,effective,evidence);}).OrderBy(x=>x.Name).ToList();
    }
}
