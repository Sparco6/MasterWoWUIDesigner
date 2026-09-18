using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using System.Diagnostics;
using System.Text.Json;

namespace MasterWoW.UIStudio.Core;

public enum RuntimeExecutionContext { InitialLoad, AioHandler, EventHandler, FrameScript, CompatLayer, FrameXmlScript }
public sealed class RuntimeExecutionLimitException : TimeoutException
{
    public RuntimeExecutionLimitException(RuntimeExecutionContext context,string sourceFile,TimeSpan elapsed,TimeSpan limit):base($"Runtime execution limit exceeded. Context: {context}; Elapsed: {elapsed.TotalSeconds:0.00}s; Limit: {limit.TotalSeconds:0.00}s; File: {sourceFile}"){Context=context;SourceFile=sourceFile;Elapsed=elapsed;Limit=limit;}
    public RuntimeExecutionContext Context{get;} public string SourceFile{get;} public TimeSpan Elapsed{get;} public TimeSpan Limit{get;}
}
public interface ILuaPreviewRuntime
{
    WowDocument Execute(string source,double width,double height,double uiScale=1,TbcClientIndex? clientIndex=null,string sourceFile="addon.lua");
    Task<WowDocument> ExecuteAsync(string source,double width,double height,double uiScale=1,TbcClientIndex? clientIndex=null,string sourceFile="addon.lua",CancellationToken cancellationToken=default);
    bool InvokeAioHandler(WowDocument document,string module,string handler,params object?[] arguments);
    Task<bool> InvokeAioHandlerAsync(WowDocument document,string module,string handler,CancellationToken cancellationToken=default,params object?[] arguments);
    Task<bool> InvokeFrameScriptAsync(WowDocument document,WowUiObject item,string scriptName,CancellationToken cancellationToken=default,params object?[] arguments);
}

public sealed class MoonSharpPreviewRuntime : ILuaPreviewRuntime
{
    private int _anonymous;
    private Script? _activeScript;
    private string? _activeSource;
    private List<(WowUiObject Item,string Expression)>? _activeSourceTags;
    private readonly Dictionary<string,DynValue> _aioCallbacks=new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _executionGate=new(1,1);
    private static readonly object RegistrationLock=new();
    private static bool _registered;

    public WowDocument Execute(string source,double width,double height,double uiScale=1,TbcClientIndex? clientIndex=null,string sourceFile="addon.lua")
        => ExecuteWithCompatibility(source,width,height,uiScale,clientIndex,Array.Empty<string>(),sourceFile);
    public async Task<WowDocument> ExecuteAsync(string source,double width,double height,double uiScale=1,TbcClientIndex? clientIndex=null,string sourceFile="addon.lua",CancellationToken cancellationToken=default)
    {await _executionGate.WaitAsync(cancellationToken);try{return await Task.Run(()=>ExecuteWithCompatibility(source,width,height,uiScale,clientIndex,Array.Empty<string>(),sourceFile,cancellationToken),cancellationToken);}finally{_executionGate.Release();}}

