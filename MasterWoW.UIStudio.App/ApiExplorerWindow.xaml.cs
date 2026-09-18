using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MasterWoW.UIStudio.Core;

namespace MasterWoW.UIStudio.App;

public partial class ApiExplorerWindow : Window
{
    private readonly List<Row> _all=new();
    public ApiExplorerWindow(){InitializeComponent();var root=Path.Combine(AppContext.BaseDirectory,"Data","TBC243");foreach(var file in new[]{"widgets.json","globals.json","events.json","templates.json","constants.json"}){var catalog=ApiCatalogLoader.Load(Path.Combine(root,file));_all.AddRange(catalog.Entries.Select(x=>new Row(x,file[..^5])));}var observed=DeveloperKitApiLoader.LoadRuntimeFunctions(Path.Combine(AppContext.BaseDirectory,"Data","DeveloperKit","APIwithoutaddons.txt"));var known=_all.Select(x=>x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);_all.AddRange(observed.Where(x=>!known.Contains(x.Name)).Select(x=>new Row(x,"Runtime observed")));ApplyFilter();}
    private void Filter_Changed(object sender,EventArgs e){if(!IsLoaded)return;ApplyFilter();}
    private void ApplyFilter(){var text=Search?.Text?.Trim()??"";var kind=(Kind?.SelectedItem as ComboBoxItem)?.Content?.ToString()??"All";Entries.ItemsSource=_all.Where(x=>(kind=="All"||x.Kind.StartsWith(kind,StringComparison.OrdinalIgnoreCase))&&(text.Length==0||x.SearchText.Contains(text,StringComparison.OrdinalIgnoreCase))).ToList();}
    private void Entries_SelectionChanged(object sender,SelectionChangedEventArgs e){if(Entries.SelectedItem is not Row row){Details.Text="";return;}var x=row.Entry;Details.Text=$"{x.Signature}\n\n{x.Documentation}\n\nDeclared owner: {x.OwnerType}\nBase type: {x.BaseType}\nStock TBC 2.4.3: {x.Tbc243Availability}\nIntroduced: {x.IntroducedVersion??"Uncertain"}\nRemoved: {x.RemovedVersion??"—"}\nEmulator: {x.EmulatorImplementationStatus}\nTests: {x.TestStatus}\n\nEvidence:\n"+string.Join("\n",x.SourceReference.Select(s=>$"• {s.Title}: {s.Evidence}"));}
    private void OpenDocumentation_Click(object sender,RoutedEventArgs e){if(Entries.SelectedItem is not Row { Entry.SourceReference.Count:>0 } row||row.Entry.SourceReference[0].Url.StartsWith("local://",StringComparison.OrdinalIgnoreCase))return;Process.Start(new ProcessStartInfo(row.Entry.SourceReference[0].Url){UseShellExecute=true});}
    private sealed class Row{public Row(ApiCatalogEntry entry,string kind){Entry=entry;Kind=kind;}public ApiCatalogEntry Entry{get;}public string Name=>Entry.Name;public string Kind{get;}public string Owner=>Entry.OwnerType;public string Availability=>Entry.Tbc243Availability.ToString();public string Implementation=>Entry.EmulatorImplementationStatus.ToString();public string SearchText=>$"{Name} {Kind} {Owner} {Entry.BaseType} {Entry.Documentation} {string.Join(' ',Entry.SourceReference.Select(x=>x.Title))}";}
}
