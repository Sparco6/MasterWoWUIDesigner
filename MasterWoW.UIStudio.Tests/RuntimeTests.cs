using MasterWoW.UIStudio.Core;
using System.Text.Json;

namespace MasterWoW.UIStudio.Tests;
public sealed class RuntimeTests
{
    [Fact] public void CreateSection_helper_result_maps_call_size_and_later_position()
    {
        const string source="local function CreateSection(parent,width,height) local panel=CreateFrame('Frame',nil,parent);panel:SetSize(width,height);return panel end\nlocal frame=CreateFrame('Frame','F',UIParent)\nframe.content=CreateSection(frame,990,610)\nframe.content:SetPoint('TOPLEFT',10,0)";
        var document=new MoonSharpPreviewRuntime().Execute(source,1280,720);var content=document.ByName.Values.Single(x=>x.Parent?.Name=="F"&&x.Type==WowObjectType.Frame);Assert.Equal("990",content.Sources["Width"].OriginalText);Assert.Equal("610",content.Sources["Height"].OriginalText);Assert.Equal("10",content.Sources["X"].OriginalText);Assert.Equal("0",content.Sources["Y"].OriginalText);
    }

    [Fact] public void Executed_member_creation_maps_its_own_source_instead_of_an_unexecuted_earlier_branch()
    {
        const string source="local f=CreateFrame('Frame','F',UIParent)\nlocal function unused()\n f.other=f:CreateFontString(nil,'OVERLAY')\n f.other:SetPoint('TOPLEFT',99,-99)\nend\nlocal function build()\n f.page=f:CreateFontString(nil,'OVERLAY')\n f.page:SetPoint('TOPLEFT',10,-214)\nend\nbuild()";
        var document=new MoonSharpPreviewRuntime().Execute(source,1280,720);
        var page=Assert.Single(document.ByName.Values.Where(x=>x.Type==WowObjectType.FontString));
        Assert.Equal("10",page.Sources["X"].OriginalText);
        Assert.Equal("-214",page.Sources["Y"].OriginalText);
        var changed=new LuaSourceMapper().PatchLiteral(source,page.Sources["Y"],-180);
        Assert.Contains("f.page:SetPoint('TOPLEFT',10,-180)",changed);
        Assert.Contains("f.other:SetPoint('TOPLEFT',99,-99)",changed);
    }

    [Fact] public void Member_font_string_short_setpoint_is_source_mapped()
    {
        const string source="local f=CreateFrame('Frame','F',UIParent)\nf.page=f:CreateFontString(nil,'OVERLAY')\nf.page:SetPoint('TOPLEFT',10,-214)\nf.page:SetWidth(130)";
        var document=new MoonSharpPreviewRuntime().Execute(source,1280,720);
        var page=Assert.Single(document.ByName.Values.Where(x=>x.Type==WowObjectType.FontString));
        Assert.Equal("10",page.Sources["X"].OriginalText);
        Assert.Equal("-214",page.Sources["Y"].OriginalText);
        Assert.Equal("130",page.Sources["Width"].OriginalText);
    }

    [Fact] public void Texture_creation_receives_a_navigable_source_span()
    {
        const string source="local f=CreateFrame('Frame','F',UIParent)\nlocal art=f:CreateTexture(nil,'ARTWORK')\nart:SetTexture('Interface/Icons/INV_Misc_QuestionMark')";
        var document=new MoonSharpPreviewRuntime().Execute(source,1280,720);
        var texture=document.ByName.Values.Single(x=>x.Type==WowObjectType.Texture);
        Assert.True(texture.Sources.TryGetValue("Creation",out var span));
        Assert.NotNull(span);
        Assert.InRange(span!.Line,2,3);
    }