    public WowDocument ExecuteWithCompatibility(string source,double width,double height,double uiScale,TbcClientIndex? clientIndex,IEnumerable<string> compatibilityFiles,string sourceFile="addon.lua",CancellationToken cancellationToken=default)
    {
        var document = new WowDocument(width, height) { UiScale = uiScale };
        EnsureUserDataRegistered();
        var script = new Script(CoreModules.Preset_SoftSandbox);
        _activeScript=script; _activeSource=source; _aioCallbacks.Clear();
        var sourceMapper=new LuaSourceMapper();
        var sourceTags=new List<(WowUiObject Item,string Expression)>();
        _activeSourceTags=sourceTags;
        script.Globals["__mw_source_map"]=DynValue.NewCallback((_,args)=>
        {
            if(args.Count>=2&&args[0].Type==DataType.UserData&&args[0].UserData.Object is LuaUiObject item&&args[1].Type==DataType.String)
                sourceTags.Add((item.Model,args[1].String));
            return args.Count>0?args[0]:DynValue.Nil;
        });
        var math=script.Globals.Get("math").Table;
        math["fmod"]=DynValue.NewCallback((_,args)=>{var left=args.Count>0?args[0].CastToNumber()??0:0;var right=args.Count>1?args[1].CastToNumber()??0:0;if(right==0)return DynValue.NewNumber(double.NaN);return DynValue.NewNumber(left-Math.Truncate(left/right)*right);});
        var uiParent = new LuaUiObject(document.Root, document);
        script.Globals["UIParent"] = uiParent;
        script.Globals["WorldFrame"] = uiParent;
        script.Globals["UISpecialFrames"] = new Table(script);
        script.Globals["StaticPopupDialogs"] = new Table(script);
        script.Globals["GetScreenWidth"] = (Func<double>)(() => width);
        script.Globals["GetScreenHeight"] = (Func<double>)(() => height);
        script.Globals["GetItemInfo"] = DynValue.NewCallback((_,args)=>{var id=args.Count>0?(int)(args[0].CastToNumber()??0):0;var name=$"Item {id}";return DynValue.NewTuple(DynValue.NewString(name),DynValue.NewString($"|cffffffff|Hitem:{id}:0:0:0:0:0:0:0|h[{name}]|h|r"),DynValue.NewNumber(1),DynValue.NewNumber(1),DynValue.NewNumber(1),DynValue.NewString("Miscellaneous"),DynValue.NewString(""),DynValue.NewNumber(1),DynValue.NewString(""),DynValue.NewString("Interface\\Icons\\INV_Misc_QuestionMark"));});
        script.Globals["GetItemIcon"] = (Func<double,string>)(_=>"Interface\\Icons\\INV_Misc_QuestionMark");
        script.Globals["time"] = (Func<double>)(()=>DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var slashCommands=new Table(script);slashCommands.MetaTable=new Table(script);slashCommands.MetaTable["__newindex"]=DynValue.NewCallback((_,args)=>{if(args.Count>=3){args[0].Table.Set(args[1],args[2]);if(args[1].Type==DataType.String)document.SlashCommands.Add(args[1].String);}return DynValue.Nil;});script.Globals["SlashCmdList"]=slashCommands;
        script.Globals["CreateFont"] = (Func<string,LuaUiObject>)(name =>
        {
            if(document.ByName.TryGetValue(name,out var existing))return new(existing,document);
            var model=new WowUiObject(name,WowObjectType.Font);document.ByName[name]=model;var proxy=new LuaUiObject(model,document);script.Globals[name]=proxy;return proxy;
        });
        SeedFont("GameFontNormal", "Fonts\\FRIZQT__.TTF", 12, (1, .82, 0, 1));
        SeedFont("GameFontNormalSmall", "Fonts\\FRIZQT__.TTF", 10, (1, .82, 0, 1));
        SeedFont("GameFontNormalLarge", "Fonts\\FRIZQT__.TTF", 16, (1, .82, 0, 1));
        SeedFont("GameFontHighlight", "Fonts\\FRIZQT__.TTF", 12, (1, 1, 1, 1));
        SeedFont("GameFontHighlightSmall", "Fonts\\FRIZQT__.TTF", 10, (1, 1, 1, 1));
        SeedFont("ChatFontNormal", "Fonts\\ARIALN.TTF", 14, (1, 1, 1, 1));
        void SeedFont(string name,string path,double size,(double R,double G,double B,double A) color)
        {
            var model=new WowUiObject(name,WowObjectType.Font){FontPath=path,FontSize=size,TextColor=color};
            document.ByName[name]=model;script.Globals[name]=UserData.Create(new LuaUiObject(model,document));
        }
        script.Globals["CreateFrame"] = (Func<DynValue, DynValue, DynValue, DynValue, LuaUiObject>)((type, name, parent, template) =>
        {
            var typeName = type.CastToString() ?? "Frame";
            if (!Enum.TryParse<WowObjectType>(typeName, true, out var objectType) || objectType is WowObjectType.UIParent or WowObjectType.Texture or WowObjectType.FontString)
            {
                document.Diagnostics.Add(new(DiagnosticSeverity.Warning, "Unsupported Frame Type", $"{typeName} is rendered as Frame."));
                objectType = WowObjectType.Frame;
            }
            var parentObject = parent.Type == DataType.UserData && parent.UserData.Object is LuaUiObject p ? p.Model : document.Root;
            var objectName = name.Type == DataType.String ? name.String : $"anonymous#{++_anonymous}";
            var frameTemplate=template.Type==DataType.String?template.String:null;var model = new WowUiObject(objectName, objectType, parentObject){TemplateName=frameTemplate};
            if(frameTemplate?.Contains("UIPanelCloseButton",StringComparison.OrdinalIgnoreCase)==true){model.Width=32;model.Height=32;model.TexturePath="Interface\\Buttons\\UI-Panel-MinimizeButton-Up";}
            document.ByName[objectName] = model;
            if (!objectName.StartsWith("anonymous#", StringComparison.Ordinal)) script.Globals[objectName] = UserData.Create(new LuaUiObject(model, document));
            if(template.Type==DataType.String&&clientIndex is not null)
            {
                foreach(var templateName in template.String.Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries))
                    if(!new XmlTemplateInstantiator().ApplyTemplate(clientIndex,templateName,model,document))document.Diagnostics.Add(new(DiagnosticSeverity.Warning,"Missing template",templateName));
            }
            document.ApiCalls.Add($"CreateFrame(\"{typeName}\", \"{objectName}\", {parentObject.Name})");
            return new LuaUiObject(model, document);
        });
        AddAioMock(script, document);
        AddFrameXmlMocks(script,document);

        foreach(var compatPath in compatibilityFiles)
        {
            try{ExecuteBounded(script,script.LoadString(File.ReadAllText(compatPath),codeFriendlyName:compatPath),Array.Empty<DynValue>(),compatPath,RuntimeExecutionContext.CompatLayer,TimeSpan.FromSeconds(2),cancellationToken);document.CompatibilityLayers.Add(new(compatPath,true,"CLR compatibility adapter",null));document.Diagnostics.Add(new(DiagnosticSeverity.Info,"Compatibility layer",$"Loaded {Path.GetFileName(compatPath)}; CLR UI userdata uses equivalent adapter methods instead of mutable native prototypes."));}
            catch(RuntimeExecutionLimitException ex){document.CompatibilityLayers.Add(new(compatPath,false,"CLR compatibility adapter",ex.Message));document.Diagnostics.Add(new(DiagnosticSeverity.Error,"Runtime timeout",ex.Message));return document;}
            catch(InterpreterException ex){document.CompatibilityLayers.Add(new(compatPath,false,"CLR compatibility adapter",ex.DecoratedMessage??ex.Message));document.Diagnostics.Add(new(DiagnosticSeverity.Warning,"Compatibility layer",$"{Path.GetFileName(compatPath)} partially loaded: {ex.DecoratedMessage??ex.Message}. Definitions executed before the failure remain active; adapter methods remain available."));}
        }
        try
        {
            var executableSource=LuaSourceInstrumentation.Instrument(source);
            ExecuteBounded(script,script.LoadString(executableSource,codeFriendlyName:sourceFile),Array.Empty<DynValue>(),sourceFile,RuntimeExecutionContext.InitialLoad,TimeSpan.FromSeconds(2),cancellationToken);
            sourceMapper.Map(source, document);
            sourceMapper.MapExpressions(source,sourceTags);
            DispatchLifecycle(script,document,"ADDON_LOADED",Path.GetFileNameWithoutExtension(sourceFile),sourceFile,cancellationToken);
            DispatchLifecycle(script,document,"PLAYER_LOGIN",null,sourceFile,cancellationToken);
        }
        catch(RuntimeExecutionLimitException ex)
        {
            document.Diagnostics.Add(new(DiagnosticSeverity.Error,"Runtime timeout",ex.Message));
        }
        catch(OperationCanceledException){throw;}
        catch (InterpreterException ex)
        {
            document.Diagnostics.Add(new(DiagnosticSeverity.Error, "Lua error", ex.DecoratedMessage ?? ex.Message));
        }
        catch (Exception ex)
        {
            document.Diagnostics.Add(new(DiagnosticSeverity.Error, "Runtime", ex.Message));
        }
        return document;
    }
    private static void DispatchLifecycle(Script script,WowDocument document,string eventName,string? argument,string sourceFile,CancellationToken cancellationToken)
    {
        foreach(var item in document.ByName.Values.Distinct().Where(x=>x.Events.Contains(eventName)&&x.ScriptCallbacks.TryGetValue("OnEvent",out var callback)&&callback is DynValue).ToList())
        {
            var callback=(DynValue)item.ScriptCallbacks["OnEvent"];var args=new List<DynValue>{UserData.Create(new LuaUiObject(item,document)),DynValue.NewString(eventName)};if(argument is not null)args.Add(DynValue.NewString(argument));
            try{ExecuteBounded(script,callback,args.ToArray(),$"{sourceFile}:{eventName}",RuntimeExecutionContext.EventHandler,TimeSpan.FromSeconds(2),cancellationToken);document.ApiCalls.Add($"Event({eventName}) -> {item.Name}");}
            catch(RuntimeExecutionLimitException ex){document.Diagnostics.Add(new(DiagnosticSeverity.Error,"Runtime timeout",ex.Message));}
            catch(InterpreterException ex){document.Diagnostics.Add(new(DiagnosticSeverity.Error,"Event handler",ex.DecoratedMessage??ex.Message));}
        }
    }
    public async Task<WowDocument> ExecuteWithCompatibilityAsync(string source,double width,double height,double uiScale,TbcClientIndex? clientIndex,IEnumerable<string> compatibilityFiles,string sourceFile="addon.lua",CancellationToken cancellationToken=default)
    {await _executionGate.WaitAsync(cancellationToken);try{return await Task.Run(()=>ExecuteWithCompatibility(source,width,height,uiScale,clientIndex,compatibilityFiles,sourceFile,cancellationToken),cancellationToken);}finally{_executionGate.Release();}}

