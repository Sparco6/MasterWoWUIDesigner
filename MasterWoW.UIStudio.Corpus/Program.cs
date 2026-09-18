using MasterWoW.UIStudio.Core;
if(args.Length>1&&args[0]=="--probe")
{
    var target=Path.GetFullPath(args[1]);var source=await File.ReadAllTextAsync(target);var runtime=new MoonSharpPreviewRuntime();var document=runtime.Execute(source,1280,720);
    var open=document.AioHandlers.FirstOrDefault(x=>x.Handler.Contains("Open",StringComparison.OrdinalIgnoreCase))??document.AioHandlers.FirstOrDefault(x=>x.Handler.Contains("Show",StringComparison.OrdinalIgnoreCase))??document.AioHandlers.FirstOrDefault();if(open is not null){using var payload=System.Text.Json.JsonDocument.Parse("{}");runtime.InvokeAioHandler(document,open.Module,open.Handler,null,payload.RootElement.Clone());}
    Console.WriteLine($"target={target}; frames={document.ByName.Count-1}; aioHandlers={document.AioHandlers.Count}; invoked={open?.Module}.{open?.Handler}; errors={document.Diagnostics.Count(x=>x.Severity==DiagnosticSeverity.Error)}");
    foreach(var diagnostic in document.Diagnostics.Take(10))Console.WriteLine($"{diagnostic.Severity}|{diagnostic.Category}|{diagnostic.Message.Replace(Environment.NewLine," ")}");
    return;
}
var workspace=Path.GetFullPath(args.Length>0?args[0]:Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
var studio=Path.Combine(workspace,"MasterWoW.UIStudio"); var scanner=new AddonCorpusScanner(); var roots=scanner.DiscoverWorkspaceSources(workspace).ToList();
foreach(var extra in new[]{Path.Combine(workspace,"TBCClientEdit","Interface","FrameXML"),Path.Combine(workspace,"TBCClientEdit","Interface","GlueXML"),Path.Combine(workspace,"InterfaceClient")})if(Directory.Exists(extra))roots.Add(extra);
var index=await scanner.ScanAsync(roots.Distinct(StringComparer.OrdinalIgnoreCase)); var writer=new CorpusReportWriter();
writer.WriteCoverage(index,Path.Combine(studio,"docs","REAL_ADDON_API_COVERAGE.md"));
var implemented=new HashSet<string>(StringComparer.OrdinalIgnoreCase){"CreateFrame","SetPoint","ClearAllPoints","SetAllPoints","GetNumPoints","GetLeft","GetRight","GetTop","GetBottom","SetWidth","GetWidth","SetHeight","GetHeight","SetSize","Show","Hide","IsShown","IsVisible","SetAlpha","GetAlpha","SetText","GetText","SetTexture","GetTexture","CreateTexture","CreateFontString","SetBackdrop","SetScript","HookScript","SetMinMaxValues","SetValue","GetValue","SetStatusBarTexture","SetStatusBarColor","AddAddon","AddHandlers","Handle","GetScreenWidth","GetScreenHeight"};
writer.WriteBacklog(index,Path.Combine(studio,"docs","EMULATOR_BACKLOG.md"),implemented);
var luaCount=index.Files.Count(x=>x.Path.EndsWith(".lua",StringComparison.OrdinalIgnoreCase)); var xmlCount=index.Files.Count(x=>x.Path.EndsWith(".xml",StringComparison.OrdinalIgnoreCase));
Console.WriteLine($"roots={index.ScannedRoots.Count}; files={index.Files.Count}; lua={luaCount}; xml={xmlCount}; handlers={index.AioHandlers.Count}; compat={index.CompatibilityApis.Count}");
