# Architecture

The dependency direction is `Lua source → MoonSharp TBC facade → WowDocument/WowUiObject → WPF renderer`. The source mapper independently associates safe literal call arguments with model properties. WPF never becomes the source of truth, and runtime userdata never wraps WPF controls.

- **App**: dark IDE shell, AvalonEdit, commands, canvas interaction, file dialogs.
- **Core/Lua**: soft-sandbox runtime and registered WoW/AIO globals.
- **Core model**: frames, regions, hierarchy, anchors, visual properties, diagnostics and API trace.
- **Source patching**: narrow source spans and exact-original-text validation.
- **Renderer**: model traversal and WoW-like custom canvas primitives.
- **TbcApi**: data-driven compatibility/implementation metadata.

The current UI uses a view model for document state and workflow, with a code-behind renderer for WPF-specific visual construction. Renderer and runtime can be extracted behind interfaces without changing the model.

## TbcClientData

Local client data is an independent pipeline: profile/settings, asynchronous scanner, cache, XML/template registry, Lua usage/event/global scanner, path resolver, BLP/TGA decoders, and memory-sensitive texture cache. XML and Lua both produce the existing internal UI model. WPF receives only decoded RGBA images and model objects; it does not parse client formats.