    private static DynValue ExecuteBounded(Script script,DynValue function,DynValue[] arguments,string sourceFile,RuntimeExecutionContext context,TimeSpan limit,CancellationToken cancellationToken)
    {
        var coroutine=script.CreateCoroutine(function).Coroutine;coroutine.AutoYieldCounter=10_000;var clock=Stopwatch.StartNew();var first=true;var result=DynValue.Nil;
        while(coroutine.State!=CoroutineState.Dead){cancellationToken.ThrowIfCancellationRequested();result=first?coroutine.Resume(arguments):coroutine.Resume();first=false;if(clock.Elapsed>limit)throw new RuntimeExecutionLimitException(context,sourceFile,clock.Elapsed,limit);}
        return result;
    }

    private static void EnsureUserDataRegistered()
    {
        if(_registered)return;lock(RegistrationLock){if(_registered)return;var reflected=UserData.RegisterType<LuaUiObject>();UserData.UnregisterType<LuaUiObject>();UserData.RegisterType<LuaUiObject>(new LuaUiObjectDescriptor(reflected));_registered=true;}
    }

    public bool InvokeAioHandler(WowDocument document,string module,string handler,params object?[] arguments)
        =>InvokeAioHandlerCore(document,module,handler,CancellationToken.None,arguments);
    public async Task<bool> InvokeAioHandlerAsync(WowDocument document,string module,string handler,CancellationToken cancellationToken=default,params object?[] arguments)
    {await _executionGate.WaitAsync(cancellationToken);try{return await Task.Run(()=>InvokeAioHandlerCore(document,module,handler,cancellationToken,arguments),cancellationToken);}finally{_executionGate.Release();}}
    private bool InvokeAioHandlerCore(WowDocument document,string module,string handler,CancellationToken cancellationToken,object?[] arguments)
    {
        if(_activeScript is null||!_aioCallbacks.TryGetValue(module+"."+handler,out var callback))return false;
        try{var values=arguments.Select(x=>ToDynValue(_activeScript,x)).ToArray();ExecuteBounded(_activeScript,callback,values,$"AIO:{module}.{handler}",RuntimeExecutionContext.AioHandler,TimeSpan.FromSeconds(2),cancellationToken);if(_activeSource is not null){var mapper=new LuaSourceMapper();mapper.Map(_activeSource,document);if(_activeSourceTags is not null)mapper.MapExpressions(_activeSource,_activeSourceTags);}document.ApiCalls.Add($"AIO.Invoke({module}.{handler})");return true;}
        catch(RuntimeExecutionLimitException ex){document.Diagnostics.Add(new(DiagnosticSeverity.Error,"Runtime timeout",ex.Message));return false;}
        catch(OperationCanceledException){document.Diagnostics.Add(new(DiagnosticSeverity.Warning,"Runtime canceled",$"Execution canceled. Context: {RuntimeExecutionContext.AioHandler}; File: AIO:{module}.{handler}"));return false;}
        catch(InterpreterException ex){document.Diagnostics.Add(new(DiagnosticSeverity.Error,"AIO handler",ex.DecoratedMessage??ex.Message));return false;}
        catch(Exception ex){document.Diagnostics.Add(new(DiagnosticSeverity.Error,"AIO handler",ex.Message));return false;}
    }
    public async Task<bool> InvokeFrameScriptAsync(WowDocument document,WowUiObject item,string scriptName,CancellationToken cancellationToken=default,params object?[] arguments)
    {await _executionGate.WaitAsync(cancellationToken);try{return await Task.Run(()=>InvokeFrameScriptCore(document,item,scriptName,cancellationToken,arguments),cancellationToken);}finally{_executionGate.Release();}}
    private bool InvokeFrameScriptCore(WowDocument document,WowUiObject item,string scriptName,CancellationToken cancellationToken,object?[] arguments)
    {
        if(_activeScript is null||!item.ScriptCallbacks.TryGetValue(scriptName,out var stored)||stored is not DynValue callback)return false;
        try{var values=new[]{UserData.Create(new LuaUiObject(item,document))}.Concat(arguments.Select(x=>ToDynValue(_activeScript,x))).ToArray();ExecuteBounded(_activeScript,callback,values,$"{item.Name}:{scriptName}",RuntimeExecutionContext.FrameScript,TimeSpan.FromSeconds(2),cancellationToken);document.ApiCalls.Add($"{item.Name}:{scriptName}()");return true;}
        catch(RuntimeExecutionLimitException ex){document.Diagnostics.Add(new(DiagnosticSeverity.Error,"Runtime timeout",ex.Message));return false;}
        catch(OperationCanceledException){document.Diagnostics.Add(new(DiagnosticSeverity.Warning,"Runtime canceled",$"Execution canceled. Context: {RuntimeExecutionContext.FrameScript}; File: {item.Name}:{scriptName}"));return false;}
        catch(InterpreterException ex){document.Diagnostics.Add(new(DiagnosticSeverity.Error,"Frame script",ex.DecoratedMessage??ex.Message));return false;}
        catch(Exception ex){document.Diagnostics.Add(new(DiagnosticSeverity.Error,"Frame script",ex.Message));return false;}
    }
    private static DynValue ToDynValue(Script script,object? value)
    {
        if(value is not JsonElement json)return DynValue.FromObject(script,value);
        return json.ValueKind switch
        {
            JsonValueKind.Object=>ObjectTable(json),JsonValueKind.Array=>ArrayTable(json),JsonValueKind.String=>DynValue.NewString(json.GetString()??string.Empty),
            JsonValueKind.Number=>DynValue.NewNumber(json.GetDouble()),JsonValueKind.True=>DynValue.True,JsonValueKind.False=>DynValue.False,_=>DynValue.Nil
        };
        DynValue ObjectTable(JsonElement element){var table=new Table(script);foreach(var property in element.EnumerateObject())table[property.Name]=ToDynValue(script,property.Value);return DynValue.NewTable(table);}
        DynValue ArrayTable(JsonElement element){var table=new Table(script);var index=1;foreach(var item in element.EnumerateArray())table[index++]=ToDynValue(script,item);return DynValue.NewTable(table);}
    }

