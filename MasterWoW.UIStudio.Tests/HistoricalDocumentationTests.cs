using MasterWoW.UIStudio.Core;

namespace MasterWoW.UIStudio.Tests;

public sealed class HistoricalDocumentationTests
{
    [Fact] public void Importer_extracts_api_links_without_treating_them_as_tbc_proof()
    {
        var html="<a href='/wiki/SetPoint'>SetPoint</a><a href='/wiki/not-an-api'>guide</a>";var facts=new HistoricalApiDocumentationImporter().ParseIndexHtml(html,"https://wowwiki-archive.fandom.com/wiki/Widget_API","WidgetMethod");
        var fact=Assert.Single(facts);Assert.Equal("SetPoint",fact.Name);Assert.Null(fact.IntroducedVersion);Assert.Equal(EvidenceConfidence.HistoricalDocumentation,fact.Confidence);
    }
    [Fact] public void Cached_metadata_loads_offline_and_preserves_unavailable_status()
    {
        var json="{\"schemaVersion\":\"1.0\",\"refreshedAt\":\"2026-08-26T00:00:00Z\",\"sources\":[{\"key\":\"WidgetApi\",\"url\":\"https://example.invalid\",\"status\":\"Unavailable\",\"checkedAt\":\"2026-08-26T00:00:00Z\",\"error\":\"offline\",\"importedEntries\":0}],\"entries\":[]}";
        var snapshot=new HistoricalApiDocumentationImporter().ImportCachedMetadata(json);Assert.Equal(HistoricalSourceStatus.Unavailable,snapshot.Sources.Single().Status);
    }
    [Fact] public void Unified_merge_prefers_compat_and_direct_client_evidence_over_history()
    {
        var native=new[]{new ApiCatalogEntry("CreateColor","Global","LuaGlobal","CreateColor(...) ",Array.Empty<ApiArgument>(),Array.Empty<ApiReturnValue>(),"3.0",null,Tbc243Availability.NotInTBC,"Later stock API",new[]{new ApiReference("x","https://example.invalid","test")},EmulatorImplementationStatus.Implemented,CatalogTestStatus.Covered,"")};
        var corpus=new AddonCorpusIndex();corpus.CompatibilityApis.Add(new("CreateColor","GlobalPolyfill","Compat.lua",1,"function CreateColor",true));var merged=new UnifiedApiKnowledgeBuilder().Build(native,corpus,null,null).Single();Assert.Equal(KnowledgeAvailability.NotSupported,merged.StockTbc);Assert.Equal(KnowledgeAvailability.CompatProvided,merged.MasterWow);
    }
}
