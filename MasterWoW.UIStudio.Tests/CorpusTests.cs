using MasterWoW.UIStudio.Core;

namespace MasterWoW.UIStudio.Tests;

public sealed class CorpusTests
{
    [Fact] public void Toc_parser_preserves_declared_order_and_metadata()
    {
        var path=Path.GetTempFileName();
        try { File.WriteAllText(path,"## Interface: 20400\n## Dependencies: One, Two\nCore.lua\nUI.xml\n"); var toc=new TocParser().Parse(path); Assert.Equal("20400",toc.Headers["Interface"]); Assert.Equal(new[]{"Core.lua","UI.xml"},toc.OrderedFiles); Assert.Equal(new[]{"One","Two"},toc.Dependencies); }
        finally { File.Delete(path); }
    }
    [Fact] public void Content_classification_detects_aio_and_compatibility_layers()
    {
        Assert.Equal(SourceClassification.AioClient,AddonCorpusScanner.Classify("Any.lua","local AIO=AIO or require('AIO'); AIO.AddHandlers('X',{})",".lua",out _));
        Assert.Equal(SourceClassification.CompatibilityLayer,AddonCorpusScanner.Classify("Compat.lua","_AIO_POLYFILLS.SetSize = true",".lua",out _));
    }
}
