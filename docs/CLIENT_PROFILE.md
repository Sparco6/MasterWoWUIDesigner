# Client profile

## MasterWoW TBC 2.4.3

The active profile combines stock TBC 2.4.3 build 8606 evidence, the externally configured extracted Interface tree, its scanned FrameXML templates/assets, and the discovered `Compat.lua` layer. The reference Interface tree is read-only; the Studio only reads and decodes its assets in memory.

Current live order:

1. Create core Lua globals and CLR-backed UI object adapter.
2. Attach the scanned client template/asset index.
3. Execute the first discovered `Compat.lua` as a compatibility stage.
4. Execute the target Lua source through the resulting Lua global table.

Whole-FrameXML TOC/include/script execution and additional custom-client extension ordering remain incomplete.
