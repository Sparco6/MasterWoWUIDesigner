# MasterWoW UI Studio

An early, working WPF vertical slice of a visual Lua UI development environment for World of Warcraft: The Burning Crusade 2.4.3 (build 8606).

## Requirements and launch

- Windows 10/11
- .NET 6 SDK (the architecture is ready to retarget to .NET 8 when an 8.x SDK is installed)
- Visual Studio 2022 is optional

```powershell
dotnet restore MasterWoW.UIStudio.sln
dotnet run --project MasterWoW.UIStudio.App
```

Open a client-side `.lua` file, edit it, and press Ctrl+R. The sandboxed runtime builds a WoW-style object model; WPF renders that model. Select objects in the tree or canvas. Geometry changes patch only proven numeric literals. Save writes the edited source.

## Safety

MoonSharp runs with `Preset_SoftSandbox`; filesystem, process, registry, network, and native library access are not exposed. Lua is protected at execution time by coroutine instruction budgets, cancellation, and elapsed-time limits. Source text is never rejected merely for containing loop syntax such as `while true do`.

## Known limitations

This is the requested first vertical slice, not complete Phase 1. Rendering is geometric and thematic; WoW textures/BLP decoding, full backdrop behavior, configurable AIO responses, full AST mapping, and file encoding preservation remain future work. See `docs/IMPLEMENTATION_STATUS.md`.

## Adding APIs

Add model state to `UiModel.cs`, expose the compatible method through `LuaUiObject`, classify it in `TbcApi/api-registry.json`, render it without coupling Lua objects to WPF, and add a runtime/model test.
## Building reliably

Build from the solution so every executable receives the Core assembly produced from the same source revision:

```powershell
dotnet clean .\MasterWoW.UIStudio.sln -m:1
dotnet restore .\MasterWoW.UIStudio.Core\MasterWoW.UIStudio.Core.csproj
dotnet restore .\MasterWoW.UIStudio.App\MasterWoW.UIStudio.App.csproj
dotnet restore .\MasterWoW.UIStudio.Tests\MasterWoW.UIStudio.Tests.csproj
dotnet restore .\MasterWoW.UIStudio.Corpus\MasterWoW.UIStudio.Corpus.csproj
dotnet build .\MasterWoW.UIStudio.sln --no-restore --no-incremental -m:1
dotnet test .\MasterWoW.UIStudio.Tests\MasterWoW.UIStudio.Tests.csproj --no-build
```

Run the application from:

```text
MasterWoW.UIStudio.App\bin\Debug\net6.0-windows\MasterWoW.UIStudio.App.exe
```

Do not manually copy `MasterWoW.UIStudio.Core.dll` between output folders. The App, tests, and corpus utility use `ProjectReference`; building the solution copies a matching Core assembly and dependency set automatically. If output provenance is uncertain, close running Studio/corpus/test processes, delete the projects' generated `bin` and `obj` folders, and rebuild the solution.
