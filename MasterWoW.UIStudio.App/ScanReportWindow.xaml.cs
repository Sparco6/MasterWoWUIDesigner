using System.Linq;
using System.Windows;
using MasterWoW.UIStudio.Core;
namespace MasterWoW.UIStudio.App;
public partial class ScanReportWindow:Window
{
 public ScanReportWindow(TbcClientIndex index){InitializeComponent();var m=index.Metadata!;Summary.Text=$"TBC Client Scan Complete\n\nProfile:         {m.ProfileName}\nTarget:          {m.TargetVersion}\nInterface root:  {m.InterfaceRoot}\n\nLua files:       {m.LuaFiles}\nXML files:       {m.XmlFiles}\nBLP files:       {m.BlpFiles}\nTGA files:       {m.TgaFiles}\nFonts:           {m.FontFiles}\nTemplates/frames:{m.Templates}\nObserved APIs:   {m.ObservedApis}\nEvents:          {m.Events}\nMissing assets:  {m.MissingAssets}\nWarnings:        {m.Warnings}\nErrors:          {m.Errors}\n\nCompleted: {m.CompletedAt.LocalDateTime}";Apis.ItemsSource=index.ApiUsage;Diagnostics.ItemsSource=index.Diagnostics.Select(x=>$"{x.Severity} [{x.Category}] {x.File} {x.Message}");}
}
