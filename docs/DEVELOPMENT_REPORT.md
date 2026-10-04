# AC8 PGM（Proportional Guided Missile）开发简报

## 1. 目标与结论

旧实现通过 UE4SS Lua 在运行时查找、遍历和修改武器 UObject。快速切换机体时，
对象会密集创建和销毁，这条路径容易产生卡顿、无效引用或 UE4SS 异常。新的实现将
`HomingForesightAmount` 在游戏启动前写入资源，再以高优先级 IoStore 容器覆盖原
资源。运行阶段没有逐帧脚本、对象轮询或 `ProcessEvent` 钩子。

最终方案已在 ACE COMBAT 8 版本 1.1.2.0 和 UE4SS 3.0.1 Beta #0 上验证：容器挂载
返回 `0 / OK`，70 个目标资源可反向提取并与构建清单逐字节匹配，游戏内实际读取
到配置值。

## 2. 资源修改链

1. 用 retoc 和游戏 AES 密钥将目标 UE5.4 Zen 资源转成 Legacy 资源。
2. 用 `Mappings.usmap` 配合 UAssetAPI 结构化读取 `.uasset/.uexp`。
3. 修改 68 个玩家/NPC 武器蓝图 CDO 的 `HomingForesightAmount`。
4. 修改 `DT_WeaponSettings` 与 `DT_NPC_Weapon` 中对应武器行；玩家表为
   C.STORY/EASY 引用建立独立行，避免共享行连带修改。
5. 用 retoc 将 Legacy 资源重新转换成 UE5.4 Zen IoStore 容器。
6. 执行容器校验、Zen -> Legacy 反向提取和 70 个资源载荷哈希比对。

`DT_WpnPilotRankConfig_Missile` 中相关字段均为全局倍率 `1.0`，因此不修改。

## 3. 为什么不能只放入 Paks

将自定义 `.utoc/.ucas/.pak` 放入 `Paks`、`Paks\~mods` 或 `Paks\LogicMods`
并不能可靠生效。使用标准 pakchunk 命名后游戏会尝试扫描，但 ACE COMBAT 8 版本 1.1.2.0 对无签名
IoStore 容器返回 `Missing signature`。面向 UE4.27 的公开 IoStoreLoaderMod 也因
UE5.4 内部函数和 ABI 变化而无法定位旧签名。

因此本项目的准确定位是“静态资源覆盖 + 启动期挂载器”，而不是“直接替换原始
游戏文件”。资源修改和重打包在离线阶段完成；UE4SS 只提供原生 `main.dll` 的
启动入口，加载器随后调用游戏自己的 IoStore 挂载函数。没有 UE4SS 或其他等价的
启动注入机制时，覆盖容器只是未被发现的文件，不会生效。

## 4. 启动加载器

加载器是最小 UE4SS C++ 模组：

- 导出 `start_mod` 和 `uninstall_mod`，使用 MinHook。
- 唯一匹配 ACE COMBAT 8 版本 1.1.2.0 EXE 内的 UE5.4 IoStore 挂载函数；当前验证地址为
  `0x1427E4AA0`，仅用于说明，代码实际使用字节签名扫描。
- 观察官方挂载过程，在 `pakchunk0-Windows.utoc` 成功后，以 order `1000`
  同步挂载自定义容器。
- UE5.4 挂载 ABI 为：dispatcher、状态缓冲区、UTOC 路径、order、GUID、key。
- 状态对象至少需要 `0x180` 字节；旧 UE4.27 的 `0x104` 假设会破坏栈。
- 仅在自定义挂载调用期间把签名要求分支临时改为无条件跳转，调用返回后立即恢复
  原字节；官方容器仍使用原校验路径。
- 所有关键签名必须恰好匹配一次，否则加载器失败关闭并记录日志。

该设计依然依赖游戏内部实现，因此游戏更新后必须重新验证签名、ABI 和状态对象
大小。它比常驻 Lua 修改稳定，但不是跨版本永久兼容接口。

该实现不改变原始 `pakchunk0-Windows.*`，但它仍然是进程内原生加载器。因此发布
和测试范围应限定为单机战役/离线环境；不要在多人、联网活动、排行榜或反作弊环境
中启用，也不要把它描述为在线兼容或反作弊安全。

## 5. 发布结构

玩家包包含：

```text
Install.cmd / Install.ps1
Uninstall.cmd / Uninstall.ps1
payload/
  dlls/main.dll
  AC8PGMDirect/
    AC8PGMDirect_P.utoc
    AC8PGMDirect_P.ucas
    AC8PGMDirect_P.pak
    AC8PGMDirect_P.build.json
```

安装目标为：

```text
Game/Binaries/Win64/ue4ss/Mods/IoStoreLoaderMod/
```

安装器只更新 `mods.txt` 中的 `IoStoreLoaderMod` 和映射生成器；使用前应先卸载干净其他比例制导模组
状态，不应整体覆盖用户的 `mods.txt`。

发布前置条件还包括 UE4SS 3.0.1 Beta #0（`e3ba1016`）。UE4SS 应按官方压缩包
原结构安装到 `Game/Binaries/Win64`，并至少成功初始化一次以生成 `ue4ss/Mods`
和 `mods.txt`。本项目不把 UE4SS 本体重新打包进玩家压缩包。

EAC 是独立的在线安全组件。发布包提供 `AC8PGM_Offline.json` 和
`steam_appid.txt`，由 `EnableOffline.cmd` 复制并备份；Steam 启动项使用：

```text
cmd /d /c "set EOS_USE_ANTICHEATCLIENTNULL=1&& %command% -anticheat_settings=AC8PGM_Offline.json"
```

该方式只用于单机战役，测试完成后运行 `DisableOffline.cmd` 恢复原文件并移除启动项。
不要修改或删除 EAC 二进制，也不要在多人、联网或排行榜功能中使用。开发复现前应先
卸载干净其他比例制导模组。

## 6. 复现构建

`AC8AssetPatcher` 需要 .NET 10 SDK、retoc 0.1.5、UAssetAPI 2.0.0、
aes-dumper-rs、`Mappings.usmap` 和可供 retoc 使用的 Oodle 9 库。
`AC8IoStoreLoader` 使用 Visual Studio 2019 x64 C++ 工具链和 MinHook。

不要在公开源码包中提交 AES 密钥、游戏原始资源、提取缓存、日志、PDB、Process
Monitor 跟踪或 Epic 的 Oodle 二进制。开发者应从自己的合法游戏副本生成映射和
构建产物，并遵守游戏及第三方组件的许可条款。

核心命令：

```text
AC8AssetPatcher doctor
AC8AssetPatcher prepare-mappings
AC8AssetPatcher build
AC8AssetPatcher install
AC8AssetPatcher verify
```

## 7. 移植检查表

从 ACE COMBAT 8 版本 1.1.2.0 升级到其他版本后，依次检查：游戏版本、AES 候选密钥、USMAP 可读性、70 个目标资源路径、
属性类型、数据表行布局、Zen 往返一致性、挂载函数唯一签名、签名分支唯一签名、
挂载 ABI、状态缓冲区写入上界以及实际游戏内数值。任何一项不确定时都应失败关闭，
而不是继续写内存。
