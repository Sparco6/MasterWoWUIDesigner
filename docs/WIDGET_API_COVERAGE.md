# Widget API coverage

Current versioned `Data/TBC243/widgets.json` contains 20 curated method entries. The real corpus scanner currently observes 1,586 distinct colon-call names; that larger number includes Lua/string methods and custom/server objects and therefore is evidence to classify, not a count of native widgets.

| Declared owner | Catalog entries |
|---|---:|
| UIObject | 1 |
| Region | 7 |
| Frame | 6 |
| Texture | 1 |
| FontString | 1 |
| EditBox | 1 |
| ScrollFrame | 1 |
| Slider | 1 |
| StatusBar | 1 |

Inherited methods are represented by `ownerType` and `baseType`; they should not be duplicated for every derived widget. Historical Widget API import added zero entries because the requested index was unavailable. Direct extracted-client and addon evidence remain authoritative.
