using System.Collections.ObjectModel;

namespace MasterWoW.UIStudio.Core;

public enum WowObjectType { UIParent, Frame, Button, CheckButton, EditBox, ScrollFrame, Slider, StatusBar, Texture, FontString, Font }
public enum DiagnosticSeverity { Info, Warning, Error }

public sealed record SourceSpan(string Property, int ValueStart, int ValueLength, string OriginalText, bool IsLiteral, int Line);
public sealed record UiDiagnostic(DiagnosticSeverity Severity, string Category, string Message, int? Line = null);
public sealed record WowAnchor(string Point, string RelativeTo, string RelativePoint, double X, double Y);

public class WowUiObject
{
    public WowUiObject(string name, WowObjectType type, WowUiObject? parent = null)
    {
        Name = name; Type = type; Parent = parent; parent?.Children.Add(this);
    }

    public string Name { get; }
    public WowObjectType Type { get; }
    public WowUiObject? Parent { get; private set; }
    public ObservableCollection<WowUiObject> Children { get; } = new();
    public Dictionary<string, SourceSpan> Sources { get; } = new(StringComparer.OrdinalIgnoreCase);
    public double Width { get; set; } = 100;
    public double Height { get; set; } = 40;
    public double Alpha { get; set; } = 1;
    public bool Shown { get; set; } = true;
    public ObservableCollection<WowAnchor> Anchors { get; } = new();
    public WowAnchor Anchor
    {
        get => Anchors.Count == 0 ? new("CENTER", Parent?.Name ?? "UIParent", "CENTER", 0, 0) : Anchors[0];
        set { Anchors.Clear(); Anchors.Add(value); }
    }
    public string? Text { get; set; }
    public string? TemplateName { get; set; }
    public (double R,double G,double B,double A) TextColor { get; set; } = (1,1,1,1);
    public string HorizontalJustification { get; set; } = "CENTER";
    public string VerticalJustification { get; set; } = "MIDDLE";
    public bool Enabled { get; set; } = true;
    public string? TexturePath { get; set; }
    public double[] TextureCoordinates { get; set; } = {0,1,0,1};
    public (double R,double G,double B,double A) VertexColor { get; set; } = (1,1,1,1);
    public string BlendMode { get; set; } = "BLEND";
    public string? BackdropBackground { get; set; }
    public string? BackdropEdge { get; set; }
    public (double R,double G,double B,double A) BackdropColor { get; set; } = (1,1,1,1);
    public (double R,double G,double B,double A) BackdropBorderColor { get; set; } = (1,1,1,1);
    public HashSet<string> Scripts { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string,object> RuntimeFields { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string,object> ScriptCallbacks { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Events { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string? FontPath { get; set; }
    public double FontSize { get; set; } = 12;
    public string? FontFlags { get; set; }
    public (double R,double G,double B,double A) ShadowColor { get; set; } = (0,0,0,0);
    public (double X,double Y) ShadowOffset { get; set; }
    public double Scale { get; set; } = 1;
    public bool TopLevel { get; set; }
    public double TextSpacing { get; set; }
    public (double Left,double Right,double Top,double Bottom) TextInsets { get; set; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; } = 100;
    public double Value { get; set; }
    public bool Movable { get; set; }
    public bool ClampedToScreen { get; set; }
    public string FrameStrata { get; set; } = "MEDIUM";
    public int FrameLevel { get; set; }
    public WowUiObject? ScrollChild { get; set; }
    public double VerticalScroll { get; set; }
    public double HorizontalScroll { get; set; }

    public void SetParent(WowUiObject parent)
    {
        Parent?.Children.Remove(this); Parent = parent; parent.Children.Add(this);
    }
}

public sealed class WowDocument
{
    public WowDocument(double width = 1280, double height = 720)
    {
        Root = new WowUiObject("UIParent", WowObjectType.UIParent) { Width = width, Height = height };
        ByName[Root.Name] = Root;
    }
    public WowUiObject Root { get; }
    public Dictionary<string, WowUiObject> ByName { get; } = new(StringComparer.OrdinalIgnoreCase);
    public ObservableCollection<UiDiagnostic> Diagnostics { get; } = new();
    public ObservableCollection<string> ApiCalls { get; } = new();
    public ObservableCollection<AioHandlerRegistration> AioHandlers { get; } = new();
    public ObservableCollection<AioRequestRegistration> AioRequests { get; } = new();
    public ObservableCollection<string> SlashCommands { get; } = new();
    public ObservableCollection<CompatibilityLoadRecord> CompatibilityLayers { get; } = new();
    public double UiScale { get; set; } = 1;
    public bool ReadOnlyClientPreview { get; set; }
}

public sealed record AioHandlerRegistration(string Module, string Handler);
public sealed record AioRequestRegistration(string Module,string Handler,IReadOnlyList<object?> Arguments);
public sealed record CompatibilityLoadRecord(string Path, bool Loaded, string PrototypeMode, string? Diagnostic);
public sealed record WowRect(double Left, double Top, double Width, double Height)
{
    public double Right => Left + Width;
    public double Bottom => Top + Height;
    public double CenterX => Left + Width / 2;
    public double CenterY => Top + Height / 2;
}

public sealed class WowLayoutEngine
{
    public WowRect Resolve(WowUiObject item, WowDocument document)
        => Resolve(item, document, new HashSet<WowUiObject>());

    private WowRect Resolve(WowUiObject item, WowDocument document, HashSet<WowUiObject> resolving)
    {
        if (item == document.Root) return new(0, 0, item.Width, item.Height);
        if (!resolving.Add(item)) return item.Parent is null ? new(0,0,item.Width,item.Height) : Resolve(item.Parent,document,resolving);
        IEnumerable<WowAnchor> anchors = item.Anchors.Count == 0 ? new[] { new WowAnchor("CENTER", item.Parent?.Name ?? "UIParent", "CENTER", 0, 0) } : item.Anchors;
        double? left=null,right=null,top=null,bottom=null,centerX=null,centerY=null;
        foreach (var anchor in anchors)
        {
            var relativeTarget = document.ByName.TryGetValue(anchor.RelativeTo, out var target) && target != item ? target : item.Parent ?? document.Root;
            var relative = Resolve(relativeTarget, document, resolving);
            var (rx, ry) = Point(relative, anchor.RelativePoint);
            var x=rx+anchor.X; var y=ry-anchor.Y;
            switch(anchor.Point.ToUpperInvariant())
            {
                case "TOPLEFT": left=x; top=y; break; case "TOP": centerX=x; top=y; break; case "TOPRIGHT": right=x; top=y; break;
                case "LEFT": left=x; centerY=y; break; case "CENTER": centerX=x; centerY=y; break; case "RIGHT": right=x; centerY=y; break;
                case "BOTTOMLEFT": left=x; bottom=y; break; case "BOTTOM": centerX=x; bottom=y; break; case "BOTTOMRIGHT": right=x; bottom=y; break;
            }
        }
        var width=(left.HasValue&&right.HasValue ? Math.Max(0,right.Value-left.Value) : item.Width)*item.Scale;
        var height=(top.HasValue&&bottom.HasValue ? Math.Max(0,bottom.Value-top.Value) : item.Height)*item.Scale;
        var l=left ?? (right.HasValue ? right.Value-width : (centerX ?? 0)-width/2);
        var t=top ?? (bottom.HasValue ? bottom.Value-height : (centerY ?? 0)-height/2);
        resolving.Remove(item);
        return new(l,t,width,height);
    }

    private static (double X,double Y) Point(WowRect r,string point) => point.ToUpperInvariant() switch
    {
        "TOPLEFT"=>(r.Left,r.Top), "TOP"=>(r.CenterX,r.Top), "TOPRIGHT"=>(r.Right,r.Top),
        "LEFT"=>(r.Left,r.CenterY), "RIGHT"=>(r.Right,r.CenterY), "BOTTOMLEFT"=>(r.Left,r.Bottom),
        "BOTTOM"=>(r.CenterX,r.Bottom), "BOTTOMRIGHT"=>(r.Right,r.Bottom), _=>(r.CenterX,r.CenterY)
    };
}
