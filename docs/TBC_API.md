# TBC API policy

The target is TBC 2.4.3/build 8606. The authoritative application catalogs now live in `Data/TBC243`: widgets, globals, events, templates, and constants. Every entry separates historical availability from emulator implementation and test status.

Classification rules:

1. `SupportedInTBC` requires direct version-pinned 2.4.3 FrameXML evidence or an explicit introduction at/before 2.4.3 with no prior removal.
2. `NotInTBC` requires an introduction after 2.4.3 or other decisive exclusion evidence.
3. Conflicting, mixed-version, or incomplete history is `Uncertain`.
4. Emulator convenience behavior never changes historical availability.

The earlier `TbcApi/api-registry.json` is retained temporarily for backward compatibility but is superseded by these catalogs. Validation rejects duplicates, missing evidence, incomplete records, wrong targets, and any supported record with a parsed introduction newer than 2.4.3.

Primary evidence currently includes the historical WoWWiki archive and the MOUZU version-pinned Blizzard 2.4.3 FrameXML repository. Fandom blocks direct automated retrieval, so indexed archive text is used only where an explicit version annotation is visible; ambiguity is recorded instead of guessed.
