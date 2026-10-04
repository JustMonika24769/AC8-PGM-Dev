# Build and Reproduction Guide

This guide describes how to build AC8 PGM from source for ACE COMBAT 8 `1.1.2.0`.

## Prerequisites

- Windows x64
- .NET 10 SDK
- Visual Studio 2019 Build Tools with the x64 C++ toolchain
- UE4SS 3.0.1 Beta #0 (`e3ba1016`) installed in the game directory
- A legal local copy of ACE COMBAT 8 version `1.1.2.0`
- `retoc.exe` 0.1.5
- `aes-dumper.exe` at the pinned commit listed in `docs/THIRD_PARTY.md`
- Epic/Oodle `oo2core_9_win64.dll`, obtained from the developer's own legal game installation

The repository does not redistribute UE4SS, retoc, aes-dumper-rs, Oodle, game resources, AES keys, or compiled DLL/EXE files.

## Prepare the Tree

1. Copy `src/AC8AssetPatcher/config.example.json` to `src/AC8AssetPatcher/config.json`.
2. Edit `config.json` and set `gamePath` to the local ACE COMBAT 8 directory.
3. Put the required external tools in `src/AC8AssetPatcher/tools/`:
   `retoc.exe`, `aes-dumper.exe`, and `oo2core_9_win64.dll`.
4. Make sure the game's original IoStore files are present under `Game/Content/Paks`.

## Build the UE4SS Loader

Open a developer command prompt or use a shell with the Visual Studio x64 tools available:

```bat
cd src\AC8IoStoreLoader
build.cmd
```

The build produces `main.dll` and copies it to
`src/AC8AssetPatcher/assets/AC8IoStoreLoader/main.dll`. This file is a local build artifact and is ignored by Git.

## Build the Resource Tool

```powershell
cd src\AC8AssetPatcher
dotnet restore
dotnet build --configuration Release
dotnet publish --configuration Release --output dist --no-restore
```

The published executable is written to `src/AC8AssetPatcher/dist/AC8AssetPatcher.exe`.

## Generate Mappings and Build the Mod

Run the following from `src/AC8AssetPatcher`:

```powershell
.\dist\AC8AssetPatcher.exe doctor
.\dist\AC8AssetPatcher.exe prepare-mappings
```

Start the game with UE4SS, reach the main menu, wait for the mapping dump to finish, and exit the game. Then run:

```powershell
.\dist\AC8AssetPatcher.exe build
```

`build` performs resource validation, IoStore conversion, and payload verification. To install the locally built payload into UE4SS, use:

```powershell
.\dist\AC8AssetPatcher.exe install
.\dist\AC8AssetPatcher.exe verify
```

Before testing, uninstall other proportional-guidance mods. Use the documented offline EAC configuration from the player release package, and restrict testing to the single-player campaign.

## Release Packaging

The player package is assembled from the validated files under the release workspace. Do not commit `bin`, `obj`, `dist`, `work`, `Mappings.usmap`, extracted game assets, or compiled `main.dll`/`.exe` files. Recalculate all SHA-256 files after packaging.

For architecture details and compatibility checks, see [docs/DEVELOPMENT_REPORT.md](docs/DEVELOPMENT_REPORT.md).