    [Fact] public void Master_progression_open_window_builds_visible_preview()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
        var path=Path.Combine(root,"MasterProgression","MasterProgression_AIO_Client.lua");
        if(!File.Exists(path))return;
        var runtime=new MoonSharpPreviewRuntime();
        var document=runtime.Execute(File.ReadAllText(path),1280,720);
        var open=document.AioHandlers.Single(x=>x.Module=="MasterProgression"&&x.Handler=="OpenWindow");
        using var payload=JsonDocument.Parse("{}");
        Assert.True(runtime.InvokeAioHandler(document,open.Module,open.Handler,null,payload.RootElement.Clone()),string.Join(" | ",document.Diagnostics.Select(x=>x.Message)));
        Assert.True(document.ByName.Count>100,$"Only {document.ByName.Count} UI objects were created.");
        Assert.Contains(document.ByName.Values,x=>x.Shown&&x.Text=="Master Progression");
        Assert.DoesNotContain(document.Diagnostics,x=>x.Severity==DiagnosticSeverity.Error);
    }

    [Fact] public void Dynamic_loop_creates_frames()
    {
        var d = new MoonSharpPreviewRuntime().Execute("for i=1,3 do local b=CreateFrame('Button','B'..i,UIParent); b:SetSize(80,20); end", 1280, 720);
        Assert.Equal(3, d.Root.Children.Count); Assert.Empty(d.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error));
    }
    [Fact] public void Lua_math_fmod_matches_tbc_for_positive_grid_indices()
    {
        var d=new MoonSharpPreviewRuntime().Execute("local f=CreateFrame('Frame','GridCell',UIParent);f:SetPoint('TOPLEFT',16+(math.fmod(3,2)*314),-120)",1280,720);
        Assert.Equal(330,d.ByName["GridCell"].Anchor.X);
    }
    [Fact] public void Frame_properties_flow_into_model()
    {
        var d = new MoonSharpPreviewRuntime().Execute("local f=CreateFrame('Frame','F',UIParent); f:SetSize(500,300); f:SetPoint('CENTER',UIParent,'CENTER',12,-8); f:SetAlpha(.5)", 1280, 720);
        var f = d.ByName["F"]; Assert.Equal(500, f.Width); Assert.Equal(300, f.Height); Assert.Equal(12, f.Anchor.X); Assert.Equal(.5, f.Alpha);
    }
    [Fact] public void Source_mapper_patches_only_literal()
    {
        var source = "local f=CreateFrame('Frame','F',UIParent)\nf:SetWidth(700) -- keep\n"; var d = new MoonSharpPreviewRuntime().Execute(source,1280,720); var span=d.ByName["F"].Sources["Width"];
        var changed = new LuaSourceMapper().PatchLiteral(source,span,850); Assert.Equal("local f=CreateFrame('Frame','F',UIParent)\nf:SetWidth(850) -- keep\n",changed);
    }
    [Fact] public void Dynamic_expression_is_not_source_editable()
    {
        var source="local f=CreateFrame('Frame','F',UIParent)\nlocal w=400\nf:SetWidth(w*2)"; var d=new MoonSharpPreviewRuntime().Execute(source,1280,720);
        Assert.False(d.ByName["F"].Sources.ContainsKey("Width")); Assert.Equal(800,d.ByName["F"].Width);
    }
    [Fact] public void Obvious_infinite_loop_is_blocked() { var d=new MoonSharpPreviewRuntime().Execute("while true do end",1280,720); Assert.Contains(d.Diagnostics,x=>x.Category=="Runtime timeout"); }
    [Fact] public void Bounded_while_true_with_break_is_allowed()
    {
        var d=new MoonSharpPreviewRuntime().Execute("local i=1; while true do if i >= 10 then break end; i=i+1 end; local f=CreateFrame('Frame','AfterLoop',UIParent)",800,600);
        Assert.True(d.ByName.ContainsKey("AfterLoop"));Assert.DoesNotContain(d.Diagnostics,x=>x.Category=="Runtime timeout");
    }
    [Fact] public void Parser_while_true_with_return_is_allowed()
    {
        var source="local function parser(s,i) while true do local c=s:sub(i,i); if c == '' then return i end; i=i+1 end end; local f=CreateFrame('Frame','Parsed',UIParent); f:SetWidth(parser('abc',1))";
        var d=new MoonSharpPreviewRuntime().Execute(source,800,600);Assert.Equal(4,d.ByName["Parsed"].Width);Assert.DoesNotContain(d.Diagnostics,x=>x.Category=="Runtime timeout");
    }
    [Fact] public void Infinite_aio_handler_is_terminated_by_same_runtime_guard()
    {
        var runtime=new MoonSharpPreviewRuntime();var d=runtime.Execute("local H=AIO.AddHandlers('Loop',{}); function H.Run() while true do end end",800,600);
        Assert.False(runtime.InvokeAioHandler(d,"Loop","Run"));var diagnostic=Assert.Single(d.Diagnostics.Where(x=>x.Category=="Runtime timeout"));Assert.Contains("AioHandler",diagnostic.Message);Assert.Contains("AIO:Loop.Run",diagnostic.Message);
    }
    [Fact] public async Task Initial_execution_is_externally_cancelable()
    {
        var runtime=new MoonSharpPreviewRuntime();using var cancellation=new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>runtime.ExecuteAsync("while true do end",800,600,sourceFile:"cancel.lua",cancellationToken:cancellation.Token));
    }
    [Fact] public void SetPoint_supports_overloads_and_multiple_anchors()
    {
        var source="local f=CreateFrame('Frame','F',UIParent); f:ClearAllPoints(); f:SetPoint('TOPLEFT',UIParent,'TOPLEFT',10,-20); f:SetPoint('BOTTOMRIGHT',UIParent,'BOTTOMRIGHT',-30,40)";
        var d=new MoonSharpPreviewRuntime().Execute(source,1000,700); var f=d.ByName["F"];
        Assert.Equal(2,f.Anchors.Count); var rect=new WowLayoutEngine().Resolve(f,d);
        Assert.Equal(10,rect.Left); Assert.Equal(20,rect.Top); Assert.Equal(960,rect.Width); Assert.Equal(640,rect.Height);
    }
    [Fact] public void Shorthand_setpoint_is_relative_to_parent()
    {
        var d=new MoonSharpPreviewRuntime().Execute("local p=CreateFrame('Frame','P',UIParent);p:SetSize(400,300);p:SetPoint('CENTER');local c=CreateFrame('Button','C',p);c:SetSize(100,20);c:SetPoint('TOPLEFT',16,-14)",1280,720);
        var parent=new WowLayoutEngine().Resolve(d.ByName["P"],d);var child=new WowLayoutEngine().Resolve(d.ByName["C"],d);Assert.Equal(parent.Left+16,child.Left);Assert.Equal(parent.Top+14,child.Top);
    }
    [Fact] public void Named_frames_are_globals_and_aio_client_does_not_return_early()
    {
        var source="local AIO=AIO or require('AIO'); if AIO.AddAddon() then return end; local H=AIO.AddHandlers('Demo',{}); function H.Open() end; CreateFrame('Frame','VisibleFrame',UIParent); VisibleFrame:SetWidth(321)";
        var d=new MoonSharpPreviewRuntime().Execute(source,1280,720);
        Assert.Equal(321,d.ByName["VisibleFrame"].Width); Assert.Contains(d.AioHandlers,h=>h.Module=="Demo"&&h.Handler=="Open");
    }
    [Fact] public void CreateFrame_applies_scanned_template_and_parent_names()
    {
        var index=new TbcClientIndex(); index.Templates.Add(new("DemoTemplate","Button",true,Array.Empty<string>(),ClientEnvironment.InGame,"demo.xml",120,32,Array.Empty<XmlAnchorDefinition>(),new[]{new XmlRegionDefinition("$parentIcon","Texture",null,"Interface\\Icons\\INV_Misc_QuestionMark",null)},new Dictionary<string,string>(),Array.Empty<string>(),Array.Empty<string>()));
        var d=new MoonSharpPreviewRuntime().Execute("CreateFrame('Button','DemoButton',UIParent,'DemoTemplate')",800,600,1,index);
        Assert.Equal(120,d.ByName["DemoButton"].Width); Assert.True(d.ByName.ContainsKey("DemoButtonIcon"));
    }
    [Fact] public void Captured_aio_handler_can_be_invoked()
    {
        var runtime=new MoonSharpPreviewRuntime();var d=runtime.Execute("local H=AIO.AddHandlers('Demo',{}); function H.Open() CreateFrame('Frame','FromHandler',UIParent) end",800,600);
        Assert.True(runtime.InvokeAioHandler(d,"Demo","Open")); Assert.True(d.ByName.ContainsKey("FromHandler"));
    }
    [Fact] public void Compatibility_layer_loads_before_target_and_records_adapter_mode()
    {
        var path=Path.GetTempFileName();try{File.WriteAllText(path,"function CompatValue() return 42 end");var runtime=new MoonSharpPreviewRuntime();var d=runtime.ExecuteWithCompatibility("local f=CreateFrame('Frame','CompatFrame',UIParent); f:SetWidth(CompatValue())",800,600,1,null,new[]{path});Assert.Equal(42,d.ByName["CompatFrame"].Width);Assert.True(d.CompatibilityLayers.Single().Loaded);Assert.Contains("adapter",d.CompatibilityLayers.Single().PrototypeMode,StringComparison.OrdinalIgnoreCase);}finally{File.Delete(path);}
    }
    [Fact] public void Actual_masterwow_compat_registers_polyfills_when_workspace_is_available()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));var path=Path.Combine(root,"InterfaceClient","Interface","FrameXML","FrameNew","DB","Compat.lua");if(!File.Exists(path))return;
        var runtime=new MoonSharpPreviewRuntime();var d=runtime.ExecuteWithCompatibility("local f=CreateFrame('Frame','AfterCompat',UIParent); f:SetWidth(Clamp(50,1,25))",800,600,1,null,new[]{path});
        Assert.Equal(25,d.ByName["AfterCompat"].Width);Assert.Single(d.CompatibilityLayers);Assert.Contains(d.Diagnostics,x=>x.Category=="Compatibility layer");
    }
    [Fact] public void Frames_accept_arbitrary_lua_fields_without_losing_widget_methods()
    {
        var d=new MoonSharpPreviewRuntime().Execute("local f=CreateFrame('Frame','F',UIParent); f.header={ value=73 }; f:SetWidth(f.header.value)",800,600);
        Assert.Equal(73,d.ByName["F"].Width);Assert.Empty(d.Diagnostics.Where(x=>x.Severity==DiagnosticSeverity.Error));
    }
    [Fact] public void Frame_strata_and_level_round_trip()
    {
        var d=new MoonSharpPreviewRuntime().Execute("local f=CreateFrame('Frame','Layered',UIParent);f:SetFrameStrata('DIALOG');f:SetFrameLevel(12);local s=f:GetFrameStrata();local l=f:GetFrameLevel()",800,600);
        var f=d.ByName["Layered"];Assert.Equal("DIALOG",f.FrameStrata);Assert.Equal(12,f.FrameLevel);Assert.Empty(d.Diagnostics.Where(x=>x.Severity==DiagnosticSeverity.Error));
    }
    [Fact] public async Task Frame_onclick_executes_with_runtime_protection()
    {
        var runtime=new MoonSharpPreviewRuntime();var d=runtime.Execute("local b=CreateFrame('Button','Tab',UIParent);b:SetScript('OnClick',function(self) self:SetWidth(321) end)",800,600);
        Assert.True(await runtime.InvokeFrameScriptAsync(d,d.ByName["Tab"],"OnClick"));Assert.Equal(321,d.ByName["Tab"].Width);
    }
    [Fact] public void Real_contribute_addon_passes_dynamic_frame_field_stage_when_workspace_is_available()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));var path=Path.Combine(root,"ContributeSystem","ZContributeAIO_Client.lua");if(!File.Exists(path))return;
        var runtime=new MoonSharpPreviewRuntime();var d=runtime.Execute(File.ReadAllText(path),1280,720);var open=d.AioHandlers.First(x=>x.Handler=="OpenWindow");runtime.InvokeAioHandler(d,open.Module,open.Handler);
        Assert.DoesNotContain(d.Diagnostics,x=>x.Message.Contains("Unbounded loop signature",StringComparison.OrdinalIgnoreCase));Assert.Contains(d.AioHandlers,x=>x.Handler=="OpenWindow");Assert.Contains(d.AioHandlers,x=>x.Handler=="UpdateWindow");Assert.True(d.ByName.Count>2);
        var window=Assert.IsType<WowUiObject>(d.ByName["ContributeAIOFrame"]);Assert.True(window.Shown);Assert.Equal(1000,window.Width);Assert.Equal(665,window.Height);Assert.True(window.Children.Count>0);Assert.Contains("Width",window.Sources.Keys);Assert.Contains("Height",window.Sources.Keys);
        Assert.True(d.Diagnostics.All(x=>x.Severity!=DiagnosticSeverity.Error),string.Join(" | ",d.Diagnostics.Where(x=>x.Severity==DiagnosticSeverity.Error).Select(x=>x.Message)));
    }
    [Fact] public void Real_pve_automatization_window_executes_when_workspace_is_available()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));var path=Path.Combine(root,"PvEAutomatization","PvEAutomatizationAIO_Client.lua");if(!File.Exists(path))return;
        var runtime=new MoonSharpPreviewRuntime();var d=runtime.Execute(File.ReadAllText(path),1280,720);Assert.True(d.AioHandlers.Any(x=>x.Handler=="ShowWindow"),string.Join(" | ",d.Diagnostics.Select(x=>x.Message)));using var payload=JsonDocument.Parse("{}");Assert.True(runtime.InvokeAioHandler(d,"PvEAutomatizationAIO","ShowWindow",null,payload.RootElement.Clone()),string.Join(" | ",d.Diagnostics.Select(x=>x.Message)));
        var frame=d.ByName["PvEAutomatizationAIOFrame"];Assert.True(frame.Shown);Assert.Equal("DIALOG",frame.FrameStrata);Assert.True(frame.Children.Count>0);var layout=new WowLayoutEngine();var totalObject=d.ByName.Values.Single(x=>x.Text=="Total Kills");var eliteObject=d.ByName.Values.Single(x=>x.Text=="Elite Kills");var total=layout.Resolve(totalObject,d);var elite=layout.Resolve(eliteObject,d);Assert.True(elite.Left>total.Left,$"Total={total} parent={totalObject.Parent?.Name}/{layout.Resolve(totalObject.Parent!,d)} parentAnchors={string.Join(';',totalObject.Parent!.Anchors)} anchors={string.Join(';',totalObject.Anchors)}; Elite={elite} parent={eliteObject.Parent?.Name}/{layout.Resolve(eliteObject.Parent!,d)} parentAnchors={string.Join(';',eliteObject.Parent!.Anchors)} anchors={string.Join(';',eliteObject.Anchors)}");Assert.Equal(total.Top,elite.Top);Assert.True(d.Diagnostics.All(x=>x.Severity!=DiagnosticSeverity.Error),string.Join(" | ",d.Diagnostics.Where(x=>x.Severity==DiagnosticSeverity.Error).Select(x=>x.Message)));
    }
    [Fact] public void Real_activity_rewards_panel_has_readable_non_overlapping_text_when_workspace_is_available()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));var path=Path.Combine(root,"AR_panel","AR_Panel","AR_Panel.lua");if(!File.Exists(path))return;var runtime=new MoonSharpPreviewRuntime();var d=runtime.Execute(File.ReadAllText(path),1280,720);Assert.True(runtime.InvokeAioHandler(d,"AR_Panel","Update",null,120d,300d,30d,1d,2d,1d,0d,0d,0d,0d,0d,0d),string.Join(" | ",d.Diagnostics.Select(x=>x.Message)));var frame=d.ByName["AR_Panel_Frame"];Assert.Equal(270,frame.Width);Assert.Equal(108,frame.Height);var title=d.ByName.Values.Single(x=>x.Text?.Contains("Activity Rewards",StringComparison.Ordinal)==true);Assert.True(title.Width<frame.Width);Assert.True(title.Height<40);Assert.DoesNotContain(d.Diagnostics,x=>x.Severity==DiagnosticSeverity.Error);
    }
    [Fact] public async Task Real_pve_tab_button_emits_request_for_preview_bridge_when_workspace_is_available()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));var path=Path.Combine(root,"PvEAutomatization","PvEAutomatizationAIO_Client.lua");if(!File.Exists(path))return;
        var runtime=new MoonSharpPreviewRuntime();var d=runtime.Execute(File.ReadAllText(path),1280,720);using var payload=JsonDocument.Parse("{}");Assert.True(runtime.InvokeAioHandler(d,"PvEAutomatizationAIO","ShowWindow",null,payload.RootElement.Clone()));var gear=d.ByName.Values.First(x=>x.Type==WowObjectType.Button&&x.Text=="Gear Vendor");Assert.True(await runtime.InvokeFrameScriptAsync(d,gear,"OnClick"));var request=Assert.Single(d.AioRequests.Where(x=>x.Handler=="RequestTab"));Assert.Equal("vendor",request.Arguments[0]);
    }
    [Theory]
    [InlineData("home","PvE Overview")]
    [InlineData("daily","Daily Challenges")]
    [InlineData("weekly","Weekly Challenges")]
    [InlineData("vendor","Gear Vendor")]
    [InlineData("reagents","Reagent Shop")]
    [InlineData("vendors","Vendors")]
    [InlineData("loot","Loot")]
    [InlineData("lfg","LFG")]
    [InlineData("leaderboards","Leaderboards")]
    public void Real_pve_show_window_can_render_every_tab(string tab,string heading)
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));var path=Path.Combine(root,"PvEAutomatization","PvEAutomatizationAIO_Client.lua");if(!File.Exists(path))return;
        var title=tab=="daily"?",\"tabData\":{\"title\":\"Daily Challenges\"}":tab=="weekly"?",\"tabData\":{\"title\":\"Weekly Challenges\"}":"";var runtime=new MoonSharpPreviewRuntime();var d=runtime.Execute(File.ReadAllText(path),1280,720);using var payload=JsonDocument.Parse($"{{\"activeTab\":\"{tab}\"{title}}}");Assert.True(runtime.InvokeAioHandler(d,"PvEAutomatizationAIO","ShowWindow",null,payload.RootElement.Clone()),string.Join(" | ",d.Diagnostics.Select(x=>x.Message)));Assert.Contains(d.ByName.Values,x=>x.Shown&&x.Text==heading);Assert.DoesNotContain(d.Diagnostics,x=>x.Severity==DiagnosticSeverity.Error);
    }
    [Fact] public void ScrollFrame_tracks_child_offsets_and_ranges()
    {
        var d=new MoonSharpPreviewRuntime().Execute("local s=CreateFrame('ScrollFrame','S',UIParent);s:SetSize(100,80);local c=CreateFrame('Frame','C',s);c:SetSize(120,200);s:SetScrollChild(c);s:SetVerticalScroll(25)",800,600);
        var s=d.ByName["S"];Assert.Same(d.ByName["C"],s.ScrollChild);Assert.Equal(25,s.VerticalScroll);Assert.Equal(120,s.ScrollChild!.Width);Assert.Empty(d.Diagnostics.Where(x=>x.Severity==DiagnosticSeverity.Error));
    }
    [Fact] public void Small_real_client_lua_module_executes_when_workspace_is_available()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));var path=Path.Combine(root,"TBCClientEdit","Interface","FrameXML","RaidFrame.lua");if(!File.Exists(path))return;
        var d=new MoonSharpPreviewRuntime().Execute(File.ReadAllText(path),1280,720);Assert.True(d.Diagnostics.All(x=>x.Severity!=DiagnosticSeverity.Error),string.Join(" | ",d.Diagnostics.Where(x=>x.Severity==DiagnosticSeverity.Error).Select(x=>x.Message)));
    }
}
