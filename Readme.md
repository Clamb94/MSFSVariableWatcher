# MSFS Variable Watcher

## Monitor LVARs

This tool helps you find LVARs for MSFS quick and easy.

Simply run `MSFSVariableWatcher.exe` and visit `http://localhost:7672/` in a web browser of your choice.
Make sure to have MSFS running _before_ launching MSFSVariableWatcher (the FSUIPC connection is established once at startup).

By default, it only shows LVARs that have recently changed.

To monitor SimVars, I recommend the SimvarWatcher included in the MSFS SDK.

Download: [Releases](https://github.com/Clamb94/MSFSVariableWatcher/releases)

![MSFSVariableWatcher](https://github.com/Clamb94/MSFSVariableWatcher/assets/17512695/9829ec03-bc35-41e0-9ca4-fbc9afc68afa)

## Features

- Search LVARs by name
- Hide unchanged / keep-changed-once filtering
- Lock list: freeze the set of rows while values keep updating
- Blacklist LVARs to remove noise
- Write a value back to an LVAR (Set button)

## Requirements

- Windows x64
- Microsoft Flight Simulator running, with FSUIPC + the WAPID WASM module active
- No .NET install needed: releases are self-contained single-file executables

## Build from source

```
dotnet restore
dotnet build
dotnet run --project MSFSVariableWatcher
```

The UI is served at `http://localhost:7672`.

Standalone publish (self-contained, no .NET runtime required on the target machine):

```
dotnet publish MSFSVariableWatcher -r win-x64 -o publish -p:PublishSingleFile=true --self-contained true
```

The output folder contains `MSFSVariableWatcher.exe` plus the native `FSUIPC_WAPID.dll` (ship them together).

## Credits & license

This tool uses the FSUIPC Client DLL for .NET by Paul Henty.

Licensed under the Apache License 2.0. See [LICENSE](LICENSE).
