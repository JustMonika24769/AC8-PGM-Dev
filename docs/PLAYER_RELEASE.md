# AC8 PGM 1.0.0

`AC8 PGM`（`Proportional Guided Missile`，比例制导导弹）是 ACE COMBAT 8 的资源覆盖模组。此版本直接覆盖武器蓝图与数据表，不使用 UObject 轮询逻辑。

## 要求

- ACE COMBAT 8 版本 1.1.2.0（2026-10-04 验证）
- UE4SS 3.0.1 Beta #0（提交 `e3ba1016`）
- Windows PowerShell 5.1 或 PowerShell 7

## 先安装 UE4SS

1. 从 UE4SS 官方发布页取得与 UE5.4 x64 兼容的 `3.0.1 Beta #0`（提交
   `e3ba1016`）压缩包。不要混用其他游戏的 UE4SS 文件或未知 nightly 版本。
2. 退出游戏，并备份 `Game\Binaries\Win64` 下现有的 UE4SS 文件和
   `ue4ss\Mods` 目录。
3. 按 UE4SS 压缩包原有目录结构，将内容解压到：

   ```text
   ACE COMBAT 8\Game\Binaries\Win64\
   ```

   不要把压缩包再套一层目录。安装后应能看到
   `Game\Binaries\Win64\ue4ss\Mods`，以及 UE4SS 压缩包提供的启动加载文件。
4. 启动一次游戏到主菜单后退出，使 UE4SS 完成初始化；确认
   `ue4ss\Mods\mods.txt` 已生成，再安装本模组。

如果 UE4SS 没有正确安装，`Install.cmd` 会提示找不到 `ue4ss\Mods`，此时不要
手动创建空目录冒充 UE4SS。

## EAC 与启动方式

UE4SS 原生加载器与 Easy Anti-Cheat（EAC）不应在同一进程中运行。发布包提供了
与当前游戏版本匹配的 EAC 空客户端配置和 `steam_appid.txt`，只用于单机战役离线启动。

1. 先运行 `EnableOffline.cmd`。脚本会把 `offline\EasyAntiCheat\AC8PGM_Offline.json`
   复制到游戏的 `EasyAntiCheat` 目录，并把 `offline\Game\Binaries\Win64\steam_appid.txt`
   复制到 `Game\Binaries\Win64`；已有文件会自动备份。
2. 在 Steam 的本游戏启动选项中加入以下内容（`%command%` 必须保留，由 Steam 展开）：

   ```text
   cmd /d /c "set EOS_USE_ANTICHEATCLIENTNULL=1&& %command% -anticheat_settings=AC8PGM_Offline.json"
   ```

3. 只从该启动项进入单机战役。完成后退出游戏并运行 `DisableOffline.cmd`，脚本会校验文件未被
   其他程序修改，再恢复原文件。

- 不要使用普通的 EAC 启动项加载本模组，也不要把上述参数用于联网功能。
- 不要在多人、联网活动、排行榜或其他在线服务中启动本模组。
- 不要删除、替换、重命名 `EasyAntiCheat` 目录或修改 EAC 二进制；
  `EasyAntiCheat_EOS_Setup.exe` 只用于安装/修复 EAC，不是禁用开关。
- 如果当前游戏版本不接受该空客户端配置，或启动项无法由 Steam 展开 `%command%`，请不要使用本模组。

完成战役后，运行 `DisableOffline.cmd` 并移除该 Steam 启动项，再恢复普通的带 EAC 启动方式。
该方法只让本次离线进程使用 EAC 空客户端，不删除、替换或修改 EAC 二进制，也不适用于在线服务。

## 运行方式与使用范围

本模组不是把资源写回游戏原始 `pakchunk0-Windows.*`。修改后的资源保存在独立的
IoStore 覆盖容器中，UE4SS 只负责在游戏启动时加载 AC8 专用原生挂载器，让游戏
以更高优先级读取这个容器。没有 UE4SS 时，覆盖容器不会自动生效；不要把文件
手动复制到 `Paks`、`~mods` 或 `LogicMods`。

本模组仅支持单机战役和离线测试。请在以下场景禁用并运行 `Uninstall.cmd`：

- 多人、联机、联网活动或排行榜模式
- 任何需要在线服务验证的启动方式
- 与其他会修改武器、制导或 IoStore 挂载的模组同时使用

该模组没有经过多人环境、反作弊环境或在线服务兼容性测试，不应尝试绕过游戏的
在线限制或安全检查。

## 安装

1. 退出游戏。
2. 卸载干净其他比例制导模组及其 UE4SS 文件，再继续安装本模组。
3. 解压完整压缩包，不要只取其中的 DLL。
4. 双击 `Install.cmd`。
5. 如果脚本未找到游戏，在命令行指定游戏路径，例如：

```powershell
.\Install.ps1 -GamePath "D:\SteamLibrary\steamapps\common\ACE COMBAT 8"
```

安装器只增改 `mods.txt` 中的本模组条目，不覆盖其他模组配置。卸载其他比例制导模组时，
应使用其自带卸载程序；不要把旧模组文件与本包文件混装。

安装前请确认游戏已退出，并确认游戏版本为 `1.1.2.0`。建议先备份
`Game\Binaries\Win64\ue4ss\Mods\mods.txt` 和现有 UE4SS 模组目录。

## 卸载

退出游戏后双击 `Uninstall.cmd`。卸载器只删除本包安装的容器和加载器，不修改游戏原始 `pakchunk`。

## 当前配置

玩家：QAAM 1.00、SAAM/2AAM 0.95、LAAM/SASM/4AAM/6AAM 0.90、
HVAA 0.85、HCAA/MSL 0.80、HPAA 0.75。

敌方：QAAM/SAAM/LAAM/2AAM 0.30、MSL/4AAM/6AAM/8AAM/HCAA/HVAA/SASM
0.25、HPAA 0.20。

## 排错

运行日志：

`Game\Binaries\Win64\ue4ss\Mods\IoStoreLoaderMod\AC8IoStoreLoader.log`

正常日志应包含 `custom mount code=0 message=OK`。游戏更新后若签名不再唯一，加载器会拒绝补丁；此时需要适配新版本，不能强行复用旧 DLL。

如果启动后出现异常，请退出游戏并运行 `Uninstall.cmd`；也可以将
`IoStoreLoaderMod : 0` 写入 `ue4ss\Mods\mods.txt` 作为紧急停用措施。模组不修改
原始游戏容器，但仍可能与其他 UE4SS 模组或未来游戏更新产生冲突。

技术说明和源码在单独的 Developer 压缩包中。底层 UE4SS 加载器条目仍叫
`IoStoreLoaderMod`，这是加载器模块名，不是模组展示名。
