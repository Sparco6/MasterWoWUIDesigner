using System;
using System.IO;
using System.Text.Json;
using MasterWoW.UIStudio.Core;

namespace MasterWoW.UIStudio.App;

public sealed class ClientSettingsService
{
    private readonly string _path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MasterWoW.UIStudio","client-profile.json");
    private readonly string _preferencesPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MasterWoW.UIStudio","studio-preferences.json");
    public TbcClientProfile? Load(){try{return File.Exists(_path)?JsonSerializer.Deserialize<TbcClientProfile>(File.ReadAllText(_path)):null;}catch{return null;}}
    public void Save(TbcClientProfile profile){Directory.CreateDirectory(Path.GetDirectoryName(_path)!);File.WriteAllText(_path,JsonSerializer.Serialize(profile,new JsonSerializerOptions{WriteIndented=true}));}
    public StudioPreferences LoadPreferences(){try{return File.Exists(_preferencesPath)?JsonSerializer.Deserialize<StudioPreferences>(File.ReadAllText(_preferencesPath))??new():new();}catch{return new();}}
    public void SavePreferences(StudioPreferences preferences){Directory.CreateDirectory(Path.GetDirectoryName(_preferencesPath)!);File.WriteAllText(_preferencesPath,JsonSerializer.Serialize(preferences,new JsonSerializerOptions{WriteIndented=true}));}
}

public sealed record StudioPreferences
{
    public bool ShowBottomPanels { get; init; } = true;
    public bool ShowPropertiesPanel { get; init; } = true;
    public bool AutoAcceptDesignerChanges { get; init; }
}

public static class WpfTextureFactory
{
    public static System.Windows.Media.Imaging.BitmapSource Create(DecodedTexture texture)
    {
        var bgra=new byte[texture.Rgba32.Length];for(var i=0;i<texture.Rgba32.Length;i+=4){bgra[i]=texture.Rgba32[i+2];bgra[i+1]=texture.Rgba32[i+1];bgra[i+2]=texture.Rgba32[i];bgra[i+3]=texture.Rgba32[i+3];}
        var bitmap=System.Windows.Media.Imaging.BitmapSource.Create(texture.Width,texture.Height,96,96,System.Windows.Media.PixelFormats.Bgra32,null,bgra,texture.Width*4);bitmap.Freeze();return bitmap;
    }
}
