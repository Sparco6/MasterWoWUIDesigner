using MasterWoW.UIStudio.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MasterWoW.UIStudio.Tests;

public sealed class ClientDataTests
{
    [Fact] public void Resolver_normalizes_case_separators_and_missing_extension()
    {
        var file=new InterfaceFileRecord("x","Icons\\Sword.blp","Interface\\Icons\\Sword.blp",InterfaceFileKind.Blp,ClientEnvironment.Shared,1,DateTimeOffset.UtcNow,".blp");
        var result=new AssetResolver(new[]{file}).Resolve("interface/icons/sword");Assert.True(result.Found);Assert.Equal(file,result.File);
    }
    [Fact] public void Blp1_paletted_alpha_decodes_rgba()
    {
        using var stream=CreateBlp1Palette();var decoded=new BlpDecoder().Decode(stream);Assert.Equal(2,decoded.Width);Assert.Equal(1,decoded.Height);Assert.Equal(new byte[]{255,0,0,255,0,255,0,0},decoded.Rgba32);
    }
    [Fact] public void Blp1_jpeg_decodes_rgba()
    {
        using var image=new Image<Rgba32>(1,1,new Rgba32(12,34,56,255));using var jpeg=new MemoryStream();image.SaveAsJpeg(jpeg);using var blp=CreateBlp1Jpeg(jpeg.ToArray());var decoded=new BlpDecoder().Decode(blp);Assert.Equal(1,decoded.Width);Assert.Equal(1,decoded.Height);Assert.Equal("JPEG",decoded.Metadata.Compression);
    }
    [Theory] [InlineData("DXT1",0,0)] [InlineData("DXT3",8,1)] [InlineData("DXT5",8,7)]
    public void Blp2_dxt_variants_decode(string mode,byte alphaDepth,byte alphaEncoding)
    {
        using var stream=CreateBlp2(mode,alphaDepth,alphaEncoding);var decoded=new BlpDecoder().Decode(stream);Assert.Equal(4,decoded.Width);Assert.Equal(4,decoded.Height);Assert.Equal(mode,decoded.Metadata.Compression);Assert.Equal(255,decoded.Rgba32[0]);
    }
    [Fact] public void Tga_uncompressed_rgba_preserves_alpha_and_bottom_origin()
    {
        using var stream=CreateTga(false,false);var decoded=new TgaDecoder().Decode(stream);Assert.Equal(new byte[]{0,255,0,255,255,0,0,128},decoded.Rgba32);
    }
    [Fact] public void Tga_rle_top_origin_decodes()
    {
        using var stream=CreateTga(true,true);var decoded=new TgaDecoder().Decode(stream);Assert.Equal(2,decoded.Width);Assert.Equal(255,decoded.Rgba32[3]);Assert.Equal(255,decoded.Rgba32[7]);
    }
    [Fact] public void Casino_tga_resolves_directly_when_scan_cache_is_stale()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","TBCClientEdit","Interface"));if(!Directory.Exists(root))return;var path=AssetResolver.ResolveFromInterfaceFolder(root,"Interface\\Custom\\Casino\\slots_bg");Assert.NotNull(path);using var stream=File.OpenRead(path!);var decoded=new TgaDecoder().Decode(stream);Assert.True(decoded.Width>0);Assert.True(decoded.Height>0);
    }
    [Fact] public async Task Scanner_parses_virtual_template_parent_names_and_usage()
    {
        var root=Path.Combine(Path.GetTempPath(),"masterwow-scan-"+Guid.NewGuid().ToString("N"));var cache=Path.Combine(root,"cache");Directory.CreateDirectory(Path.Combine(root,"FrameXML"));
        try{await File.WriteAllTextAsync(Path.Combine(root,"FrameXML","Test.xml"),"<Ui><Button name=\"TestTemplate\" virtual=\"true\"><Size x=\"120\" y=\"30\"/><Layers><Layer level=\"ARTWORK\"><Texture name=\"$parentIcon\" file=\"Interface\\Icons\\Test\"/></Layer></Layers><Scripts><OnClick>Test_OnClick();</OnClick></Scripts></Button></Ui>");await File.WriteAllTextAsync(Path.Combine(root,"FrameXML","Test.lua"),"function Test_OnClick() UIParent:SetPoint('CENTER'); this:RegisterEvent('PLAYER_LOGIN'); end");var index=await new InterfaceScanner().ScanAsync(new("Synthetic","2.4.3.8606",root,cache),ScanMode.FullRebuild);var template=Assert.Single(index.Templates);Assert.True(template.Virtual);Assert.Contains(index.ApiUsage,x=>x.Name=="SetPoint");Assert.Contains(index.Events,x=>x.Name=="PLAYER_LOGIN");var doc=new XmlTemplateInstantiator().Instantiate(index,"TestTemplate");Assert.Contains(doc.ByName.Keys,x=>x=="PreviewTestTemplateIcon");}
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    [Fact] public async Task Scanner_preserves_nested_frame_and_region_geometry()
    {
        var root=Path.Combine(Path.GetTempPath(),"masterwow-hierarchy-"+Guid.NewGuid().ToString("N"));var cache=Path.Combine(root,"cache");Directory.CreateDirectory(Path.Combine(root,"FrameXML"));
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root,"FrameXML","Panel.xml"),"<Ui><Frame name=\"Panel\"><Size><AbsDimension x=\"300\" y=\"200\"/></Size><Frames><Button name=\"$parentAction\"><Size><AbsDimension x=\"80\" y=\"22\"/></Size><Anchors><Anchor point=\"BOTTOM\"><Offset><AbsDimension x=\"0\" y=\"12\"/></Offset></Anchor></Anchors></Button></Frames><Layers><Layer level=\"ARTWORK\"><FontString name=\"$parentTitle\" text=\"Hello\"><Size><AbsDimension x=\"120\" y=\"18\"/></Size><Anchors><Anchor point=\"TOP\"><Offset><AbsDimension x=\"0\" y=\"-10\"/></Offset></Anchor></Anchors></FontString></Layer></Layers></Frame></Ui>");
            var index=await new InterfaceScanner().ScanAsync(new("Synthetic","2.4.3.8606",root,cache),ScanMode.FullRebuild);var top=index.Templates.Single(x=>x.Name=="Panel");var nested=index.Templates.Single(x=>x.Name=="$parentAction");Assert.Null(top.ParentName);Assert.Equal("Panel",nested.ParentName);var doc=new XmlTemplateInstantiator().Instantiate(index,"Panel");var action=doc.ByName["PreviewPanelAction"];var title=doc.ByName["PreviewPanelTitle"];Assert.Same(doc.ByName["PreviewPanel"],action.Parent);Assert.Equal(80,action.Width);Assert.Equal(120,title.Width);Assert.Equal("TOP",title.Anchor.Point);
        }
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    [Fact] public async Task Scanner_preserves_declared_parent_and_missing_anchor_targets_stay_local()
    {
        var root=Path.Combine(Path.GetTempPath(),"masterwow-parent-"+Guid.NewGuid().ToString("N"));var cache=Path.Combine(root,"cache");Directory.CreateDirectory(Path.Combine(root,"FrameXML"));
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root,"FrameXML","Child.xml"),"<Ui><Frame name=\"Child\" parent=\"Owner\" setAllPoints=\"true\"><Layers><Layer><Texture name=\"$parentArt\"><Size x=\"20\" y=\"10\"/><Anchors><Anchor point=\"TOPLEFT\" relativeTo=\"UnavailableSibling\"/></Anchors></Texture></Layer></Layers></Frame></Ui>");
            var index=await new InterfaceScanner().ScanAsync(new("Synthetic","2.4.3.8606",root,cache),ScanMode.FullRebuild);var definition=Assert.Single(index.Templates,x=>x.Name=="Child");Assert.Equal("Owner",definition.DeclaredParent);var doc=new XmlTemplateInstantiator().Instantiate(index,"Child",400,300);var frame=doc.ByName["PreviewChild"];var art=doc.ByName["PreviewChildArt"];var rect=new WowLayoutEngine().Resolve(art,doc);var parentRect=new WowLayoutEngine().Resolve(frame,doc);Assert.Equal(parentRect.Left,rect.Left);Assert.Equal(parentRect.Top,rect.Top);
        }
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    [Fact] public void Local_extracted_blp_decodes_when_available()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","TBCClientEdit","Interface"));if(!Directory.Exists(root))return;var file=Directory.EnumerateFiles(root,"*.blp",SearchOption.AllDirectories).First();using var stream=File.OpenRead(file);var decoded=new BlpDecoder().Decode(stream);Assert.True(decoded.Width>0);Assert.Equal(decoded.Width*decoded.Height*4,decoded.Rgba32.Length);
    }
    [Fact] public async Task Local_extracted_interface_scans_when_available()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","TBCClientEdit","Interface"));if(!Directory.Exists(root))return;var cache=Path.Combine(Path.GetTempPath(),"masterwow-local-scan-"+Guid.NewGuid().ToString("N"));
        try{var index=await new InterfaceScanner().ScanAsync(new("Local integration","2.4.3.8606",root,cache),ScanMode.FullRebuild);Assert.True(index.Textures.Count>1000);Assert.Contains(index.Textures,x=>x.Metadata?.Format=="BLP2");Assert.NotEmpty(index.Templates.Where(x=>x.Virtual));Assert.NotEmpty(index.ApiUsage);Assert.NotEmpty(index.Events);Assert.True(File.Exists(Path.Combine(cache,"TBC243","scan-metadata.json")));}
        finally{if(Directory.Exists(cache))Directory.Delete(cache,true);}
    }
    private static MemoryStream CreateBlp1Palette(){var s=new MemoryStream();using(var w=new BinaryWriter(s,System.Text.Encoding.ASCII,true)){w.Write(System.Text.Encoding.ASCII.GetBytes("BLP1"));w.Write(1u);w.Write(8u);w.Write(2u);w.Write(1u);w.Write(5u);w.Write(4u);for(var i=0;i<16;i++)w.Write(i==0?1180u:0u);for(var i=0;i<16;i++)w.Write(i==0?4u:0u);var palette=new byte[1024];palette[2]=255;palette[5]=255;w.Write(palette);w.Write(new byte[]{0,1,255,0});}s.Position=0;return s;}
    private static MemoryStream CreateBlp1Jpeg(byte[] jpeg){var s=new MemoryStream();using(var w=new BinaryWriter(s,System.Text.Encoding.ASCII,true)){w.Write(System.Text.Encoding.ASCII.GetBytes("BLP1"));w.Write(0u);w.Write(0u);w.Write(1u);w.Write(1u);w.Write(0u);w.Write(0u);for(var i=0;i<16;i++)w.Write(i==0?160u:0u);for(var i=0;i<16;i++)w.Write(i==0?(uint)jpeg.Length:0u);w.Write(0u);w.Write(jpeg);}s.Position=0;return s;}
    private static MemoryStream CreateBlp2(string mode,byte alphaDepth,byte alphaEncoding){var s=new MemoryStream();using(var w=new BinaryWriter(s,System.Text.Encoding.ASCII,true)){w.Write(System.Text.Encoding.ASCII.GetBytes("BLP2"));w.Write(1u);w.Write((byte)2);w.Write(alphaDepth);w.Write(alphaEncoding);w.Write((byte)0);w.Write(4u);w.Write(4u);for(var i=0;i<16;i++)w.Write(i==0?1172u:0u);var size=mode=="DXT1"?8u:16u;for(var i=0;i<16;i++)w.Write(i==0?size:0u);w.Write(new byte[1024]);if(mode=="DXT3")w.Write(Enumerable.Repeat((byte)255,8).ToArray());else if(mode=="DXT5")w.Write(new byte[]{255,0,0,0,0,0,0,0});w.Write((ushort)0xF800);w.Write((ushort)0x001F);w.Write(0u);}s.Position=0;return s;}
    private static MemoryStream CreateTga(bool rle,bool top){var s=new MemoryStream();using(var w=new BinaryWriter(s,System.Text.Encoding.ASCII,true)){w.Write((byte)0);w.Write((byte)0);w.Write((byte)(rle?10:2));w.Write(new byte[9]);w.Write((ushort)2);w.Write((ushort)1);w.Write((byte)32);w.Write((byte)((top?0x20:0)|8));if(rle){w.Write((byte)0x81);w.Write(new byte[]{0,0,255,255});}else{w.Write(new byte[]{0,255,0,255,0,0,255,128});}}s.Position=0;return s;}
}
