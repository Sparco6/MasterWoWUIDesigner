using MasterWoW.UIStudio.Core;

namespace MasterWoW.UIStudio.Tests;

public sealed class ApiCatalogTests
{
    [Fact] public void Developer_kit_runtime_export_imports_only_unique_functions()
    {
        var path=Path.GetTempFileName();try{File.WriteAllText(path,"Build: 2.4.3\nCreateFrame  [function]\nSomeTable [table]\nCreateFrame [function]\nGetItemInfo [function]\n");var entries=DeveloperKitApiLoader.LoadRuntimeFunctions(path);Assert.Equal(new[]{"CreateFrame","GetItemInfo"},entries.Select(x=>x.Name));Assert.All(entries,x=>Assert.Equal(Tbc243Availability.SupportedInTBC,x.Tbc243Availability));}finally{File.Delete(path);}
    }
    public static IEnumerable<object[]> CatalogFiles() => new[] { "widgets.json", "globals.json", "events.json", "templates.json", "constants.json" }.Select(x => new object[] { x });
    [Theory]
    [MemberData(nameof(CatalogFiles))]
    public void Catalog_is_valid_and_version_explicit(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Data", "TBC243", fileName));
        var catalog = ApiCatalogLoader.Load(path);
        Assert.NotEmpty(catalog.Entries); Assert.Empty(ApiCatalogLoader.Validate(catalog));
        Assert.All(catalog.Entries, entry => { Assert.False(string.IsNullOrWhiteSpace(entry.Notes)); Assert.NotEmpty(entry.SourceReference); Assert.True(Enum.IsDefined(entry.Tbc243Availability)); Assert.True(Enum.IsDefined(entry.EmulatorImplementationStatus)); });
    }
    [Fact]
    public void Later_api_is_never_classified_as_tbc()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Data", "TBC243"));
        var all = Directory.EnumerateFiles(root, "*.json").Select(ApiCatalogLoader.Load).SelectMany(x => x.Entries);
        Assert.DoesNotContain(all, x => x.Tbc243Availability == Tbc243Availability.SupportedInTBC && Version.TryParse(x.IntroducedVersion, out var v) && v > new Version(2, 4, 3));
    }
}
