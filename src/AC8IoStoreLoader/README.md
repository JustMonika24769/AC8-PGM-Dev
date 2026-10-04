# AC8 IoStore Loader

ACE COMBAT 8 版本 1.1.2.0 UE5.4 的启动期 IoStore 覆盖容器加载器。它由
`AC8AssetPatcher` 自动安装，不需要手工复制文件。

## 工作方式

- 通过 UE4SS 原生模组接口加载，不运行 Lua，也不遍历 UObject。
- 定位并挂钩游戏的 IoStore 挂载函数，在官方 `pakchunk0-Windows.utoc`
  挂载完成后加载 `AC8PGMDirect`（Proportional Guided Missile）。
- 只在自定义容器的同步挂载调用期间临时允许无签名容器，调用返回后立即恢复
  原始指令；官方容器仍按游戏原有逻辑校验。
- 签名扫描必须得到唯一匹配，否则拒绝修改并写日志，避免游戏更新后误补丁。

运行日志位于：

`ACE COMBAT 8/Game/Binaries/Win64/ue4ss/Mods/IoStoreLoaderMod/AC8IoStoreLoader.log`

资源容器位于：

`ACE COMBAT 8/Game/Binaries/Win64/ue4ss/Mods/IoStoreLoaderMod/AC8PGMDirect/`

## 构建

需要 Visual Studio 2019 C++ 工具链。运行：

```bat
build.cmd
```

生成的 `main.dll` 会同时同步到 `AC8AssetPatcher` 的打包资源目录。正式安装和
校验请使用：

```powershell
..\AC8AssetPatcher\dist\AC8AssetPatcher.exe install
..\AC8AssetPatcher\dist\AC8AssetPatcher.exe verify
```

加载器中的地址签名针对 ACE COMBAT 8 版本 1.1.2.0。游戏更新后如果日志显示签名没有唯一匹配，
需要重新分析签名并重新构建，不能强行继续使用。
