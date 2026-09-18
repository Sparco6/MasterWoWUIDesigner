# Source patching

The mapper recognizes direct assignments from `CreateFrame` and literal arguments in `SetWidth`, `SetHeight`, `SetSize`, and the X/Y arguments of the five-argument `SetPoint` form. Each span records start, length, original text, property, and line.

Before changing source, the patcher verifies that the span still contains the exact original token. Multiple edits are applied from right to left. Expressions are not mapped and therefore cannot be overwritten by designer operations. After a patch the source is re-executed and remapped. Undo/redo stores source snapshots.

The next milestone should replace regex discovery with a Lua AST/token stream while retaining this exact-span contract.
