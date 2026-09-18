using System.Text;

namespace MasterWoW.UIStudio.Core;

public sealed class CorpusReportWriter
{
    public void WriteCoverage(AddonCorpusIndex index,string path)
    {
        var b=new StringBuilder("# Real Addon API Coverage\n\nGenerated from the workspace corpus; counts below are scanner observations, not estimates.\n\n");
        b.AppendLine($"- Source folders: {index.ScannedRoots.Count}");
        var luaCount=index.Files.Count(x=>Path.GetExtension(x.Path).Equals(".lua",StringComparison.OrdinalIgnoreCase));
        var xmlCount=index.Files.Count(x=>Path.GetExtension(x.Path).Equals(".xml",StringComparison.OrdinalIgnoreCase));
        b.AppendLine($"- Lua files: {luaCount}");
        b.AppendLine($"- XML files: {xmlCount}");
        b.AppendLine($"- TOC files: {index.Tocs.Count}"); b.AppendLine($"- Unique widget methods: {index.WidgetMethods.Count}"); b.AppendLine($"- Unique global calls: {index.GlobalApis.Count}");
        b.AppendLine($"- AIO handlers: {index.AioHandlers.Count}"); b.AppendLine($"- Compatibility definitions: {index.CompatibilityApis.Select(x=>x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()}\n");
        Section(b,"Classifications",index.Files.GroupBy(x=>x.Classification).OrderByDescending(x=>x.Count()).Select(x=>(x.Key.ToString(),x.Count(),0)));
        Usage(b,"Widget methods",index.WidgetMethods); Usage(b,"Global calls",index.GlobalApis); Usage(b,"Frame types",index.FrameTypes); Usage(b,"Templates",index.Templates); Usage(b,"Events",index.Events);
        b.AppendLine("## Compatibility APIs\n"); foreach(var x in index.CompatibilityApis.GroupBy(x=>x.Name,StringComparer.OrdinalIgnoreCase).OrderBy(x=>x.Key))b.AppendLine($"- `{x.Key}` — {x.Count()} evidence record(s)");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path,b.ToString());
    }
    public void WriteBacklog(AddonCorpusIndex index,string path,ISet<string> implemented)
    {
        var missing=index.WidgetMethods.Concat(index.GlobalApis).Where(x=>!implemented.Contains(x.Name)).OrderByDescending(x=>x.PriorityScore).Take(100).ToList();
        var b=new StringBuilder("# Emulator Backlog\n\nRanked from actual usage: call count × distinct files × distinct source folders × initialization-critical weighting.\n\n");
        b.AppendLine("| Rank | API | Category | Calls | Files | Sources | Critical | Score |\n|---:|---|---|---:|---:|---:|:---:|---:|");
        for(var i=0;i<missing.Count;i++){var x=missing[i];b.AppendLine($"| {i+1} | `{x.Name}` | {x.Category} | {x.Count} | {x.DistinctFiles} | {x.DistinctSources} | {(x.InitializationCritical?"Yes":"No")} | {x.PriorityScore:0} |");}
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path,b.ToString());
    }
    private static void Usage(StringBuilder b,string title,IEnumerable<CorpusUsage> rows){b.AppendLine($"## {title}\n\n| Name | Calls | Files | Sources |\n|---|---:|---:|---:|");foreach(var x in rows.Take(50))b.AppendLine($"| `{x.Name}` | {x.Count} | {x.DistinctFiles} | {x.DistinctSources} |");b.AppendLine();}
    private static void Section(StringBuilder b,string title,IEnumerable<(string Name,int Count,int Dummy)> rows){b.AppendLine($"## {title}\n");foreach(var x in rows)b.AppendLine($"- {x.Name}: {x.Count}");b.AppendLine();}
}
