using System.Globalization;
using System.Text.RegularExpressions;

namespace MasterWoW.UIStudio.Core;

public sealed class LuaSourceMapper
{
    private static readonly Regex Assignment = new(@"(?m)^\s*(?:local\s+)?(?<var>[A-Za-z_]\w*)\s*=\s*CreateFrame\s*\(\s*[""'](?<type>\w+)[""']\s*,\s*(?:(?:[""'](?<name>[^""']+)[""'])|nil)", RegexOptions.Compiled);
    private static readonly Regex TextureCreation = new(@"(?m)^\s*(?:local\s+)?(?<var>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\s*=\s*[^\r\n]*?(?<call>:\s*CreateTexture\s*\()", RegexOptions.Compiled);
    private static readonly Regex FontStringCreation = new(@"(?m)^\s*(?:local\s+)?(?<var>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\s*=\s*[^\r\n]*?(?<call>:\s*CreateFontString\s*\()", RegexOptions.Compiled);

    public void Map(string source, WowDocument document)
    {
        foreach (Match creation in Assignment.Matches(source))
        {
            var variable = creation.Groups["var"].Value;
            var name = creation.Groups["name"].Success ? creation.Groups["name"].Value : string.Empty;
            var target = !string.IsNullOrEmpty(name) && document.ByName.TryGetValue(name, out var named)
                ? named : document.ByName.Values.FirstOrDefault(x => x.Name.StartsWith(variable + "#", StringComparison.Ordinal));
            if (target is null) continue;
            MapNumeric(source, variable, target, "SetWidth", "Width");
            MapNumeric(source, variable, target, "SetHeight", "Height");
            MapSize(source, variable, target);
            MapPoint(source, variable, target);
        }
        MapRegionCreation(source, document, WowObjectType.Texture, TextureCreation);
        MapRegionCreation(source, document, WowObjectType.FontString, FontStringCreation);
    }

    public void MapExpression(string source,string expression,WowUiObject target)
    {
        var assignment=new Regex($@"(?m)^\s*(?:local\s+)?{Regex.Escape(expression)}\s*=\s*(?:Create(?:Frame|Section)\s*\(|[^\r\n]*?:\s*Create(?:Texture|FontString)\s*\()",RegexOptions.Compiled).Match(source);
        var start=assignment.Success?assignment.Index:0;
        foreach(var key in new[]{"Creation","Width","Height","X","Y"})target.Sources.Remove(key);
        if(assignment.Success)target.Sources["Creation"]=Span(source,"Creation",assignment.Groups[0],false);
        MapNumeric(source,expression,target,"SetWidth","Width",start);
        MapNumeric(source,expression,target,"SetHeight","Height",start);
        MapSize(source,expression,target,start);
        MapSectionSize(source,expression,target,start);
        MapPoint(source,expression,target,start);
    }

    public void MapExpressions(string source,IEnumerable<(WowUiObject Item,string Expression)> tags)
    {
        var grouped=tags.GroupBy(x=>x.Expression,StringComparer.Ordinal).ToList();
        foreach(var group in grouped)
        {
            var first=group.First().Item;
            MapExpression(source,group.Key,first);
            foreach(var tag in group.Skip(1))
                foreach(var pair in first.Sources.Where(x=>x.Key is "Creation" or "Width" or "Height" or "X" or "Y"))tag.Item.Sources[pair.Key]=pair.Value;
        }
    }

    private static void MapRegionCreation(string source, WowDocument document, WowObjectType type, Regex creationPattern)
    {
        var creations=creationPattern.Matches(source).Cast<Match>().ToList();
        if(creations.Count==0)return;
        var regions=document.ByName.Values.Where(x=>x.Type==type&&!x.Sources.ContainsKey("Creation")).ToList();
        foreach(var region in regions)
        {
            var creation=creations[Math.Min(regions.IndexOf(region),creations.Count-1)];
            Group call=creation.Groups["call"];
            if(type==WowObjectType.Texture&&!string.IsNullOrWhiteSpace(region.TexturePath))
            {
                var path=region.TexturePath!;
                var index=source.IndexOf(path,StringComparison.OrdinalIgnoreCase);
                if(index<0)index=source.IndexOf(path.Replace('\\','/'),StringComparison.OrdinalIgnoreCase);
                if(index>=0){region.Sources["Creation"]=new("Creation",index,path.Length,source.Substring(index,Math.Min(path.Length,source.Length-index)),false,1+source.Take(index).Count(c=>c=='\n'));continue;}
            }
            region.Sources["Creation"]=Span(source,"Creation",call,false);
            var variable=creation.Groups["var"].Value;
            MapNumeric(source,variable,region,"SetWidth","Width");
            MapNumeric(source,variable,region,"SetHeight","Height");
            MapSize(source,variable,region);
            MapPoint(source,variable,region);
        }
    }

    private static void MapNumeric(string source, string variable, WowUiObject target, string method, string property,int start=0)
    {
        if(target.Sources.ContainsKey(property))return;
        var rx = new Regex($@"(?m)\b{Regex.Escape(variable)}\s*:\s*{method}\s*\(\s*(?<value>[-+]?\d+(?:\.\d+)?)\s*\)");
        var match = rx.Match(source,start); if (!match.Success) return;
        var value = match.Groups["value"];
        target.Sources[property] = Span(source, property, value, true);
    }

