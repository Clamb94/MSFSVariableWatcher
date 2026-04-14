# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project purpose

MSFSVariableWatcher is a small Blazor Server app that exposes a local web UI for inspecting LVARs (local aircraft variables) from a running Microsoft Flight Simulator instance. It talks to MSFS through the FSUIPC Client DLL + WAPID WASM module. MSFS must be running *before* the app is launched, because the FSUIPC connection is established once at startup.

## Commands

Build / run (from repo root):

```
dotnet restore
dotnet build
dotnet run --project MSFSVariableWatcher
```

The web UI is served at `http://localhost:7672` (hard-coded in `Program.cs`).

Published single-file release build (matches the CI workflow in `.github/workflows/dotnet.yml`):

```
dotnet publish MSFSVariableWatcher -r win-x64 -o publish -p:PublishSingleFile=true --self-contained true
```

There are no tests in this repo.

## Version / framework caveat

- `MSFSVariableWatcher.csproj` targets `net9.0-windows10.0.17763.0`.
- `.github/workflows/dotnet.yml` installs `dotnet-version: 9.0.x`.

When touching the TFM or the workflow, update both together.

Also: the project is Windows-only (`-windows` TFM, x64 native `FSUIPC_WAPID.dll`). Do not try to build or run it on Linux/macOS.

## Architecture

The app is intentionally tiny — three moving parts:

1. **`Program.cs`** — calls `MSFSService.InitMSFSServices()` and `MSFSService.Start()` *before* building the web host. The FSUIPC connection is a process-wide singleton established at startup; if MSFS is not running when the .exe launches, `MSFSVariableServices.IsRunning` is false for the lifetime of the process and the UI shows a "NOT CONNECTED" message instead of data.

2. **`MSFSService.cs`** — thin static wrapper around `FSUIPC.MSFSVariableServices` (from the `FSUIPCClientDLL` NuGet package). Wires the log handler, calls `Init()`, then `Start()`. All LVAR state lives inside `MSFSVariableServices` — this project does not maintain its own cache of LVAR values.

3. **`Pages/Lvars.razor`** — the only real UI page (`@page "/"`). It:
   - Subscribes to `MSFSVariableServices.OnValuesChanged` to drive re-renders (via `StateHasChanged`) when `autoRefresh` is on.
   - Subscribes per-LVAR to `OnValueChanged` in `OnInitialized` to record a `lastChanged` timestamp per variable.
   - Uses `HasChanged(name)` (= changed in the last 5s) to both highlight rows and power the "Hide unchanged" filter.
   - Maintains an in-memory `blacklist` and `keepChangedSet` (session-scoped, not persisted).

The `FSUIPC_WAPID.dll` next to the csproj is a native dependency copied to the output directory on every build (`CopyToOutputDirectory=Always`). It must ship alongside the exe.

## Notes for future changes

- If you add new pages/services that read LVAR state, go through `FSUIPC.MSFSVariableServices` directly — don't introduce a second source of truth.
- `Lvars.razor` subscribes to every LVAR's `OnValueChanged` via `SubscribeNewLVars`, which is re-run on each `OnValuesChanged` event so LVARs discovered after page load are tracked too. The component implements `IDisposable` to detach those handlers when the circuit ends — any new shared state read on the Blazor thread and written from the FSUIPC callback thread must be thread-safe (see `lastChanged`, `subscribedLVars`, `keepChangedSet` using `ConcurrentDictionary`).
- The listening port (7672) is hard-coded via `builder.WebHost.UseUrls(...)`; `appsettings.json` URL config is effectively ignored.

## README (for user-facing context)

From `Readme.md`: users are expected to download a release, run `MSFSVariableWatcher.exe`, then open `http://localhost:7672/`. MSFS must already be running. For SimVars (as opposed to LVARs), the README points users at the SimvarWatcher bundled with the MSFS SDK instead of this tool.