    private void AddAioMock(Script script, WowDocument document)
    {
        var aio = new Table(script);
        aio["AddAddon"] = DynValue.NewCallback((_, _) => DynValue.NewBoolean(false));
        aio["AddHandlers"] = DynValue.NewCallback((_, args) =>
        {
            var module=args.Count>0 ? args[0].CastToString() ?? args[0].ToString() : "Unknown";
            var handlers=args.Count>1 && args[1].Type==DataType.Table ? args[1] : DynValue.NewTable(script);
            foreach(var pair in handlers.Table.Pairs)
                if(pair.Key.Type==DataType.String&&pair.Value.Type is DataType.Function or DataType.ClrFunction)
                {
                    if(!document.AioHandlers.Any(x=>x.Module.Equals(module,StringComparison.OrdinalIgnoreCase)&&x.Handler.Equals(pair.Key.String,StringComparison.OrdinalIgnoreCase)))document.AioHandlers.Add(new(module,pair.Key.String));
                    _aioCallbacks[module+"."+pair.Key.String]=pair.Value;
                }
            handlers.Table.MetaTable = handlers.Table.MetaTable ?? new Table(script);
            handlers.Table.MetaTable["__newindex"] = DynValue.NewCallback((_, set) =>
            {
                if(set.Count>=3)
                {
                    set[0].Table.Set(set[1],set[2]);
                    if(set[1].Type==DataType.String){document.AioHandlers.Add(new(module,set[1].String));_aioCallbacks[module+"."+set[1].String]=set[2];}
                }
                return DynValue.Nil;
            });
            return handlers;
        });
        aio["Handle"] = DynValue.NewCallback((_, args) => {var module=args.Count>0?args[0].CastToString()??"Unknown":"Unknown";var handler=args.Count>1?args[1].CastToString()??"Unknown":"Unknown";var values=new List<object?>();for(var index=2;index<args.Count;index++)values.Add(PlainValue(args[index]));document.AioRequests.Add(new(module,handler,values));document.ApiCalls.Add($"AIO.Handle({module}, {handler})"); return DynValue.Nil; });
        script.Globals["AIO"] = aio;
        static object? PlainValue(DynValue value)=>value.Type switch{DataType.String=>value.String,DataType.Number=>value.Number,DataType.Boolean=>value.Boolean,DataType.Nil or DataType.Void=>null,_=>value.ToString()};
    }

