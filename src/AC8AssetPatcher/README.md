# AC8AssetPatcher

`AC8 PGM`（`Proportional Guided Missile`）的离线资源覆盖构建工具。它将配置中的比例制导值写入武器蓝图和武器设置数据表，再生成独立的 UE5.4 IoStore 覆盖包。

当前已验证游戏版本：`ACE COMBAT 8 1.1.2.0`。加载器签名和资源映射不保证兼容其他版本。

它不会修改 `pakchunk0-Windows.*`。安装文件只放在 UE4SS 模组目录：

`ACE COMBAT 8\Game\Binaries\Win64\ue4ss\Mods\IoStoreLoaderMod\`

其中 AC8 专用 C++ 加载器在官方 `pakchunk0-Windows.utoc` 挂载完成后，以更高优先级挂载覆盖容器。它只在启动阶段工作，不扫描 UObject，不挂 `ProcessEvent`，也不执行每帧逻辑。

这里的“资源覆盖”不是直接改写游戏原始 pak 文件：覆盖资源位于独立 IoStore 容器中，
UE4SS 仅作为启动宿主加载原生挂载器。没有 UE4SS 时，容器不会被游戏自动发现；
UE4SS 也不会参与战斗逻辑或逐帧修改 UObject。

玩家发布包仅面向单机战役和离线测试。多人、联网活动、排行榜或其他在线服务场景
必须停用；当前方案没有经过在线/反作弊兼容性验证。

## UE4SS 与 EAC 前置条件

发布包不是独立注入器，安装前必须先安装兼容的 UE4SS 3.0.1 Beta #0，并让它在
`Game\Binaries\Win64\ue4ss\Mods` 创建基础目录和 `mods.txt`。UE4SS 压缩包应按
原目录结构解压到 `Game\Binaries\Win64`，不要套用额外目录层级。

UE4SS 原生加载器不应与 EAC 同进程运行。安装前请卸载干净其他比例制导模组。
玩家应先运行发布包的 `EnableOffline.cmd`，再将以下内容设置为 Steam 启动项，仅进入单机战役：

```text
cmd /d /c "set EOS_USE_ANTICHEATCLIENTNULL=1&& %command% -anticheat_settings=AC8PGM_Offline.json"
```

战役结束后运行 `DisableOffline.cmd` 并移除启动项。脚本只复制和恢复空客户端配置、
`steam_appid.txt`，不删除或修改 EAC 二进制，也不适用于多人、联网或排行榜功能。

## 已处理的覆盖链

- 玩家与 NPC 武器蓝图 CDO：`HomingForesightAmount`
- 玩家 `DT_WeaponSettings`：为 C.STORY/EASY 引用创建专属行，并更新 `qaam_ON`
- NPC `DT_NPC_Weapon`：更新名称与配置武器类型匹配的行
- `DT_WpnPilotRankConfig_Missile` 不修改：五个难度行的该字段均为倍率 `1.0`

这样不会依赖 UE4SS 在后台轮询 UObject，也不会在选机时执行 `FindObject` / `FindAllOf`。

## 修改配置并安装

编辑同目录的 `config.json`，然后在 PowerShell 中运行：

```powershell
.\dist\AC8AssetPatcher.exe install
```

配置规则：

- `player` 和 `enemy` 分开设置。
- 只列出的类型会被修改；删除某项即可保持原版。
- 数值是最终 `HomingForesightAmount`，不是额外加成。
- `aesKey` 建议保留 `auto`，游戏更新后会重新扫描并验证密钥。

## 命令

```text
doctor             检查游戏、工具、AES 和 USMAP
prepare-mappings   安装一次性 UE4SS 类型映射生成器
build              构建并完整验证，但不安装
install            构建、验证并安装加载器与 IoStore 载荷
verify             验证当前已安装包及 70 个资源载荷
uninstall          移除本工具安装的覆盖包
```

本目录已经生成 `Mappings.usmap`。只有游戏大版本更新、旧映射不再可用时，才需要再次执行 `prepare-mappings`，启动游戏到主菜单，退出后再执行 `install`。

安装前请先卸载干净其他比例制导模组，再运行 `install`。`install` 只启用本项目的
`IoStoreLoaderMod` 和停用一次性映射生成器；`uninstall` 只移除本项目的载荷和加载器。

## 验证方式

构建过程会依次执行：

1. UAssetAPI 结构化写入并逐资源数值回读。
2. retoc Legacy -> Zen 打包及容器校验。
3. Zen -> Legacy 反向提取。
4. 比较 68 个蓝图和 2 个数据表的 `.uexp` SHA-256，要求与已通过数值校验的载荷逐字节一致。

## 组件

- retoc 0.1.5，用于 UE5 IoStore 转换与校验。
- UAssetAPI 2.0.0 源码提交 `3228c1e`，用于结构化读取和写入 Unreal 资源。
- aes-dumper-rs，用于游戏更新后的 AES 候选扫描。
- Epic Oodle `oo2core_9_win64.dll`，用于解压游戏容器。

相关第三方许可证保留在 `tools`、`vendor` 目录中。
