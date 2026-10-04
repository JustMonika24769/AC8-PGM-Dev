using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;
using UAssetAPI.Unversioned;

namespace AC8AssetPatcher;

internal static partial class Program
{
    private const string RetocVersion = "UE5_4";
    private const string PropertyName = "HomingForesightAmount";
    private const string PlayerSettingsPackage = "/Game/Datatables/PLAYER/DT_WeaponSettings";
    private const string EnemySettingsPackage = "/Game/Datatables/AI/NPC/Weapon/DT_NPC_Weapon";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            var root = FindToolRoot();
            var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "help";
            var configPath = GetOption(args, "--config") ?? Path.Combine(root, "config.json");
            var app = new App(root, configPath);
            return command switch
            {
                "doctor" => app.Doctor(),
                "prepare-mappings" => app.PrepareMappings(),
                "build" => app.Build(),
                "install" => app.Install(),
                "uninstall" => app.Uninstall(),
                "verify" => app.VerifyInstalled(),
                "help" or "--help" or "-h" => PrintHelp(),
                _ => throw new UserError($"未知命令: {command}。运行 AC8AssetPatcher help 查看用法。")
            };
        }
        catch (UserError ex)
        {
            Console.Error.WriteLine($"错误: {ex.Message}");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"未处理错误: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private static int PrintHelp()
    {
        Console.WriteLine("""
            AC8AssetPatcher - ACE COMBAT 8 导弹制导资源覆盖工具

            用法:
              AC8AssetPatcher doctor
              AC8AssetPatcher prepare-mappings
              AC8AssetPatcher build
              AC8AssetPatcher install
              AC8AssetPatcher verify
              AC8AssetPatcher uninstall

            所有命令可追加: --config <配置文件路径>

            首次使用:
              1. 运行 prepare-mappings，随后启动游戏并等到主菜单。
              2. 退出游戏，运行 install。

            install 会构建 IoStore 覆盖包并安装 AC8 专用启动挂载器；它不会改写游戏原始 pakchunk 文件。
            """);
        return 0;
    }

    private static string? GetOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        return null;
    }

    private static string FindToolRoot()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            var dir = new DirectoryInfo(start);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "tools", "retoc.exe")) &&
                    File.Exists(Path.Combine(dir.FullName, "tools", "aes-dumper.exe")))
                    return dir.FullName;
                dir = dir.Parent;
            }
        }
        throw new UserError("找不到 tools\\retoc.exe 和 tools\\aes-dumper.exe。请从完整工具目录运行。 ");
    }

    [GeneratedRegex("0x[A-Fa-f0-9]{64}")]
    private static partial Regex AesRegex();

    [GeneratedRegex("^\\s*\"(?<path>/Game/[^\"]+\\.Default__[^\"]+_C)\",?\\s*$", RegexOptions.Multiline)]
    private static partial Regex LuaPathRegex();

    private sealed class App
    {
        private readonly string _root;
        private readonly string _configPath;
        private readonly Config _config;
        private readonly string _game;
        private readonly string _paks;
        private readonly string _win64;
        private readonly string _work;
        private readonly string _tools;
        private readonly string _assets;

        public App(string root, string configPath)
        {
            _root = root;
            _configPath = Path.GetFullPath(configPath, Environment.CurrentDirectory);
            if (!File.Exists(_configPath)) throw new UserError($"配置文件不存在: {_configPath}");
            _config = JsonSerializer.Deserialize<Config>(File.ReadAllText(_configPath), JsonOptions)
                ?? throw new UserError("配置文件为空或无效。");
            var configDir = Path.GetDirectoryName(_configPath)!;
            _game = Path.GetFullPath(_config.GamePath, configDir);
            _paks = Path.Combine(_game, "Game", "Content", "Paks");
            _win64 = Path.Combine(_game, "Game", "Binaries", "Win64");
            _work = Path.Combine(_root, "work");
            _tools = Path.Combine(_root, "tools");
            _assets = Path.Combine(_root, "assets");
        }

        public int Doctor()
        {
            Console.WriteLine($"配置: {_configPath}");
            Console.WriteLine($"游戏: {_game}");
            RequireLayout();
            Console.WriteLine("[OK] 游戏目录与资源处理工具");
            var key = ResolveAesKey();
            Console.WriteLine($"[OK] IoStore AES 密钥: {MaskKey(key)}");
            var mapping = FindMappings(false);
            Console.WriteLine(mapping is null
                ? "[待处理] 尚未找到 Mappings.usmap，请运行 prepare-mappings。"
                : $"[OK] 类型映射: {mapping}");
            var manifest = ReadManifest();
            Console.WriteLine($"[OK] 武器蓝图清单: 玩家 {manifest.Count(x => x.Side == Side.Player)}，敌方 {manifest.Count(x => x.Side == Side.Enemy)}");
            ValidateCoefficients();
            Console.WriteLine("[OK] 配置中的制导系数均为有限数值");
            return mapping is null ? 3 : 0;
        }

        public int PrepareMappings()
        {
            RequireLayout();
            var mods = Path.Combine(_win64, "ue4ss", "Mods");
            if (!Directory.Exists(mods))
                throw new UserError("未找到 UE4SS Mods 目录；首次生成类型映射需要当前 UE4SS 安装。 ");

            var modDir = Path.Combine(mods, "AC8AssetMappingDumper");
            var scriptsDir = Path.Combine(modDir, "Scripts");
            Directory.CreateDirectory(scriptsDir);
            var manifest = ReadManifest();
            File.WriteAllText(Path.Combine(scriptsDir, "main.lua"), BuildMappingLua(manifest), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(modDir, "enabled.txt"), "", new UTF8Encoding(false));
            EnableMod(Path.Combine(mods, "mods.txt"), "AC8AssetMappingDumper");
            Console.WriteLine("已安装一次性类型映射生成器。");
            Console.WriteLine("现在启动游戏并停留到主菜单；看到 UE4SS 控制台提示 dump 完成后退出游戏。");
            Console.WriteLine("随后运行: AC8AssetPatcher install");
            return 0;
        }

        public int Build()
        {
            BuildPackage();
            return 0;
        }

        public int Install()
        {
            var utoc = BuildPackage();
            var mods = Path.Combine(_win64, "ue4ss", "Mods");
            if (!Directory.Exists(mods))
                throw new UserError("未找到 UE4SS Mods 目录；运行时挂载 IoStore 覆盖包需要 UE4SS。 ");
            var loaderSource = Path.Combine(_assets, "AC8IoStoreLoader", "main.dll");
            RequireFile(loaderSource, "AC8 UE5.4 IoStore 加载器");
            var loaderDir = Path.Combine(mods, "IoStoreLoaderMod");
            var targetDir = Path.Combine(loaderDir, "AC8PGMDirect");
            Directory.CreateDirectory(targetDir);
            foreach (var ext in new[] { ".utoc", ".ucas", ".pak" })
            {
                var source = Path.ChangeExtension(utoc, ext);
                RequireFile(source, "构建产物");
                var target = Path.Combine(targetDir, _config.ModName + ext);
                var temporary = target + ".installing";
                File.Copy(source, temporary, true);
                File.Move(temporary, target, true);
                Console.WriteLine($"安装: {target}");
            }
            var buildManifest = Path.Combine(Path.GetDirectoryName(utoc)!, "build-manifest.json");
            var installedManifest = Path.Combine(targetDir, _config.ModName + ".build.json");
            RequireFile(buildManifest, "构建清单");
            File.Copy(buildManifest, installedManifest, true);
            Console.WriteLine($"安装: {installedManifest}");
            var dllDir = Path.Combine(loaderDir, "dlls");
            Directory.CreateDirectory(dllDir);
            var loaderTarget = Path.Combine(dllDir, "main.dll");
            var loaderTemporary = loaderTarget + ".installing";
            File.Copy(loaderSource, loaderTemporary, true);
            File.Move(loaderTemporary, loaderTarget, true);
            Console.WriteLine($"安装: {loaderTarget}");
            EnableMod(Path.Combine(mods, "mods.txt"), "IoStoreLoaderMod");
            DisableMod(mods, "AC8AssetMappingDumper");
            Console.WriteLine("安装完成。AC8 PGM UE5.4 IoStore 启动挂载器已启用；原始游戏容器未修改。 ");
            return 0;
        }

        public int VerifyInstalled()
        {
            RequireLayout();
            var targetDir = Path.Combine(_win64, "ue4ss", "Mods", "IoStoreLoaderMod", "AC8PGMDirect");
            var target = Path.Combine(targetDir, _config.ModName + ".utoc");
            if (!File.Exists(target)) throw new UserError($"尚未安装覆盖包: {target}");
            Run(Path.Combine(_tools, "retoc.exe"), ["verify", target]);
            var manifestPath = Path.Combine(targetDir, _config.ModName + ".build.json");
            RequireFile(manifestPath, "安装清单");
            VerifyInstalledPayloads(target, manifestPath);
            return 0;
        }

        public int Uninstall()
        {
            var ue4ssMods = Path.Combine(_win64, "ue4ss", "Mods");
            var loaderDir = Path.Combine(ue4ssMods, "IoStoreLoaderMod");
            var modsDir = Path.Combine(loaderDir, "AC8PGMDirect");
            var removed = 0;
            foreach (var ext in new[] { ".utoc", ".ucas", ".pak", ".build.json" })
            {
                var path = Path.Combine(modsDir, _config.ModName + ext);
                if (File.Exists(path)) { File.Delete(path); removed++; }
            }
            var loader = Path.Combine(loaderDir, "dlls", "main.dll");
            if (File.Exists(loader)) { File.Delete(loader); removed++; }
            if (Directory.Exists(ue4ssMods)) DisableMod(ue4ssMods, "IoStoreLoaderMod");
            Console.WriteLine($"已移除 {removed} 个覆盖包/加载器文件并停用 IoStoreLoaderMod。原始游戏资源未改动。 ");
            return 0;
        }

        private void RequireLayout()
        {
            RequireFile(Path.Combine(_win64, "AceCombat8.exe"), "游戏可执行文件");
            RequireFile(Path.Combine(_paks, "pakchunk0-Windows.utoc"), "主 IoStore 索引");
            RequireFile(Path.Combine(_tools, "retoc.exe"), "retoc");
            RequireFile(Path.Combine(_tools, "aes-dumper.exe"), "AES 扫描器");
            RequireFile(Path.Combine(_tools, "oo2core_9_win64.dll"), "Oodle 解压库");
        }

        private static void RequireFile(string path, string label)
        {
            if (!File.Exists(path)) throw new UserError($"找不到{label}: {path}");
        }

        private string ResolveAesKey()
        {
            if (!_config.AesKey.Equals("auto", StringComparison.OrdinalIgnoreCase))
                return NormalizeKey(_config.AesKey);

            var exe = Path.Combine(_win64, "AceCombat8.exe");
            var result = Run(Path.Combine(_tools, "aes-dumper.exe"), [exe], capture: true);
            var candidates = AesRegex().Matches(result).Select(x => NormalizeKey(x.Value)).Distinct().ToArray();
            if (candidates.Length == 0) throw new UserError("未能从游戏程序中找到 AES 候选密钥。 ");
            var utoc = Path.Combine(_paks, "pakchunk0-Windows.utoc");
            foreach (var key in candidates)
            {
                var code = Run(Path.Combine(_tools, "retoc.exe"), ["--aes-key", key, "info", utoc], capture: true, allowFailure: true, out _);
                if (!code.Contains("error", StringComparison.OrdinalIgnoreCase) && code.Contains("pakchunk0", StringComparison.OrdinalIgnoreCase))
                    return key;
            }
            throw new UserError($"找到 {candidates.Length} 个 AES 候选值，但没有一个能解密 pakchunk0。 ");
        }

        private static string NormalizeKey(string key)
        {
            key = key.Trim();
            if (key.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) key = key[2..];
            if (key.Length != 64 || key.Any(c => !Uri.IsHexDigit(c)))
                throw new UserError("aesKey 必须是 32 字节十六进制值，或字符串 auto。 ");
            return "0x" + key.ToUpperInvariant();
        }

        private static string MaskKey(string key) => key[..10] + "..." + key[^8..];

        private string? FindMappings(bool required)
        {
            var candidates = new[]
            {
                Path.Combine(_root, "Mappings.usmap"),
                Path.Combine(_win64, "Mappings.usmap"),
                Path.Combine(_win64, "ue4ss", "Mappings.usmap"),
                Path.Combine(_win64, "ue4ss", "Mods", "Mappings.usmap")
            };
            var result = candidates.FirstOrDefault(File.Exists);
            if (result is null && Directory.Exists(_win64))
                result = Directory.EnumerateFiles(_win64, "Mappings.usmap", SearchOption.AllDirectories).FirstOrDefault();
            if (required && result is null)
                throw new UserError("找不到 Mappings.usmap。请先运行 prepare-mappings，启动游戏到主菜单后退出。 ");
            return result;
        }

        private List<WeaponAsset> ReadManifest()
        {
            var result = new List<WeaponAsset>();
            ReadManifestFile(Path.Combine(_assets, "weapon_classes.lua"), Side.Player, result);
            ReadManifestFile(Path.Combine(_assets, "npc_weapons.lua"), Side.Enemy, result);
            if (result.Count == 0) throw new UserError("武器蓝图清单为空。 ");
            return result.DistinctBy(x => x.CdoPath, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void ReadManifestFile(string path, Side side, List<WeaponAsset> output)
        {
            RequireFile(path, "武器蓝图清单");
            foreach (Match match in LuaPathRegex().Matches(File.ReadAllText(path)))
            {
                var cdo = match.Groups["path"].Value;
                var dot = cdo.IndexOf(".Default__", StringComparison.Ordinal);
                var package = cdo[..dot];
                var name = Path.GetFileName(package.Replace('/', Path.DirectorySeparatorChar));
                output.Add(new WeaponAsset(side, cdo, package + "." + name, package));
            }
        }

        private void ValidateCoefficients()
        {
            foreach (var (side, values) in new[] { ("player", _config.Player), ("enemy", _config.Enemy) })
                foreach (var (name, value) in values)
                    if (string.IsNullOrWhiteSpace(name) || double.IsNaN(value) || double.IsInfinity(value))
                        throw new UserError($"{side}.{name} 的系数不是有限数值。 ");
        }

        private string BuildPackage()
        {
            RequireLayout();
            ValidateCoefficients();
            var mapping = FindMappings(true)!;
            var key = ResolveAesKey();
            var targets = SelectTargets(ReadManifest());
            if (targets.Count == 0) throw new UserError("配置没有匹配到任何武器蓝图。 ");
            Console.WriteLine($"目标: 玩家 {targets.Count(x => x.Asset.Side == Side.Player)}，敌方 {targets.Count(x => x.Asset.Side == Side.Enemy)}，共 {targets.Count} 个蓝图");
            Console.WriteLine($"USMAP: {mapping}");
            Console.WriteLine($"AES: {MaskKey(key)}");

            var buildRoot = Path.Combine(_work, "build");
            var extracted = Path.Combine(buildRoot, "extracted");
            var staged = Path.Combine(buildRoot, "staged");
            var output = Path.Combine(buildRoot, "output");
            ResetDirectory(buildRoot);
            Directory.CreateDirectory(extracted);
            Directory.CreateDirectory(staged);
            Directory.CreateDirectory(output);

            foreach (var filter in new[] { "BP_plwp_", "BP_wp_", "DT_WeaponSettings", "DT_NPC_Weapon" })
            {
                Console.WriteLine($"提取: {filter}*");
                Run(Path.Combine(_tools, "retoc.exe"), ["--aes-key", key, "to-legacy", "--version", RetocVersion, "--filter", filter, "--no-shaders", _paks, extracted]);
            }
            var scriptObjects = Path.Combine(extracted, "scriptobjects.bin");
            if (File.Exists(scriptObjects)) File.Copy(scriptObjects, Path.Combine(staged, "scriptobjects.bin"), true);

            var mappings = new Usmap(mapping);
            var playerSettings = new SettingsTableEditor(
                Path.Combine(extracted, PackageToRelativeFile(PlayerSettingsPackage)), mappings, "PLAYER");
            var enemySettings = new SettingsTableEditor(
                Path.Combine(extracted, PackageToRelativeFile(EnemySettingsPackage)), mappings, "ENEMY");
            if (_config.Player.TryGetValue("qaam", out var playerQaam))
                playerSettings.SetExistingRow("qaam_ON", (float)playerQaam);
            enemySettings.SetMatchingRows(rowName => MatchSettingsRow(rowName, _config.Enemy));
            var changed = 0;
            foreach (var target in targets)
            {
                var relative = PackageToRelativeFile(target.Asset.PackagePath);
                var source = Path.Combine(extracted, relative);
                if (!File.Exists(source)) throw new UserError($"IoStore 提取结果中缺少目标蓝图: {target.Asset.PackagePath}");
                var destination = Path.Combine(staged, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                PatchAsset(source, destination, mappings, target.Value, target.Asset.CdoPath,
                    target.Asset.Side == Side.Player ? playerSettings : enemySettings);
                changed++;
                Console.WriteLine($"[{changed}/{targets.Count}] {target.Asset.Side.ToString().ToLowerInvariant()} {Path.GetFileNameWithoutExtension(source)} = {target.Value.ToString("G9", CultureInfo.InvariantCulture)}");
            }

            playerSettings.Write(Path.Combine(staged, PackageToRelativeFile(PlayerSettingsPackage)));
            enemySettings.Write(Path.Combine(staged, PackageToRelativeFile(EnemySettingsPackage)));
            var payloadPackages = targets.Select(x => x.Asset.PackagePath)
                .Append(PlayerSettingsPackage).Append(EnemySettingsPackage)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            var package = Path.Combine(output, _config.ModName + ".utoc");
            Console.WriteLine("打包 IoStore 覆盖容器...");
            Run(Path.Combine(_tools, "retoc.exe"), ["to-zen", staged, package, "--version", RetocVersion]);
            Run(Path.Combine(_tools, "retoc.exe"), ["verify", package]);
            VerifyPackage(package, payloadPackages, staged);
            WriteBuildManifest(output, targets, payloadPackages, package, staged);
            Console.WriteLine($"构建完成: {package}");
            return package;
        }

        private static float? MatchSettingsRow(string rowName, IReadOnlyDictionary<string, double> configured)
        {
            foreach (var key in configured.Keys.OrderByDescending(x => x.Length))
            {
                var token = key.Equals("msl", StringComparison.OrdinalIgnoreCase) ? "missile" : key;
                if (rowName.Contains(token, StringComparison.OrdinalIgnoreCase)) return (float)configured[key];
            }
            return null;
        }

        private List<PatchTarget> SelectTargets(IEnumerable<WeaponAsset> manifest)
        {
            var result = new List<PatchTarget>();
            foreach (var asset in manifest)
            {
                var configured = asset.Side == Side.Player ? _config.Player : _config.Enemy;
                var leaf = asset.CdoPath[(asset.CdoPath.LastIndexOf('/') + 1)..];
                var match = configured.Keys.OrderByDescending(x => x.Length)
                    .FirstOrDefault(x => leaf.Contains(x, StringComparison.OrdinalIgnoreCase));
                if (match is not null) result.Add(new PatchTarget(asset, (float)configured[match], match));
            }
            return result;
        }

        private static string PackageToRelativeFile(string packagePath)
        {
            const string prefix = "/Game/";
            if (!packagePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new UserError($"不支持的包路径: {packagePath}");
            return Path.Combine("Live", "Content", packagePath[prefix.Length..].Replace('/', Path.DirectorySeparatorChar) + ".uasset");
        }

        private static void PatchAsset(string source, string destination, Usmap mappings, float value, string cdoPath, SettingsTableEditor settings)
        {
            var asset = new UAsset(source, EngineVersion.VER_UE5_4, mappings);
            var expectedName = cdoPath[(cdoPath.LastIndexOf('.') + 1)..];
            var cdo = asset.Exports.OfType<NormalExport>().SingleOrDefault(x => x.ObjectName.ToString().Equals(expectedName, StringComparison.OrdinalIgnoreCase));
            if (cdo is null) throw new UserError($"蓝图中找不到 CDO {expectedName}: {source}");
            var existing = cdo[PropertyName];
            if (existing is not null && existing is not FloatPropertyData)
                throw new UserError($"{source} 中的 {PropertyName} 不是 float，而是 {existing.GetType().Name}");
            cdo[PropertyName] = new FloatPropertyData(FName.FromString(asset, PropertyName)) { Value = value };
            if (cdo["WeaponSettingsId_DifficultyMap"] is MapPropertyData settingsMap)
            {
                foreach (var entry in settingsMap.Value)
                {
                    if (entry.Value is not NamePropertyData rowName) continue;
                    var customRow = settings.GetOrAddRow(rowName.Value.ToString(), value);
                    rowName.Value = FName.FromString(asset, customRow);
                }
            }
            asset.ResolveAncestries();
            asset.Write(destination);
            VerifyAsset(destination, mappings, value, expectedName);
        }

        private static void VerifyAsset(string path, Usmap mappings, float expected, string cdoName)
        {
            var asset = new UAsset(path, EngineVersion.VER_UE5_4, mappings);
            var cdo = asset.Exports.OfType<NormalExport>().SingleOrDefault(x => x.ObjectName.ToString().Equals(cdoName, StringComparison.OrdinalIgnoreCase));
            var actual = (cdo?[PropertyName] as FloatPropertyData)?.Value;
            if (actual is null || Math.Abs(actual.Value - expected) > 0.00001f)
                throw new UserError($"回读校验失败: {path}，期望 {expected}，实际 {(actual?.ToString(CultureInfo.InvariantCulture) ?? "缺失")}");
        }

        private void VerifyPackage(string package, IReadOnlyCollection<string> packages, string expectedRoot)
        {
            var verifyRoot = Path.Combine(_work, "verify");
            ResetDirectory(verifyRoot);
            var input = Path.Combine(verifyRoot, "input");
            var extracted = Path.Combine(verifyRoot, "extracted");
            Directory.CreateDirectory(input);
            Directory.CreateDirectory(extracted);
            foreach (var ext in new[] { ".utoc", ".ucas" })
            {
                var global = Path.Combine(_paks, "global" + ext);
                RequireFile(global, "游戏全局 IoStore 文件");
                File.Copy(global, Path.Combine(input, "global" + ext), true);
            }
            foreach (var ext in new[] { ".utoc", ".ucas", ".pak" })
            {
                var built = Path.ChangeExtension(package, ext);
                RequireFile(built, "待验证覆盖包");
                File.Copy(built, Path.Combine(input, Path.GetFileName(built)), true);
            }
            Console.WriteLine("回读成品覆盖包...");
            Run(Path.Combine(_tools, "retoc.exe"), ["to-legacy", "--version", RetocVersion, "--no-shaders", input, extracted]);
            foreach (var packagePath in packages)
            {
                var file = Path.Combine(extracted, PackageToRelativeFile(packagePath));
                RequireFile(file, "成品中的资源");
                var expectedPayload = Path.ChangeExtension(Path.Combine(expectedRoot, PackageToRelativeFile(packagePath)), ".uexp");
                var actualPayload = Path.ChangeExtension(file, ".uexp");
                RequireFile(expectedPayload, "已通过数值校验的导出载荷");
                RequireFile(actualPayload, "成品中的导出载荷");
                if (!FileHash(expectedPayload).Equals(FileHash(actualPayload), StringComparison.OrdinalIgnoreCase))
                    throw new UserError($"成品载荷校验失败: {packagePath}");
            }
            Console.WriteLine($"[OK] 成品校验通过: {packages.Count} 个资源已数值回读，Zen 往返载荷逐字节一致");
        }

        private void WriteBuildManifest(string output, IEnumerable<PatchTarget> targets, IEnumerable<string> payloadPackages, string package, string stagedRoot)
        {
            var manifest = new
            {
                tool = "AC8AssetPatcher",
                property = PropertyName,
                config = Path.GetFileName(_configPath),
                package = Path.GetFileName(package),
                createdUtc = DateTimeOffset.UtcNow,
                assets = targets.Select(x => new
                {
                    side = x.Asset.Side.ToString().ToLowerInvariant(),
                    type = x.WeaponType,
                    value = x.Value,
                    asset = x.Asset.PackagePath
                }),
                payloads = payloadPackages.Select(x => new
                {
                    asset = x,
                    payloadSha256 = FileHash(Path.ChangeExtension(Path.Combine(stagedRoot, PackageToRelativeFile(x)), ".uexp"))
                })
            };
            File.WriteAllText(Path.Combine(output, "build-manifest.json"), JsonSerializer.Serialize(manifest, JsonOptions), new UTF8Encoding(false));
        }

        private void VerifyInstalledPayloads(string package, string manifestPath)
        {
            using var json = JsonDocument.Parse(File.ReadAllText(manifestPath));
            var expected = json.RootElement.GetProperty("payloads").EnumerateArray().ToDictionary(
                x => x.GetProperty("asset").GetString() ?? throw new UserError("安装清单包含空资源路径。"),
                x => x.GetProperty("payloadSha256").GetString() ?? throw new UserError("安装清单缺少 payloadSha256。"),
                StringComparer.OrdinalIgnoreCase);
            var verifyRoot = Path.Combine(_work, "verify-installed");
            var input = Path.Combine(verifyRoot, "input");
            var extracted = Path.Combine(verifyRoot, "extracted");
            ResetDirectory(verifyRoot);
            Directory.CreateDirectory(input);
            Directory.CreateDirectory(extracted);
            foreach (var ext in new[] { ".utoc", ".ucas" })
                File.Copy(Path.Combine(_paks, "global" + ext), Path.Combine(input, "global" + ext), true);
            foreach (var ext in new[] { ".utoc", ".ucas", ".pak" })
                File.Copy(Path.ChangeExtension(package, ext), Path.Combine(input, Path.GetFileName(Path.ChangeExtension(package, ext))), true);
            Run(Path.Combine(_tools, "retoc.exe"), ["to-legacy", "--version", RetocVersion, "--no-shaders", input, extracted]);
            foreach (var (asset, hash) in expected)
            {
                var payload = Path.ChangeExtension(Path.Combine(extracted, PackageToRelativeFile(asset)), ".uexp");
                RequireFile(payload, "已安装包中的导出载荷");
                if (!FileHash(payload).Equals(hash, StringComparison.OrdinalIgnoreCase))
                    throw new UserError($"已安装包载荷哈希不匹配: {asset}");
            }
            Console.WriteLine($"[OK] 已安装包的 {expected.Count} 个资源载荷与构建清单一致");
        }

        private static string FileHash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

        private static void ResetDirectory(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }

        private static string BuildMappingLua(IEnumerable<WeaponAsset> manifest)
        {
            var sb = new StringBuilder();
            sb.AppendLine("local assets = {");
            foreach (var item in manifest.DistinctBy(x => x.CdoPath, StringComparer.OrdinalIgnoreCase))
                sb.Append("    { asset = \"").Append(EscapeLua(item.AssetPath)).Append("\", cdo = \"").Append(EscapeLua(item.CdoPath)).AppendLine("\" },");
            sb.AppendLine("    { asset = \"/Game/Blueprints/Weapons/MSL/BP_wp_msl_BaseClass.BP_wp_msl_BaseClass\", cdo = \"/Game/Blueprints/Weapons/MSL/BP_wp_msl_BaseClass.Default__BP_wp_msl_BaseClass_C\" },");
            sb.AppendLine("    { asset = \"/Game/Datatables/AI/Diffculty/Weapon/DT_WpnPilotRankConfig_Missile.DT_WpnPilotRankConfig_Missile\", cdo = nil },");
            sb.AppendLine("}");
            sb.AppendLine("ExecuteInGameThread(function()");
            sb.AppendLine("    print('[AC8AssetPatcher] Loading weapon blueprints for mappings...')");
            sb.AppendLine("    local loaded, failed = 0, 0");
            sb.AppendLine("    for _, item in ipairs(assets) do");
            sb.AppendLine("        local ok, obj = pcall(function() return LoadAsset(item.asset) end)");
            sb.AppendLine("        if ok and obj then");
            sb.AppendLine("            loaded = loaded + 1");
            sb.AppendLine("            if item.cdo then pcall(function() StaticFindObject(item.cdo) end) end");
            sb.AppendLine("        else failed = failed + 1 end");
            sb.AppendLine("    end");
            sb.AppendLine("    print(string.format('[AC8AssetPatcher] Loaded %d assets, %d failed; dumping Mappings.usmap...', loaded, failed))");
            sb.AppendLine("    local ok, err = pcall(function() DumpUSMAP(true) end)");
            sb.AppendLine("    print(ok and '[AC8AssetPatcher] Mapping dump complete; you may exit the game.' or ('[AC8AssetPatcher] Mapping dump failed: ' .. tostring(err)))");
            sb.AppendLine("end)");
            return sb.ToString();
        }

        private static string EscapeLua(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static void EnableMod(string modsTxt, string name)
        {
            var lines = File.Exists(modsTxt) ? File.ReadAllLines(modsTxt).ToList() : [];
            var found = false;
            for (var i = 0; i < lines.Count; i++)
            {
                if (!lines[i].TrimStart().StartsWith(name + " ", StringComparison.OrdinalIgnoreCase) &&
                    !lines[i].Trim().Equals(name, StringComparison.OrdinalIgnoreCase)) continue;
                lines[i] = name + " : 1";
                found = true;
            }
            if (!found) lines.Add(name + " : 1");
            File.WriteAllLines(modsTxt, lines, new UTF8Encoding(false));
        }

        private static void DisableMod(string modsDir, string name)
        {
            var modsTxt = Path.Combine(modsDir, "mods.txt");
            if (File.Exists(modsTxt))
            {
                var lines = File.ReadAllLines(modsTxt).ToList();
                for (var i = 0; i < lines.Count; i++)
                    if (lines[i].TrimStart().StartsWith(name + " ", StringComparison.OrdinalIgnoreCase) ||
                        lines[i].Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                        lines[i] = name + " : 0";
                File.WriteAllLines(modsTxt, lines, new UTF8Encoding(false));
            }
            var enabled = Path.Combine(modsDir, name, "enabled.txt");
            var disabled = enabled + ".disabled-by-AC8AssetPatcher";
            if (File.Exists(enabled) && !File.Exists(disabled)) File.Move(enabled, disabled);
        }

        private static string Run(string exe, IEnumerable<string> args, bool capture = false) =>
            Run(exe, args, capture, false, out _);

        private static string Run(string exe, IEnumerable<string> args, bool capture, bool allowFailure, out int exitCode)
        {
            var psi = new ProcessStartInfo(exe) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            foreach (var arg in args) psi.ArgumentList.Add(arg);
            using var process = Process.Start(psi) ?? throw new UserError($"无法启动: {exe}");
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            var stdout = stdoutTask.GetAwaiter().GetResult();
            var stderr = stderrTask.GetAwaiter().GetResult();
            exitCode = process.ExitCode;
            if (!capture)
            {
                if (!string.IsNullOrWhiteSpace(stdout)) Console.Write(stdout);
                if (!string.IsNullOrWhiteSpace(stderr)) Console.Error.Write(stderr);
            }
            if (exitCode != 0 && !allowFailure)
                throw new UserError($"{Path.GetFileName(exe)} 退出码 {exitCode}: {stderr.Trim()}");
            return stdout + Environment.NewLine + stderr;
        }
    }

    private sealed class Config
    {
        public string GamePath { get; init; } = "../ACE COMBAT 8";
        public string AesKey { get; init; } = "auto";
        public string ModName { get; init; } = "AC8PGMDirect_P";
        public Dictionary<string, double> Player { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, double> Enemy { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class SettingsTableEditor
    {
        private readonly UAsset _asset;
        private readonly Usmap _mappings;
        private readonly DataTableExport _table;
        private readonly string _label;
        private readonly Dictionary<(string Source, int ValueBits), string> _created = new();
        private readonly Dictionary<string, float> _updated = new(StringComparer.OrdinalIgnoreCase);

        public SettingsTableEditor(string source, Usmap mappings, string label)
        {
            if (!File.Exists(source)) throw new UserError($"找不到{label} 武器设置数据表: {source}");
            _mappings = mappings;
            _label = label;
            _asset = new UAsset(source, EngineVersion.VER_UE5_4, mappings);
            _table = _asset.Exports.OfType<DataTableExport>().SingleOrDefault()
                ?? throw new UserError($"{label} 武器设置资源不是 DataTable: {source}");
        }

        public string GetOrAddRow(string sourceName, float value)
        {
            var key = (Source: sourceName.ToUpperInvariant(), ValueBits: BitConverter.SingleToInt32Bits(value));
            if (_created.TryGetValue(key, out var existing)) return existing;
            var source = _table.Table.Data.SingleOrDefault(x => x.Name.ToString().Equals(sourceName, StringComparison.OrdinalIgnoreCase))
                ?? throw new UserError($"{_label} 武器设置表中找不到蓝图引用的行: {sourceName}");
            var clone = (StructPropertyData)source.Clone();
            var safeSource = Regex.Replace(sourceName, "[^A-Za-z0-9_]", "_");
            var customName = $"AC8PN_{safeSource}_{unchecked((uint)key.ValueBits):X8}";
            clone.Name = FName.FromString(_asset, customName);
            if (clone[PropertyName] is not FloatPropertyData homing)
                throw new UserError($"{_label} 武器设置行 {sourceName} 缺少 float {PropertyName}");
            homing.Value = value;
            _table.Table.Data.Add(clone);
            _created[key] = customName;
            return customName;
        }

        public void SetExistingRow(string rowName, float value)
        {
            var row = _table.Table.Data.SingleOrDefault(x => x.Name.ToString().Equals(rowName, StringComparison.OrdinalIgnoreCase))
                ?? throw new UserError($"{_label} 武器设置表中找不到行: {rowName}");
            if (row[PropertyName] is not FloatPropertyData homing)
                throw new UserError($"{_label} 武器设置行 {rowName} 缺少 float {PropertyName}");
            homing.Value = value;
            _updated[rowName] = value;
        }

        public void SetMatchingRows(Func<string, float?> selector)
        {
            foreach (var row in _table.Table.Data)
            {
                var value = selector(row.Name.ToString());
                if (value is not null) SetExistingRow(row.Name.ToString(), value.Value);
            }
        }

        public void Write(string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            _asset.ResolveAncestries();
            _asset.Write(destination);
            var checkAsset = new UAsset(destination, EngineVersion.VER_UE5_4, _mappings);
            var checkTable = checkAsset.Exports.OfType<DataTableExport>().Single();
            foreach (var ((_, bits), name) in _created)
            {
                var row = checkTable.Table.Data.SingleOrDefault(x => x.Name.ToString().Equals(name, StringComparison.OrdinalIgnoreCase));
                var actual = (row?[PropertyName] as FloatPropertyData)?.Value;
                var expected = BitConverter.Int32BitsToSingle(bits);
                if (actual is null || Math.Abs(actual.Value - expected) > 0.00001f)
                    throw new UserError($"{_label} 自定义设置行回读失败: {name}");
            }
            foreach (var (name, expected) in _updated)
            {
                var row = checkTable.Table.Data.SingleOrDefault(x => x.Name.ToString().Equals(name, StringComparison.OrdinalIgnoreCase));
                var actual = (row?[PropertyName] as FloatPropertyData)?.Value;
                if (actual is null || Math.Abs(actual.Value - expected) > 0.00001f)
                    throw new UserError($"{_label} 设置行回读失败: {name}");
            }
            Console.WriteLine($"{_label} 数据表: 新增 {_created.Count} 个专属覆盖行，更新 {_updated.Count} 个现有行");
        }
    }

    private enum Side { Player, Enemy }
    private sealed record WeaponAsset(Side Side, string CdoPath, string AssetPath, string PackagePath);
    private sealed record PatchTarget(WeaponAsset Asset, float Value, string WeaponType);
    private sealed class UserError(string message) : Exception(message);
}