    private static void AddFrameXmlMocks(Script script,WowDocument document)
    {
        script.Globals["UIDropDownMenu_SetWidth"]=DynValue.NewCallback((_,args)=>{if(args.Count>=2&&args[1].Type==DataType.UserData&&args[1].UserData.Object is LuaUiObject frame){frame.SetWidth(args[0].CastToNumber()??150);document.ApiCalls.Add("UIDropDownMenu_SetWidth(mock)");}return DynValue.Nil;});
        script.Globals["UIDropDownMenu_SetText"]=DynValue.NewCallback((_,args)=>{if(args.Count>=2&&args[1].Type==DataType.UserData&&args[1].UserData.Object is LuaUiObject frame){frame.SetText(args[0].CastToString()??string.Empty);document.ApiCalls.Add("UIDropDownMenu_SetText(mock)");}return DynValue.Nil;});
        script.Globals["UIDropDownMenu_Initialize"]=DynValue.NewCallback((_,_)=>{document.ApiCalls.Add("UIDropDownMenu_Initialize(mock)");return DynValue.Nil;});
        script.Globals["UIDropDownMenu_AddButton"]=DynValue.NewCallback((_,_)=>DynValue.Nil);
    }
}

[MoonSharpUserData]
public sealed class LuaUiObject
{
    private readonly WowDocument _document;
    public LuaUiObject(WowUiObject model, WowDocument document) { Model = model; _document = document; }
    public WowUiObject Model { get; }
    public string GetName()=>Model.Name;
    internal bool TryGetDynamic(string key,out DynValue value){if(Model.RuntimeFields.TryGetValue(key,out var stored)&&stored is DynValue dyn){value=dyn;return true;}value=DynValue.Nil;return false;}
    internal void SetDynamic(string key,DynValue value)=>Model.RuntimeFields[key]=value;
    private void Log(string call) => _document.ApiCalls.Add($"{Model.Name}:{call}");

