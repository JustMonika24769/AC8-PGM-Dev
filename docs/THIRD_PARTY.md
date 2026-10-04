# Third-party components

本项目的自有代码与第三方代码应分别处理许可。发布前请保留各依赖原始许可证，
并自行确认游戏模组分发符合游戏最终用户许可协议。

## Included source

- UAssetAPI 2.0.0, commit `3228c1e`, MIT License.
  <https://github.com/atenfyr/UAssetAPI>
- MinHook, BSD 2-Clause style license, included through the IoStoreLoaderMod
  reference tree.
- IoStoreLoaderMod reference license, MIT License.
  <https://github.com/akmubi/IoStoreLoaderMod>

对应许可证文件位于各自的 `vendor` 目录。

## Not bundled in the developer source tree

- RE-UE4SS / UE4SS, commit `e3ba1016`:
  <https://github.com/UE4SS-RE/RE-UE4SS>
- retoc 0.1.5:
  <https://github.com/trumank/retoc>
- aes-dumper-rs, commit `1c9017c`:
  <https://github.com/chadlrnsn/aes-dumper-rs>
- Epic/Oodle runtime library.

Oodle 属于 Epic/其许可方的专有组件，不应随公开源码包重新分发。开发者应从自己
合法拥有的环境取得可用库，并遵守相应许可。aes-dumper-rs 当前仓库没有随源码
提供明确许可证，因此本包记录固定提交但不重新分发其源码或二进制。

## Runtime notice

玩家包中的 `main.dll` 静态包含 MinHook 源码构建结果。MinHook 的许可证文本随
玩家包提供于 `THIRD_PARTY_LICENSES/MinHook-LICENSE.txt`。
