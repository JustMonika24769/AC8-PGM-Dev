# Nexus Mods Source Review Notice

AC8 PGM includes compiled code in the released player package: a UE4SS native loader DLL and a .NET resource tool. The complete source code and build instructions are available in this repository.

## Source

Replace the placeholder below with the public repository URL before contacting Nexus Mods support:

```text
https://github.com/<account>/AC8PGM
```

## Build Summary

1. Install .NET 10 SDK and Visual Studio 2019 x64 C++ Build Tools.
2. Obtain the legally required external tools listed in `docs/THIRD_PARTY.md`.
3. Copy `src/AC8AssetPatcher/config.example.json` to `config.json` and set the local game path.
4. Run `src/AC8IoStoreLoader/build.cmd` to build `main.dll` and copy it to the resource tool assets directory.
5. Run `dotnet restore`, `dotnet build --configuration Release`, and `dotnet publish --configuration Release --output dist` in `src/AC8AssetPatcher`.
6. Run `prepare-mappings`, start ACE COMBAT 8 to the main menu once, then run `build` and `verify`.

The repository does not include the game, extracted game assets, AES keys, Oodle, UE4SS, or compiled release artifacts.