    public void SetWidth(double value) { Model.Width = Math.Max(0, value); Model.RuntimeFields["ExplicitWidth"]=true;Log($"SetWidth({value})"); }
    public void SetParent(LuaUiObject? parent){if(parent is null)return;Model.SetParent(parent.Model);Log($"SetParent({parent.Model.Name})");}
    public LuaUiObject? GetParent()=>Model.Parent is null?null:new LuaUiObject(Model.Parent,_document);
    public DynValue GetChildren()=>DynValue.NewTuple(Model.Children.Select(x=>UserData.Create(new LuaUiObject(x,_document))).ToArray());
    public int GetNumChildren()=>Model.Children.Count;
    public double GetWidth() => Model.Width;
    public void SetHeight(double value) { Model.Height = Math.Max(0, value);Model.RuntimeFields["ExplicitHeight"]=true; Log($"SetHeight({value})"); }
    public double GetHeight() => Model.Height;
    public void SetSize(double width, double height) { Model.Width = Math.Max(0, width); Model.Height = Math.Max(0, height);Model.RuntimeFields["ExplicitWidth"]=true;Model.RuntimeFields["ExplicitHeight"]=true; Log($"SetSize({width}, {height})"); }
    public void SetPoint(string point, params DynValue[] args)
    {
        var rel=Model.Parent?.Name??"UIParent"; var relPoint=point; double x=0,y=0;
        if(args.Length==2 && args[0].Type==DataType.Number) { x=args[0].Number; y=args[1].CastToNumber() ?? 0; }
        else if(args.Length>0)
        {
            if(args[0].Type==DataType.UserData && args[0].UserData.Object is LuaUiObject o) rel=o.Model.Name;
            else if(args[0].Type==DataType.String) rel=args[0].String;
            var i=1;
            if(i<args.Length && args[i].Type==DataType.String) relPoint=args[i++].String;
            if(i<args.Length && args[i].Type==DataType.Number) x=args[i++].Number;
            if(i<args.Length && args[i].Type==DataType.Number) y=args[i].Number;
        }
        var anchor=new WowAnchor(point,rel,relPoint,x,y);
        for(var i=Model.Anchors.Count-1;i>=0;i--) if(Model.Anchors[i].Point.Equals(point,StringComparison.OrdinalIgnoreCase)) Model.Anchors.RemoveAt(i);
        Model.Anchors.Add(anchor); Log($"SetPoint({point}, {rel}, {relPoint}, {x}, {y})");
    }
    public int GetNumPoints() => Model.Anchors.Count;
    public DynValue GetPoint(int index=1)
    {
        if(index<1||index>Model.Anchors.Count)return DynValue.Nil;var a=Model.Anchors[index-1];
        return DynValue.NewTuple(DynValue.NewString(a.Point),DynValue.NewString(a.RelativeTo),DynValue.NewString(a.RelativePoint),DynValue.NewNumber(a.X),DynValue.NewNumber(a.Y));
    }
    public void ClearAllPoints() => Model.Anchors.Clear();
    public void SetAllPoints(object? relative = null) { var rel = relative as LuaUiObject; var name=rel?.Model.Name??Model.Parent?.Name??"UIParent"; Model.Anchors.Clear(); Model.Anchors.Add(new("TOPLEFT",name,"TOPLEFT",0,0)); Model.Anchors.Add(new("BOTTOMRIGHT",name,"BOTTOMRIGHT",0,0)); }
    public double GetLeft() => new WowLayoutEngine().Resolve(Model,_document).Left;
    public double GetRight() => new WowLayoutEngine().Resolve(Model,_document).Right;
    public double GetTop() => new WowLayoutEngine().Resolve(Model,_document).Top;
    public double GetBottom() => new WowLayoutEngine().Resolve(Model,_document).Bottom;
    public DynValue GetCenter(){var r=new WowLayoutEngine().Resolve(Model,_document);return DynValue.NewTuple(DynValue.NewNumber(r.CenterX),DynValue.NewNumber(r.CenterY));}
    public DynValue GetRect(){var r=new WowLayoutEngine().Resolve(Model,_document);return DynValue.NewTuple(DynValue.NewNumber(r.Left),DynValue.NewNumber(r.Top),DynValue.NewNumber(r.Width),DynValue.NewNumber(r.Height));}
    public void Show() => Model.Shown = true;
    public void Hide() => Model.Shown = false;
    public bool IsShown() => Model.Shown;
    public bool IsVisible() => Model.Shown && (Model.Parent?.Shown ?? true);
    public void SetAlpha(double value) => Model.Alpha = Math.Clamp(value, 0, 1);
    public double GetAlpha() => Model.Alpha;
    public void SetScale(double value)=>Model.Scale=Math.Max(.01,value);
    public double GetScale()=>Model.Scale;
    public double GetEffectiveScale()=>Model.Scale*(Model.Parent?.Scale??1);
    public void SetToplevel(bool value)=>Model.TopLevel=value;
    public bool IsToplevel()=>Model.TopLevel;
    public void SetFrameStrata(string strata){Model.FrameStrata=string.IsNullOrWhiteSpace(strata)?"MEDIUM":strata.ToUpperInvariant();Log($"SetFrameStrata({Model.FrameStrata})");}
    public string GetFrameStrata()=>Model.FrameStrata;
    public void SetFrameLevel(int level){Model.FrameLevel=Math.Max(0,level);Log($"SetFrameLevel({Model.FrameLevel})");}
    public int GetFrameLevel()=>Model.FrameLevel;
    public void Raise(){var siblings=Model.Parent?.Children;if(siblings is not null)Model.FrameLevel=siblings.Where(x=>x!=Model).Select(x=>x.FrameLevel).DefaultIfEmpty(Model.FrameLevel).Max()+1;Log("Raise()");}
    public void Lower(){var siblings=Model.Parent?.Children;if(siblings is not null)Model.FrameLevel=Math.Max(0,siblings.Where(x=>x!=Model).Select(x=>x.FrameLevel).DefaultIfEmpty(Model.FrameLevel).Min()-1);Log("Lower()");}
    public void SetText(string? text)
    {
        Model.Text=text;if(Model.Type!=WowObjectType.FontString)return;var visible=System.Text.RegularExpressions.Regex.Replace(text??string.Empty,@"\|c[0-9a-fA-F]{8}|\|r|\|H.*?\|h|\|h",string.Empty);var lines=visible.Split('\n');if(!Model.RuntimeFields.ContainsKey("ExplicitWidth"))Model.Width=Math.Max(1,lines.Max(x=>x.Length)*Math.Max(1,Model.FontSize)*.56);if(!Model.RuntimeFields.ContainsKey("ExplicitHeight"))Model.Height=Math.Max(1,lines.Length*Math.Max(1,Model.FontSize+Model.TextSpacing));
    }
    public string? GetText() => Model.Text;
    public void HighlightText(double start=0,double? end=null)=>Model.RuntimeFields["highlight"]=(start,end);
    public LuaUiObject GetFontString()
    {
        var font=Model.Children.FirstOrDefault(x=>x.Type==WowObjectType.FontString);
        if(font is null){font=new WowUiObject($"{Model.Name}.FontString#{Model.Children.Count+1}",WowObjectType.FontString,Model){FontPath="Fonts\\FRIZQT__.TTF",FontSize=12,Text=Model.Text};_document.ByName[font.Name]=font;}
        if(font.Text is null)font.Text=Model.Text;return new LuaUiObject(font,_document);
    }
    public void SetNormalFontObject(DynValue font){GetFontString().SetFontObject(font);}
    public void SetHighlightFontObject(DynValue font){ }
    public void SetDisabledFontObject(DynValue font){ }
    public void SetNormalTexture(DynValue value)=>SetButtonTexture("NormalTexture",value);
    public LuaUiObject? GetNormalTexture()=>GetButtonTexture("NormalTexture");
    public void SetHighlightTexture(DynValue value)=>SetButtonTexture("HighlightTexture",value);
    public LuaUiObject? GetHighlightTexture()=>GetButtonTexture("HighlightTexture");
    public void SetPushedTexture(DynValue value)=>SetButtonTexture("PushedTexture",value);
    public LuaUiObject? GetPushedTexture()=>GetButtonTexture("PushedTexture");
    public void SetDisabledTexture(DynValue value)=>SetButtonTexture("DisabledTexture",value);
    public LuaUiObject? GetDisabledTexture()=>GetButtonTexture("DisabledTexture");
    public void SetThumbTexture(DynValue value)=>SetButtonTexture("ThumbTexture",value);
    public LuaUiObject? GetThumbTexture()=>GetButtonTexture("ThumbTexture");
    private void SetButtonTexture(string key,DynValue value)
    {
        LuaUiObject? texture=value.Type==DataType.UserData?value.UserData.Object as LuaUiObject:null;
        if(texture is null&&value.Type==DataType.String){var model=new WowUiObject($"{Model.Name}.{key}",WowObjectType.Texture,Model){TexturePath=value.String};model.Anchors.Add(new("TOPLEFT",Model.Name,"TOPLEFT",0,0));model.Anchors.Add(new("BOTTOMRIGHT",Model.Name,"BOTTOMRIGHT",0,0));_document.ByName[model.Name]=model;texture=new LuaUiObject(model,_document);}
        if(texture is not null)Model.RuntimeFields[key]=texture;
    }
    private LuaUiObject? GetButtonTexture(string key)=>Model.RuntimeFields.TryGetValue(key,out var value)?value as LuaUiObject:null;
    public double GetStringWidth()
    {
        var lines=(Model.Text??string.Empty).Split('\n');return lines.Length==0?0:lines.Max(x=>x.Length)*Model.FontSize*.56;
    }
    public double GetStringHeight()
    {
        var text=Model.Text??string.Empty;var lines=Math.Max(1,text.Count(c=>c=='\n')+1);if(Model.Width>0){var charsPerLine=Math.Max(1,(int)(Model.Width/(Math.Max(1,Model.FontSize)*.56)));lines=Math.Max(lines,text.Split('\n').Sum(x=>Math.Max(1,(int)Math.Ceiling((double)x.Length/charsPerLine))));}return lines*Math.Max(1,Model.FontSize+Model.TextSpacing);
    }
    public void SetSpacing(double spacing){Model.TextSpacing=spacing;Log($"SetSpacing({spacing})");}
    public double GetSpacing()=>Model.TextSpacing;
    public void SetNonSpaceWrap(bool enabled)=>Model.RuntimeFields["nonSpaceWrap"]=enabled;
    public bool CanNonSpaceWrap()=>Model.RuntimeFields.TryGetValue("nonSpaceWrap",out var value)&&value is bool enabled&&enabled;
    public void SetWordWrap(bool enabled)=>Model.RuntimeFields["wordWrap"]=enabled;
    public bool CanWordWrap()=>!Model.RuntimeFields.TryGetValue("wordWrap",out var value)||value is not bool enabled||enabled;
    public void SetTextInsets(double left,double right,double top,double bottom){Model.TextInsets=(left,right,top,bottom);Log($"SetTextInsets({left}, {right}, {top}, {bottom})");}
    public DynValue GetTextInsets()=>DynValue.NewTuple(DynValue.NewNumber(Model.TextInsets.Left),DynValue.NewNumber(Model.TextInsets.Right),DynValue.NewNumber(Model.TextInsets.Top),DynValue.NewNumber(Model.TextInsets.Bottom));
    public void SetTexture(string? path) => Model.TexturePath = path;
    public string? GetTexture() => Model.TexturePath;
    public void SetTexCoord(params double[] coordinates){if(coordinates.Length is 4 or 8)Model.TextureCoordinates=coordinates.ToArray();}
    public void SetVertexColor(double r,double g,double b,double a=1)=>Model.VertexColor=(r,g,b,a);
    public void SetBlendMode(string mode)=>Model.BlendMode=mode;
    public LuaUiObject CreateTexture(string? name = null, string? layer = null)
    {
        var model = new WowUiObject(name ?? $"{Model.Name}.Texture#{Model.Children.Count + 1}", WowObjectType.Texture, Model);
        _document.ByName[model.Name] = model; return new(model, _document);
    }
    public LuaUiObject CreateFontString(string? name = null, string? layer = null, string? template = null)
    {
        var model = new WowUiObject(name ?? $"{Model.Name}.FontString#{Model.Children.Count + 1}", WowObjectType.FontString, Model){TemplateName=template};
        if(!string.IsNullOrWhiteSpace(template)&&_document.ByName.TryGetValue(template,out var font))
        {model.FontPath=font.FontPath;model.FontSize=font.FontSize;model.FontFlags=font.FontFlags;model.TextColor=font.TextColor;}
        _document.ByName[model.Name] = model; return new(model, _document);
    }
    public void SetBackdrop(Table value)
    {
        Model.BackdropBackground = value.Get("bgFile").CastToString(); Model.BackdropEdge = value.Get("edgeFile").CastToString();
    }
    public void SetBackdropColor(double r,double g,double b,double a=1)=>Model.BackdropColor=(r,g,b,a);
    public void SetBackdropBorderColor(double r,double g,double b,double a=1)=>Model.BackdropBorderColor=(r,g,b,a);
    public void SetScript(string scriptName, DynValue callback) { Model.Scripts.Add(scriptName);Model.ScriptCallbacks[scriptName]=callback;Log($"SetScript({scriptName})"); }
    public void HookScript(string scriptName, DynValue callback) {Model.Scripts.Add(scriptName);Model.ScriptCallbacks[scriptName]=callback;}
    public void Enable() => Model.Enabled=true;
    public void Disable() => Model.Enabled=false;
    public void SetEnabled(bool value)=>Model.Enabled=value;
    public bool IsEnabled() => Model.Enabled;
    public void SetChecked(bool value)=>Model.RuntimeFields["checked"]=value;
    public bool GetChecked()=>Model.RuntimeFields.TryGetValue("checked",out var value)&&value is bool enabled&&enabled;
    public void SetButtonState(string state,bool locked=false)=>Model.RuntimeFields["buttonState"]=state;
    public string GetButtonState()=>Model.RuntimeFields.TryGetValue("buttonState",out var value)?value?.ToString()??"NORMAL":"NORMAL";
    public void SetMinMaxValues(double min, double max) { Model.MinValue = min; Model.MaxValue = max; }
    public void SetValue(double value) => Model.Value = value;
    public double GetValue() => Model.Value;
    public void SetValueStep(double value)=>Model.RuntimeFields["valueStep"]=value;
    public double GetValueStep()=>Model.RuntimeFields.TryGetValue("valueStep",out var value)&&value is double number?number:1;
    public void SetStatusBarTexture(string path) => Model.TexturePath = path;
    public LuaUiObject GetStatusBarTexture()
    {
        var texture=Model.Children.FirstOrDefault(x=>x.Type==WowObjectType.Texture);
        if(texture is null){texture=new WowUiObject($"{Model.Name}.StatusBarTexture",WowObjectType.Texture,Model){TexturePath=Model.TexturePath};texture.SetParent(Model);_document.ByName[texture.Name]=texture;}
        return new LuaUiObject(texture,_document);
    }
    public void SetStatusBarColor(double r, double g, double b, double a = 1) { }
    public void SetOrientation(string orientation)=>Model.RuntimeFields["orientation"]=orientation;
    public string GetOrientation()=>Model.RuntimeFields.TryGetValue("orientation",out var value)?value?.ToString()??"HORIZONTAL":"HORIZONTAL";
    public void LockHighlight(){Model.RuntimeFields["highlightLocked"]=true;}
    public void UnlockHighlight(){Model.RuntimeFields["highlightLocked"]=false;}
    public void SetAutoFocus(bool value) { }
    public void SetMultiLine(bool value) { }
    public void SetNumeric(bool value) { }
    public void SetPassword(bool value) { }
    public void SetMaxLetters(double value) { }
    public void SetTextColor(double r, double g, double b, double a = 1) => Model.TextColor=(Math.Clamp(r,0,1),Math.Clamp(g,0,1),Math.Clamp(b,0,1),Math.Clamp(a,0,1));
    public void SetShadowColor(double r,double g,double b,double a=1)=>Model.ShadowColor=(Math.Clamp(r,0,1),Math.Clamp(g,0,1),Math.Clamp(b,0,1),Math.Clamp(a,0,1));
    public void SetShadowOffset(double x,double y)=>Model.ShadowOffset=(x,y);
    public void SetFont(string path,double size,string flags=""){Model.FontPath=path;Model.FontSize=size;Model.FontFlags=flags;Log($"SetFont({path}, {size}, {flags})");}
    public DynValue GetFont()=>DynValue.NewTuple(DynValue.NewString(Model.FontPath??""),DynValue.NewNumber(Model.FontSize),DynValue.NewString(Model.FontFlags??""));
    public void SetFontObject(DynValue font)
    {
        LuaUiObject? value=font.Type==DataType.UserData?font.UserData.Object as LuaUiObject:null;if(value is null&&font.Type==DataType.String&&_document.ByName.TryGetValue(font.String,out var named))value=new(named,_document);
        if(value is not null){Model.FontPath=value.Model.FontPath;Model.FontSize=value.Model.FontSize;Model.FontFlags=value.Model.FontFlags;Model.TextColor=value.Model.TextColor;}
    }
    public void RegisterEvent(string eventName){Model.Events.Add(eventName);Log($"RegisterEvent({eventName})");}
    public void UnregisterEvent(string eventName)=>Model.Events.Remove(eventName);
    public void UnregisterAllEvents()=>Model.Events.Clear();
    public void SetJustifyH(string value) => Model.HorizontalJustification=value.ToUpperInvariant();
    public void SetJustifyV(string value) => Model.VerticalJustification=value.ToUpperInvariant();
    public void SetMovable(bool value){Model.Movable=value;}
    public bool IsMovable()=>Model.Movable;
    public void SetClampedToScreen(bool value){Model.ClampedToScreen=value;}
    public void EnableMouse(bool value=true) { }
    public void EnableMouseWheel(bool value=true) { }
    public void RegisterForDrag(params string[] buttons) { }
    public void RegisterForClicks(params string[] buttons) { }
    public void StartMoving() { }
    public void StopMovingOrSizing() { }
    public void SetScrollChild(LuaUiObject child)=>Model.ScrollChild=child.Model;
    public LuaUiObject? GetScrollChild()=>Model.ScrollChild is null?null:new LuaUiObject(Model.ScrollChild,_document);
    public void SetVerticalScroll(double offset)=>Model.VerticalScroll=Math.Max(0,offset);
    public double GetVerticalScroll()=>Model.VerticalScroll;
    public double GetVerticalScrollRange()=>Math.Max(0,(Model.ScrollChild?.Height??0)-Model.Height);
    public void SetHorizontalScroll(double offset)=>Model.HorizontalScroll=Math.Max(0,offset);
    public double GetHorizontalScroll()=>Model.HorizontalScroll;
    public double GetHorizontalScrollRange()=>Math.Max(0,(Model.ScrollChild?.Width??0)-Model.Width);
    public void UpdateScrollChildRect() { }
}

