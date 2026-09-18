# FrameXML and GlueXML import

The safe XML reader disables DTD processing and external resolution. It extracts frame/widget definitions, virtual templates, inheritance lists, size, anchors, Texture/FontString regions, scripts, children, Include/Script dependencies, environment, and defining file. GlueXML records remain tagged `Glue` and are not exposed as ordinary in-game definitions.

`XmlTemplateInstantiator` resolves inherited template order, expands `$parent`, and creates the same `WowDocument/WowUiObject` model consumed by Lua preview. The Template Explorer can instantiate virtual templates under an automatically created preview root.

Current limitations include incomplete XML node coverage, no execution of embedded XML handlers, no full TOC/load-order interpreter, and property merging that covers the first milestone rather than every Blizzard XML edge case.