    private static void MapSize(string source, string variable, WowUiObject target,int start=0)
    {
        var rx = new Regex($@"(?m)\b{Regex.Escape(variable)}\s*:\s*SetSize\s*\(\s*(?<w>[-+]?\d+(?:\.\d+)?)\s*,\s*(?<h>[-+]?\d+(?:\.\d+)?)\s*\)");
        var match = rx.Match(source,start); if (!match.Success) return;
        if(!target.Sources.ContainsKey("Width"))target.Sources["Width"] = Span(source, "Width", match.Groups["w"], true);
        if(!target.Sources.ContainsKey("Height"))target.Sources["Height"] = Span(source, "Height", match.Groups["h"], true);
    }

    private static void MapPoint(string source, string variable, WowUiObject target,int start=0)
    {
        var escaped=Regex.Escape(variable);
        var full = new Regex($@"(?m)(?<![\w.]){escaped}\s*:\s*SetPoint\s*\(\s*[""'](?<p>\w+)[""']\s*,\s*[^,]+\s*,\s*[""'](?<rp>\w+)[""']\s*,\s*(?<x>[-+]?\d+(?:\.\d+)?)\s*,\s*(?<y>[-+]?\d+(?:\.\d+)?)\s*\)");
        var shortForm = new Regex($@"(?m)(?<![\w.]){escaped}\s*:\s*SetPoint\s*\(\s*[""'](?<p>\w+)[""']\s*,\s*(?<x>[-+]?\d+(?:\.\d+)?)\s*,\s*(?<y>[-+]?\d+(?:\.\d+)?)\s*\)");
        var match=full.Match(source,start);if(!match.Success)match=shortForm.Match(source,start);if(!match.Success)return;
        if(!target.Sources.ContainsKey("X"))target.Sources["X"] = Span(source, "X", match.Groups["x"], true);
        if(!target.Sources.ContainsKey("Y"))target.Sources["Y"] = Span(source, "Y", match.Groups["y"], true);
    }

    private static void MapSectionSize(string source,string variable,WowUiObject target,int start)
    {
        var rx=new Regex($@"(?m)^\s*(?:local\s+)?{Regex.Escape(variable)}\s*=\s*CreateSection\s*\(\s*[^,]+\s*,\s*(?<w>[-+]?\d+(?:\.\d+)?)\s*,\s*(?<h>[-+]?\d+(?:\.\d+)?)\s*\)");
        var match=rx.Match(source,start);if(!match.Success)return;
        if(!target.Sources.ContainsKey("Width"))target.Sources["Width"]=Span(source,"Width",match.Groups["w"],true);
        if(!target.Sources.ContainsKey("Height"))target.Sources["Height"]=Span(source,"Height",match.Groups["h"],true);
    }

    private static SourceSpan Span(string source, string property, Group value, bool literal) =>
        new(property, value.Index, value.Length, value.Value, literal, 1 + source.Take(value.Index).Count(c => c == '\n'));

    public string PatchLiteral(string source, SourceSpan span, double value)
    {
        if (!span.IsLiteral || span.ValueStart < 0 || span.ValueStart + span.ValueLength > source.Length ||
            source.Substring(span.ValueStart, span.ValueLength) != span.OriginalText)
            throw new InvalidOperationException($"{span.Property} is runtime-only or its source changed; refusing unsafe patch.");
        var replacement = value.ToString("0.###", CultureInfo.InvariantCulture);
        return source.Remove(span.ValueStart, span.ValueLength).Insert(span.ValueStart, replacement);
    }
}

internal static class LuaSourceInstrumentation
{
    private static readonly Regex CreationAssignment=new(@"(?m)^\s*(?:local\s+)?(?<lhs>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\s*=\s*(?<call>CreateFrame|CreateSection|[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*\s*:\s*Create(?:Texture|FontString))\s*\(",RegexOptions.Compiled);

    public static string Instrument(string source)
    {
        var edits=new List<(int Start,int End,string Lhs)>();
        foreach(Match match in CreationAssignment.Matches(source))
        {
            var open=source.IndexOf('(',match.Groups["call"].Index+match.Groups["call"].Length);
            var close=FindClosingParen(source,open);
            if(close>open)edits.Add((match.Groups["call"].Index,close+1,match.Groups["lhs"].Value));
        }
        foreach(var edit in edits.OrderByDescending(x=>x.Start))
        {
            var call=source[edit.Start..edit.End];
            source=source[..edit.Start]+$"__mw_source_map({call}, '{edit.Lhs}')"+source[edit.End..];
        }
        return source;
    }

    private static int FindClosingParen(string source,int open)
    {
        var depth=0;char quote='\0';var escape=false;
        for(var i=open;i<source.Length;i++)
        {
            var c=source[i];
            if(quote!='\0'){if(escape){escape=false;continue;}if(c=='\\'){escape=true;continue;}if(c==quote)quote='\0';continue;}
            if(c=='\''||c=='\"'){quote=c;continue;}
            if(c=='-'&&i+1<source.Length&&source[i+1]=='-'){var nl=source.IndexOf('\n',i+2);if(nl<0)return -1;i=nl;continue;}
            if(c=='(')depth++;else if(c==')'&&--depth==0)return i;
        }
        return -1;
    }
}
