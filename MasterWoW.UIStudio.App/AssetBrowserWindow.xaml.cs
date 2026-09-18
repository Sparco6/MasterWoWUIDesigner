using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MasterWoW.UIStudio.Core;

namespace MasterWoW.UIStudio.App;
public partial class AssetBrowserWindow : Window
{
 private readonly TbcClientIndex _index; private readonly List<TextureAsset> _all; private readonly TextureCache _cache=new(); public event EventHandler<TextureAsset>? UseRequested;
 public AssetBrowserWindow(TbcClientIndex index){InitializeComponent();_index=index;_all=index.Textures;ApplyFilter();}
 private void Filter_Changed(object sender,RoutedEventArgs e){if(IsLoaded)ApplyFilter();}
 private void ApplyFilter(){var text=SearchBox.Text?.Trim()??"";var type=(TypeFilter.SelectedItem as ComboBoxItem)?.Content?.ToString()??"All";IEnumerable<TextureAsset> q=_all;if(text.Length>0)q=q.Where(x=>x.File.WowPath.Contains(text,StringComparison.OrdinalIgnoreCase));q=type switch{"BLP"=>q.Where(x=>x.File.Kind==InterfaceFileKind.Blp),"TGA"=>q.Where(x=>x.File.Kind==InterfaceFileKind.Tga),"PNG"=>q.Where(x=>x.File.Kind==InterfaceFileKind.Png),"Fonts"=>Enumerable.Empty<TextureAsset>(),"Icons"=>q.Where(x=>x.File.WowPath.StartsWith("Interface\\Icons\\",StringComparison.OrdinalIgnoreCase)),_=>q};Assets.ItemsSource=q.Take(5000).ToList();}
 private async void Assets_SelectionChanged(object sender,SelectionChangedEventArgs e){if(Assets.SelectedItem is not TextureAsset asset)return;Metadata.Text=$"{asset.File.WowPath}\n{asset.File.DiskPath}\n{asset.Metadata?.Format}  {asset.Metadata?.Width}×{asset.Metadata?.Height}  {asset.Metadata?.Compression}\nAlpha: {asset.Metadata?.AlphaDepth}-bit   Mipmaps: {asset.Metadata?.MipCount}\nFile size: {asset.File.Size:N0} bytes";try{var decoded=await _cache.GetOrDecodeAsync(asset.File.DiskPath,DecodeAsync);PreviewImage.Source=WpfTextureFactory.Create(decoded);}catch(Exception ex){PreviewImage.Source=null;Metadata.Text+=$"\n\nPreview error: {ex.Message}";}}
 private static Task<DecodedTexture> DecodeAsync(string path,System.Threading.CancellationToken ct)=>Task.Run(()=>{ct.ThrowIfCancellationRequested();using var stream=File.Open(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);ITextureDecoder decoder=path.EndsWith(".blp",StringComparison.OrdinalIgnoreCase)?new BlpDecoder():new TgaDecoder();return decoder.Decode(stream);},ct);
 private void Copy_Click(object sender,RoutedEventArgs e){if(Assets.SelectedItem is TextureAsset a)Clipboard.SetText(Path.ChangeExtension(a.File.WowPath,null)!);}
 private void Use_Click(object sender,RoutedEventArgs e){if(Assets.SelectedItem is TextureAsset a)UseRequested?.Invoke(this,a);}
 private void References_Click(object sender,RoutedEventArgs e){if(Assets.SelectedItem is not TextureAsset a)return;var stem=Path.ChangeExtension(a.File.WowPath,null)!.Replace("Interface\\","");var hits=_index.Files.Where(x=>x.Kind is InterfaceFileKind.Lua or InterfaceFileKind.Xml).SelectMany(f=>{try{return File.ReadLines(f.DiskPath).Select((line,i)=>(line,i)).Where(x=>x.line.Contains(stem,StringComparison.OrdinalIgnoreCase)).Select(x=>$"{f.RelativePath}:{x.i+1}  {x.line.Trim()}").Take(20).ToList();}catch{return new List<string>();}}).Take(200);MessageBox.Show(string.Join(Environment.NewLine,hits),"Asset references");}
 private void Close_Click(object sender,RoutedEventArgs e)=>Close();
}
