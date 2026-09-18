using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
using MasterWoW.UIStudio.Core;

namespace MasterWoW.UIStudio.App;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ILuaPreviewRuntime _runtime = new MoonSharpPreviewRuntime();
    private readonly Stack<string> _undo = new(), _redo = new();
    private string _source = SampleSource;
    private WowDocument _document = new();
    private WowUiObject? _selected;
    private string? _filePath;
    private TbcClientIndex? _clientIndex;
    private CancellationTokenSource? _executionCancellation;
    private Dictionary<string,DesignerGeometry> _designerGeometry=new();
    private Dictionary<string,DesignerGeometry> _designerBaseGeometry=new();
    private Dictionary<string,string> _clientStrings=new(StringComparer.OrdinalIgnoreCase);
    public MainViewModel() => Reload();
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? PreviewChanged;
    public string Source { get => _source; set { if (_source == value) return; _source = value; OnChanged(); } }
    public WowDocument Document { get => _document; private set { _document = value; OnChanged(); OnChanged(nameof(Root)); } }
    public WowUiObject Root => Document.Root;
    public WowUiObject? Selected { get => _selected; set { _selected = value; OnChanged(); } }
    public string Title => _filePath is null ? "MasterWoW UI Studio — Untitled" : $"MasterWoW UI Studio — {Path.GetFileName(_filePath)}";
    public int PendingDesignerOverrideCount => _designerGeometry.Count;
    public string? DesignerLayoutPath => _filePath is null ? null : _filePath + ".mwstudio.layout.json";
    public double PreviewWidth { get; set; } = 1280;
    public double PreviewHeight { get; set; } = 720;
    public double UiScale { get; set; } = 1;
    public string? BackgroundPath { get; set; }
    public ObservableCollection<string> ChangeLog { get; } = new();
    public TbcClientIndex? ClientIndex { get => _clientIndex; private set { _clientIndex = value; OnChanged(); OnChanged(nameof(ClientStatus)); } }
    public string ClientStatus => ClientIndex?.Metadata is { } m ? $"TBC Profile: {m.ProfileName}  |  Interface: Loaded  |  Assets: {m.BlpFiles + m.TgaFiles:N0} indexed  |  Templates: {m.Templates:N0}  |  APIs: {m.ObservedApis:N0}" : "TBC Profile: Not configured or scanned";
    public void Open(string path) { Source = File.ReadAllText(path); _filePath = path;LoadDesignerGeometry();OnChanged(nameof(Title)); Reload(); }
    public async Task OpenAsync(string path){Source=await File.ReadAllTextAsync(path);_filePath=path;LoadDesignerGeometry();OnChanged(nameof(Title));await ReloadAsync();}
    public void Save() { if (_filePath is null) throw new InvalidOperationException("Choose a file name first."); PromoteMappedDesignerGeometry();File.WriteAllText(_filePath, Source); }
    public void SaveAs(string path) { _filePath = path; OnChanged(nameof(Title)); Save(); }
    public void Reload()
    {
        _designerBaseGeometry=new();
        if(TryBuildClientInterfacePreview(out var clientDocument)){Document=clientDocument;ApplyDesignerGeometry();Selected=Document.Root;PreviewChanged?.Invoke(this,EventArgs.Empty);return;}
        var compat=ClientIndex?.Files.Where(x=>x.Kind==InterfaceFileKind.Lua&&Path.GetFileName(x.DiskPath).Equals("Compat.lua",StringComparison.OrdinalIgnoreCase)).Select(x=>x.DiskPath).Take(1).ToArray()??Array.Empty<string>();
        Document=_runtime is MoonSharpPreviewRuntime moon&&compat.Length>0?moon.ExecuteWithCompatibility(Source,PreviewWidth,PreviewHeight,UiScale,ClientIndex,compat,_filePath??"addon.lua"):_runtime.Execute(Source,PreviewWidth,PreviewHeight,UiScale,ClientIndex,_filePath??"addon.lua");
        AutoOpenAioPreview();AddCompanionXmlFrames();ApplyDesignerGeometry();Selected = Document.Root; PreviewChanged?.Invoke(this, EventArgs.Empty);
    }
    public async Task ReloadAsync()
    {
        _designerBaseGeometry=new();
        if(TryBuildClientInterfacePreview(out var clientDocument)){Document=clientDocument;ApplyDesignerGeometry();Selected=Document.Root;PreviewChanged?.Invoke(this,EventArgs.Empty);return;}
        _executionCancellation?.Cancel();_executionCancellation=new CancellationTokenSource();var token=_executionCancellation.Token;var compat=ClientIndex?.Files.Where(x=>x.Kind==InterfaceFileKind.Lua&&Path.GetFileName(x.DiskPath).Equals("Compat.lua",StringComparison.OrdinalIgnoreCase)).Select(x=>x.DiskPath).Take(1).ToArray()??Array.Empty<string>();
        try{Document=_runtime is MoonSharpPreviewRuntime moon&&compat.Length>0?await moon.ExecuteWithCompatibilityAsync(Source,PreviewWidth,PreviewHeight,UiScale,ClientIndex,compat,_filePath??"addon.lua",token):await _runtime.ExecuteAsync(Source,PreviewWidth,PreviewHeight,UiScale,ClientIndex,_filePath??"addon.lua",token);await AutoOpenAioPreviewAsync(token);AddCompanionXmlFrames();ApplyDesignerGeometry();Selected=Document.Root;PreviewChanged?.Invoke(this,EventArgs.Empty);}catch(OperationCanceledException){ }
    }
    private AioHandlerRegistration? PreviewOpenHandler()=>Document.AioHandlers
        .OrderBy(x=>x.Handler.Equals("OpenWindow",StringComparison.OrdinalIgnoreCase)?0:x.Handler.Equals("ShowWindow",StringComparison.OrdinalIgnoreCase)?1:x.Handler.Equals("Open",StringComparison.OrdinalIgnoreCase)?2:3)
        .FirstOrDefault(x=>x.Handler.Equals("OpenWindow",StringComparison.OrdinalIgnoreCase)||x.Handler.Equals("ShowWindow",StringComparison.OrdinalIgnoreCase)||x.Handler.Equals("Open",StringComparison.OrdinalIgnoreCase));
    private bool NeedsAioPreview()=>!Document.ByName.Values.Any(x=>x!=Document.Root&&!x.Name.StartsWith("anonymous#",StringComparison.OrdinalIgnoreCase));
    private void AutoOpenAioPreview(){if(!NeedsAioPreview()||PreviewOpenHandler() is not { } handler)return;using var payload=JsonDocument.Parse("{}");if(_runtime.InvokeAioHandler(Document,handler.Module,handler.Handler,null,payload.RootElement.Clone()))Document.Diagnostics.Add(new(DiagnosticSeverity.Info,"AIO preview",$"Automatically opened {handler.Module}.{handler.Handler} with an empty preview payload."));}
    private async Task AutoOpenAioPreviewAsync(CancellationToken token){if(!NeedsAioPreview()||PreviewOpenHandler() is not { } handler)return;using var payload=JsonDocument.Parse("{}");if(await _runtime.InvokeAioHandlerAsync(Document,handler.Module,handler.Handler,token,null,payload.RootElement.Clone()))Document.Diagnostics.Add(new(DiagnosticSeverity.Info,"AIO preview",$"Automatically opened {handler.Module}.{handler.Handler} with an empty preview payload."));}
    private bool TryBuildClientInterfacePreview(out WowDocument document)
    {
        document=null!;if(ClientIndex is null||_filePath is null)return false;var sourceRecord=ClientIndex.Files.FirstOrDefault(x=>x.Kind==InterfaceFileKind.Lua&&Path.GetFullPath(x.DiskPath).Equals(Path.GetFullPath(_filePath),StringComparison.OrdinalIgnoreCase));if(sourceRecord is null||sourceRecord.Environment is not (ClientEnvironment.InGame or ClientEnvironment.Glue))return false;var stem=Path.GetFileNameWithoutExtension(_filePath);var candidates=ClientIndex.Templates.Where(x=>!x.Virtual&&string.IsNullOrWhiteSpace(x.ParentName)&&Path.GetFileNameWithoutExtension(x.DefinedIn).Equals(stem,StringComparison.OrdinalIgnoreCase)).ToList();if(candidates.Count==0)return false;document=new WowDocument(PreviewWidth,PreviewHeight){UiScale=UiScale,ReadOnlyClientPreview=true};var primary=candidates.FirstOrDefault(x=>x.Name.Equals(stem,StringComparison.OrdinalIgnoreCase))??candidates.OrderByDescending(x=>(x.Width??0)*(x.Height??0)).First();var instantiator=new XmlTemplateInstantiator();WowUiObject previewParent=document.Root;
        if(!string.IsNullOrWhiteSpace(primary.DeclaredParent)&&!primary.DeclaredParent.Equals("UIParent",StringComparison.OrdinalIgnoreCase)){var owner=ClientIndex.Templates.FirstOrDefault(x=>x.Name.Equals(primary.DeclaredParent,StringComparison.OrdinalIgnoreCase)&&!x.Virtual);if(owner is not null){var ownerType=Enum.TryParse<WowObjectType>(owner.Type,true,out var ot)?ot:WowObjectType.Frame;previewParent=new WowUiObject(owner.Name,ownerType,document.Root){Width=owner.Width??384,Height=owner.Height??512};document.ByName[previewParent.Name]=previewParent;instantiator.ApplyTemplate(ClientIndex,owner.Name,previewParent,document);previewParent.Shown=true;}}
        foreach(var template in candidates){if(document.ByName.ContainsKey(template.Name))continue;var type=Enum.TryParse<WowObjectType>(template.Type,true,out var parsed)?parsed:WowObjectType.Frame;var parent=ReferenceEquals(template,primary)?previewParent:document.Root;var frame=new WowUiObject(template.Name,type,parent){Width=template.Width??240,Height=template.Height??80};document.ByName[frame.Name]=frame;instantiator.ApplyTemplate(ClientIndex,template.Name,frame,document);frame.Shown=ReferenceEquals(template,primary);}document.Diagnostics.Add(new(DiagnosticSeverity.Info,"Client Interface preview",$"Read-only XML/template preview for {stem}.lua. Standalone client Lua execution is disabled to prevent crashes; {primary.Name} is the active preview frame."));return true;
    }
    private void AddCompanionXmlFrames()
    {
        if(ClientIndex is null||_filePath is null||!_filePath.EndsWith(".lua",StringComparison.OrdinalIgnoreCase))return;var stem=Path.GetFileNameWithoutExtension(_filePath);var candidates=ClientIndex.Templates.Where(x=>!x.Virtual&&!x.Name.Contains('$')&&string.IsNullOrWhiteSpace(x.ParentName)&&Path.GetFileNameWithoutExtension(x.DefinedIn).Equals(stem,StringComparison.OrdinalIgnoreCase)).ToList();
        foreach(var template in candidates){if(Document.ByName.ContainsKey(template.Name))continue;var model=new WowUiObject(template.Name,Enum.TryParse<WowObjectType>(template.Type,true,out var type)?type:WowObjectType.Frame,Document.Root);Document.ByName[model.Name]=model;new XmlTemplateInstantiator().ApplyTemplate(ClientIndex,template.Name,model,Document);}
        if(candidates.Count>0)Document.Diagnostics.Add(new(DiagnosticSeverity.Info,"Companion XML",$"Loaded {candidates.Count} frame definition(s) from {stem}.xml."));
    }
    public void SetClientIndex(TbcClientIndex index) { ClientIndex = index;LoadClientStrings();if(_filePath is not null)Reload();else PreviewChanged?.Invoke(this, EventArgs.Empty); }
    public string? DisplayText(string? value)=>value is not null&&_clientStrings.TryGetValue(value,out var localized)?localized:value;
    private void LoadClientStrings()
    {
        _clientStrings=new(StringComparer.OrdinalIgnoreCase);if(ClientIndex is null)return;foreach(var file in ClientIndex.Files.Where(x=>x.Kind==InterfaceFileKind.Lua&&Path.GetFileName(x.DiskPath).Equals("GlobalStrings.lua",StringComparison.OrdinalIgnoreCase)))try{foreach(var line in File.ReadLines(file.DiskPath)){var match=Regex.Match(line,"^\\s*(?<key>[A-Z][A-Z0-9_]*)\\s*=\\s*\\\"(?<value>(?:\\\\.|[^\\\"])*)\\\"");if(match.Success)_clientStrings[match.Groups["key"].Value]=Regex.Unescape(match.Groups["value"].Value);}}catch{ }
    }
    public void PreviewTemplate(string name) { if (ClientIndex is null) return; Document = new XmlTemplateInstantiator().Instantiate(ClientIndex, name, PreviewWidth, PreviewHeight); Selected = Document.Root.Children.FirstOrDefault(); PreviewChanged?.Invoke(this, EventArgs.Empty); }
    public void ApplyRuntimeTexture(WowUiObject item, string wowPath) { item.TexturePath = Path.ChangeExtension(wowPath, null); Document.Diagnostics.Add(new(DiagnosticSeverity.Warning, "Unsafe source patch", "Texture applied to preview only; no proven source span was available.")); PreviewChanged?.Invoke(this, EventArgs.Empty); }
    public bool InvokeAioHandler(AioHandlerRegistration handler){var result=_runtime.InvokeAioHandler(Document,handler.Module,handler.Handler);PreviewChanged?.Invoke(this,EventArgs.Empty);return result;}
    public async Task<bool> InvokeAioHandlerAsync(AioHandlerRegistration handler,params object?[] arguments){_executionCancellation?.Cancel();_executionCancellation=new CancellationTokenSource();var result=await _runtime.InvokeAioHandlerAsync(Document,handler.Module,handler.Handler,_executionCancellation.Token,arguments);ApplyDesignerGeometry();PreviewChanged?.Invoke(this,EventArgs.Empty);return result;}
    public async Task<bool> InvokeFrameScriptAsync(WowUiObject item,string scriptName,params object?[] arguments){_executionCancellation?.Cancel();_executionCancellation=new CancellationTokenSource();var result=await _runtime.InvokeFrameScriptAsync(Document,item,scriptName,_executionCancellation.Token,arguments);ApplyDesignerGeometry();PreviewChanged?.Invoke(this,EventArgs.Empty);return result;}
    public void SaveDesignerGeometry(WowUiObject item,double? width,double? height,double? x,double? y)
    {
        var key=ObjectPath(item);var before=$"{item.Width:0.###}×{item.Height:0.###} @ {item.Anchor.X:0.###},{item.Anchor.Y:0.###}";var geometry=new DesignerGeometry(width??item.Width,height??item.Height,x??item.Anchor.X,y??item.Anchor.Y);_designerGeometry[key]=geometry;Apply(item,geometry);if(_filePath is not null)File.WriteAllText(_filePath+".mwstudio.layout.json",JsonSerializer.Serialize(_designerGeometry,new JsonSerializerOptions{WriteIndented=true}));ChangeLog.Insert(0,$"{DateTime.Now:HH:mm:ss}  {item.Name}: {before} → {geometry.Width:0.###}×{geometry.Height:0.###} @ {geometry.X:0.###},{geometry.Y:0.###} (Studio layout override)");PreviewChanged?.Invoke(this,EventArgs.Empty);
    }
    private void LoadDesignerGeometry(){_designerGeometry=new();if(_filePath is null)return;var path=_filePath+".mwstudio.layout.json";if(!File.Exists(path))return;try{_designerGeometry=JsonSerializer.Deserialize<Dictionary<string,DesignerGeometry>>(File.ReadAllText(path))??new();}catch{Document.Diagnostics.Add(new(DiagnosticSeverity.Warning,"Designer layout","Could not read "+path));}}
    public bool DiscardDesignerGeometry()
    {
        var path=DesignerLayoutPath;
        if(path is null)return false;
        _designerGeometry.Clear();
        if(File.Exists(path))File.Delete(path);
        ChangeLog.Insert(0,$"{DateTime.Now:HH:mm:ss}  Discarded Studio-only layout overrides.");
        Reload();
        return true;
    }
    private void ApplyDesignerGeometry(){foreach(var item in Document.ByName.Values){var key=ObjectPath(item);if(_designerGeometry.TryGetValue(key,out var geometry)){if(!_designerBaseGeometry.ContainsKey(key))_designerBaseGeometry[key]=new(item.Width,item.Height,item.Anchor.X,item.Anchor.Y);Apply(item,geometry);}}}
    private void PromoteMappedDesignerGeometry()
    {
        if(_filePath is null||_designerGeometry.Count==0)return;
        var patches=new List<(string Key,SourceSpan Span,double Value)>();var completed=new List<string>();
        foreach(var item in Document.ByName.Values)
        {
            var key=ObjectPath(item);if(!_designerGeometry.TryGetValue(key,out var desired)||!_designerBaseGeometry.TryGetValue(key,out var basis))continue;
            var differences=new[]{("Width",desired.Width,basis.Width),("Height",desired.Height,basis.Height),("X",desired.X,basis.X),("Y",desired.Y,basis.Y)}.Where(x=>Math.Abs(x.Item2-x.Item3)>.001).ToList();
            if(differences.Count==0){completed.Add(key);continue;}
            if(differences.All(x=>item.Sources.TryGetValue(x.Item1,out var span)&&span.IsLiteral))
            {foreach(var difference in differences)patches.Add((key,item.Sources[difference.Item1],difference.Item2));completed.Add(key);}
        }
        var conflicts=patches.GroupBy(x=>x.Span.ValueStart).Where(g=>g.Select(x=>x.Value).Distinct().Count()>1).Select(g=>g.Key).ToHashSet();
        var conflictedKeys=patches.Where(x=>conflicts.Contains(x.Span.ValueStart)).Select(x=>x.Key).ToHashSet();completed.RemoveAll(conflictedKeys.Contains);
        patches=patches.Where(x=>!conflicts.Contains(x.Span.ValueStart)).GroupBy(x=>x.Span.ValueStart).Select(g=>g.First()).ToList();
        if(patches.Count>0){_undo.Push(Source);_redo.Clear();var mapper=new LuaSourceMapper();foreach(var patch in patches.OrderByDescending(x=>x.Span.ValueStart))Source=mapper.PatchLiteral(Source,patch.Span,patch.Value);ChangeLog.Insert(0,$"{DateTime.Now:HH:mm:ss}  Promoted {patches.Count} stored preview value(s) into Lua.");}
        foreach(var key in completed)_designerGeometry.Remove(key);
        var layout=_filePath+".mwstudio.layout.json";if(_designerGeometry.Count==0){if(File.Exists(layout))File.Delete(layout);}else File.WriteAllText(layout,JsonSerializer.Serialize(_designerGeometry,new JsonSerializerOptions{WriteIndented=true}));
        OnChanged(nameof(PendingDesignerOverrideCount));
    }
    private static void Apply(WowUiObject item,DesignerGeometry geometry){item.Width=geometry.Width;item.Height=geometry.Height;var anchor=item.Anchor;item.Anchor=anchor with{X=geometry.X,Y=geometry.Y};}
    private static string ObjectPath(WowUiObject item){var parts=new Stack<int>();for(var current=item;current.Parent is not null;current=current.Parent)parts.Push(current.Parent.Children.IndexOf(current));return string.Join("/",parts);}
    public sealed record DesignerGeometry(double Width,double Height,double X,double Y);
    public bool PatchGeometry(WowUiObject item, double? width = null, double? height = null, double? x = null, double? y = null)
    {
        var changes = new List<(SourceSpan Span, double Value)>(); Add("Width", width); Add("Height", height); Add("X", x); Add("Y", y);
        if (changes.Count == 0) { Document.Diagnostics.Add(new(DiagnosticSeverity.Warning, "Unsafe source patch", "No safely mapped numeric literals exist for this change.")); return false; }
        _undo.Push(Source); _redo.Clear();
        try { var before=Source;var key=ObjectPath(item);var mapper = new LuaSourceMapper(); foreach (var c in changes.OrderByDescending(c => c.Span.ValueStart)) Source = mapper.PatchLiteral(Source, c.Span, c.Value);if(_designerGeometry.Remove(key)&&_filePath is not null){var layout=_filePath+".mwstudio.layout.json";if(_designerGeometry.Count==0){if(File.Exists(layout))File.Delete(layout);}else File.WriteAllText(layout,JsonSerializer.Serialize(_designerGeometry,new JsonSerializerOptions{WriteIndented=true}));OnChanged(nameof(PendingDesignerOverrideCount));}if(_filePath is not null){var backup=_filePath+".mwstudio.bak";if(!File.Exists(backup))File.Copy(_filePath,backup);File.WriteAllText(_filePath,Source);}ChangeLog.Insert(0,$"{DateTime.Now:HH:mm:ss}  {item.Name}: "+string.Join(", ",changes.OrderBy(c=>c.Span.ValueStart).Select(c=>$"{c.Span.Property} {c.Span.OriginalText} → {c.Value:0.###}")));Reload(); return true; }
        catch (InvalidOperationException ex) { Document.Diagnostics.Add(new(DiagnosticSeverity.Warning, "Unsafe source patch", ex.Message)); return false; }
        void Add(string key, double? value) { if (value.HasValue && item.Sources.TryGetValue(key, out var span)) changes.Add((span, value.Value)); }
    }
    public void Undo() { if (_undo.Count == 0) return; _redo.Push(Source); Source = _undo.Pop(); Reload(); }
    public void Redo() { if (_redo.Count == 0) return; _undo.Push(Source); Source = _redo.Pop(); Reload(); }
    private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    private const string SampleSource = @"-- Open a TBC client Lua file, or edit this sample.
local frame = CreateFrame(""Frame"", ""SampleFrame"", UIParent)
frame:SetSize(420, 240)
frame:SetPoint(""CENTER"", UIParent, ""CENTER"", 0, 0)
frame:SetBackdrop({ bgFile = ""Interface\DialogFrame\UI-DialogBox-Background"", edgeFile = ""Interface\Tooltips\UI-Tooltip-Border"" })
local title = frame:CreateFontString(nil, ""OVERLAY"")
title:SetPoint(""TOP"", frame, ""TOP"", 0, -20)
title:SetText(""MasterWoW UI Studio"")
local button = CreateFrame(""Button"", ""SampleButton"", frame)
button:SetSize(160, 34)
button:SetPoint(""BOTTOM"", frame, ""BOTTOM"", 0, 24)
button:SetText(""TBC Preview"")
";
}
