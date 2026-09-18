# GitHub source package

This directory is a clean source tree for MasterWoW UI Studio. It excludes generated build output, local caches, editor state, backup files, and Studio-only layout overrides.

## Build

Install the .NET 6 SDK on Windows, then run:

```powershell
dotnet restore .\MasterWoW.UIStudio.sln
dotnet build .\MasterWoW.UIStudio.sln -c Release --no-restore
dotnet test .\MasterWoW.UIStudio.Tests\MasterWoW.UIStudio.Tests.csproj -c Release --no-build
```

Run the application from:

```text
MasterWoW.UIStudio.App\bin\Release\net6.0-windows\MasterWoW.UIStudio.App.exe
```

The MasterWoW Developer Kit runtime API inventory is included at `Data/DeveloperKit/APIwithoutaddons.txt`. It records observed function availability, not complete signatures.

## Before publishing

Select an appropriate project license and add its `LICENSE` file. No project-level license was present when this source package was prepared.