internal sealed class LuaUiObjectDescriptor : IUserDataDescriptor
{
    private readonly IUserDataDescriptor _inner;
    public LuaUiObjectDescriptor(IUserDataDescriptor inner)=>_inner=inner;
    public string Name=>_inner.Name;
    public Type Type=>typeof(LuaUiObject);
    public DynValue Index(Script script,object obj,DynValue index,bool isDirectIndexing)
    {
        if(index.Type==DataType.String&&((LuaUiObject)obj).TryGetDynamic(index.String,out var value))return value;
        try{var reflected=_inner.Index(script,obj,index,isDirectIndexing);if(reflected is null&&index.Type==DataType.String&&index.String.Length>0&&(char.IsLower(index.String[0])||index.String[0]=='_'))return DynValue.Nil;return reflected!;}
        catch(ScriptRuntimeException) when(index.Type==DataType.String&&index.String.Length>0&&(char.IsLower(index.String[0])||index.String[0]=='_')){return DynValue.Nil;}
    }
    public bool SetIndex(Script script,object obj,DynValue index,DynValue value,bool isDirectIndexing)
    {
        try{if(_inner.SetIndex(script,obj,index,value,isDirectIndexing))return true;}catch(ScriptRuntimeException) when(index.Type==DataType.String){ }
        if(index.Type!=DataType.String)return false;((LuaUiObject)obj).SetDynamic(index.String,value);return true;
    }
    public string AsString(object obj)=>_inner.AsString(obj);
    public DynValue MetaIndex(Script script,object obj,string metaname)=>_inner.MetaIndex(script,obj,metaname);
    public bool IsTypeCompatible(Type type,object obj)=>_inner.IsTypeCompatible(type,obj);
}
