using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MasterWoW.UIStudio.Core;
namespace MasterWoW.UIStudio.App;
public partial class TemplateExplorerWindow:Window
{
 private readonly TbcClientIndex _index;public event EventHandler<XmlTemplateDefinition>? PreviewRequested;public TemplateExplorerWindow(TbcClientIndex index){InitializeComponent();_index=index;Filter();}
 private void Search_Changed(object sender,TextChangedEventArgs e){if(IsLoaded)Filter();}private void Filter(){var q=Search.Text??"";Templates.ItemsSource=_index.Templates.Where(x=>x.Virtual&&(q.Length==0||x.Name.Contains(q,StringComparison.OrdinalIgnoreCase))).OrderBy(x=>x.Name).ToList();}
 private void Templates_SelectionChanged(object sender,SelectionChangedEventArgs e){if(Templates.SelectedItem is not XmlTemplateDefinition t)return;Details.Text=$"Name: {t.Name}\nType: {t.Type}\nVirtual: {t.Virtual}\nEnvironment: {t.Environment}\nDefined in: {t.DefinedIn}\nSize: {t.Width} × {t.Height}\nInherits: {string.Join(", ",t.Inherits)}\nResolved: {string.Join(" → ",t.ResolvedInheritance)}\nRegions: {t.Regions.Count}\nScripts: {string.Join(", ",t.Scripts.Keys)}\nChildren: {string.Join(", ",t.Children)}";}
 private void Preview_Click(object sender,RoutedEventArgs e){if(Templates.SelectedItem is XmlTemplateDefinition t)PreviewRequested?.Invoke(this,t);}
}
