# AC8 PGM

Source repository for **AC8 PGM (Proportional Guided Missile)**, an ACE COMBAT 8 campaign-only resource override.

The project contains two components:

- `src/AC8AssetPatcher`: .NET tool for reading, editing, validating, and rebuilding the weapon resources.
- `src/AC8IoStoreLoader`: UE4SS native loader that mounts the generated IoStore override during game startup.

The repository intentionally excludes compiled binaries, game resources, AES keys, Oodle, extracted caches, and release payloads. See [BUILD.md](BUILD.md) for the complete build and reproduction procedure.

For a ready-to-paste compiled-code disclosure for Nexus Mods review, see [docs/NEXUS_REVIEW.md](docs/NEXUS_REVIEW.md).

The mod targets ACE COMBAT 8 version `1.1.2.0` and is intended only for offline single-player campaign use. Do not use it with multiplayer, online services, or leaderboards.

## Repository Layout

```text
src/AC8AssetPatcher/   Resource editor and IoStore build tool
src/AC8IoStoreLoader/   UE4SS C++ loader source
docs/                   Development report, release notes, and licenses
```

## License and Third-Party Code

The project contains vendored UAssetAPI and MinHook sources. Their original license files are preserved under `vendor`; see [docs/THIRD_PARTY.md](docs/THIRD_PARTY.md). The game and its original resources are not included.
