using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 团结引擎微信小游戏构建工具。
/// 通过反射调用团结引擎内部 API WXMiniGameBuildAndExportUtil.BuildAndExport，
/// 把指定场景导出为微信小游戏包。
///
/// 命令行调用（便于观察日志）：
///   Tuanjie -projectPath &lt;project&gt; -executeMethod TuanjieMiniGameBuilder.Build -quit -wmgScene &lt;scene&gt; -wmgAppId &lt;appid&gt; -wmgCdn &lt;cdn&gt; -wmgOutput &lt;dir&gt;
/// </summary>
public static class TuanjieMiniGameBuilder
{
    private const string DefaultScene = "Assets/Scenes/Main/00_Boot.scene";
    private const string DefaultOutput = "Build/WeChatMiniGame";
    private const string DefaultAppId = "wx5a535d8dfed1a1bb";

    private static string GetArg(string key, string fallback)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return fallback;
    }

    /// <summary>命令行入口。</summary>
    [UnityEditor.MenuItem("Tools/WeChat Mini Game/Build")]
    public static void BuildAndExportFromCommandLine()
    {
        string scene = GetArg("-wmgScene", DefaultScene);
        string appId = GetArg("-wmgAppId", DefaultAppId);
        string cdn = GetArg("-wmgCdn", string.Empty);
        string output = GetArg("-wmgOutput", DefaultOutput);

        bool ok = BuildAndExport(scene, appId, cdn, output);
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }

    [UnityEditor.MenuItem("Tools/WeChat Mini Game/Build All Scenes %#g")]
    public static void BuildAllScenesFromEditor()
    {
        BuildAndExport(DefaultScene, DefaultAppId, string.Empty, DefaultOutput);
    }

    /// <summary>组一组并调用团结引擎的微信小游戏构建导出入口。</summary>
    public static bool BuildAndExport(string scenePath, string appId, string cdn, string outputDir)
    {
        if (string.IsNullOrEmpty(scenePath) || !scenePath.EndsWith(".scene", StringComparison.Ordinal))
        {
            scenePath = DefaultScene;
        }

        if (!AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scenePath))
        {
            Debug.LogError($"[WeChatMiniGame] 场景不存在: {scenePath}");
            return false;
        }

        BuildTarget target = (BuildTarget)Enum.Parse(typeof(BuildTarget), "MiniGame");
        BuildTargetGroup group = (BuildTargetGroup)Enum.Parse(typeof(BuildTargetGroup), "MiniGame");

        if (EditorUserBuildSettings.activeBuildTarget != target)
        {
            Debug.Log($"[WeChatMiniGame] 切换到微信小游戏平台 {target} ...");
            EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
        }

        string[] scenes = ResolveEnabledScenes(scenePath);
        var bpo = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = System.IO.Path.GetFullPath(outputDir),
            target = target,
            targetGroup = group,
            options = BuildOptions.None,
        };

        Debug.Log("[WeChatMiniGame] 开始构建微信小游戏包 (appId=" + appId + ", cdn=" + (string.IsNullOrEmpty(cdn) ? "(空)" : cdn) + ")");
        Debug.Log("[WeChatMiniGame] 构建场景数量: " + scenes.Length + "，首场景: " + scenes[0]);
        Debug.Log("[WeChatMiniGame] 输出目录: " + bpo.locationPathName);

        bool result = InvokeBuildAndExport(bpo, appId, cdn, false);
        if (result)
        {
            OptimizeGeneratedProject(bpo.locationPathName);
        }
        Debug.Log("[WeChatMiniGame] 构建结果: " + (result ? "成功" : "失败"));
        return result;
    }

    private static string[] ResolveEnabledScenes(string fallbackScene)
    {
        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene configured in EditorBuildSettings.scenes)
        {
            if (configured.enabled && AssetDatabase.LoadAssetAtPath<SceneAsset>(configured.path) != null)
            {
                scenes.Add(configured.path);
            }
        }

        if (scenes.Count == 0)
        {
            scenes.Add(fallbackScene);
        }

        if (scenes[0] != DefaultScene && scenes.Contains(DefaultScene))
        {
            scenes.Remove(DefaultScene);
            scenes.Insert(0, DefaultScene);
        }

        return scenes.ToArray();
    }

    private static bool InvokeBuildAndExport(BuildPlayerOptions bpo, string appId, string cdn, bool enableIOSMetal)
    {
        bool currentSdkHandled;
        bool currentSdkResult = InvokeCurrentSdk(out currentSdkHandled);
        if (currentSdkHandled)
        {
            return currentSdkResult;
        }

        Type util = typeof(EditorUserBuildSettings).Assembly.GetType("UnityEditor.WXMiniGameBuildAndExportUtil");
        if (util == null)
        {
            Debug.LogError("[WeChatMiniGame] 找不到团结引擎构建工具 WXMiniGameBuildAndExportUtil");
            return false;
        }

        MethodInfo method = util.GetMethod("BuildAndExport", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null)
        {
            Debug.LogError("[WeChatMiniGame] 找不到 BuildAndExport 静态方法");
            return false;
        }

        try
        {
            object result = method.Invoke(null, new object[] { bpo, appId, cdn, enableIOSMetal });
            return result is bool b && b;
        }
        catch (TargetInvocationException ex)
        {
            Debug.LogError("[WeChatMiniGame] 构建异常: " + ex.InnerException);
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError("[WeChatMiniGame] 反射调用异常: " + ex);
            return false;
        }
    }

    private static bool InvokeCurrentSdk(out bool handled)
    {
        handled = false;
        Type converter = null;
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            converter = assembly.GetType("WeChatWASM.WXConvertCore", false);
            if (converter != null)
            {
                break;
            }
        }

        if (converter == null)
        {
            return false;
        }

        MethodInfo method = converter.GetMethod(
            "DoExport",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(bool) },
            null);
        if (method == null)
        {
            return false;
        }

        handled = true;
        try
        {
            Debug.Log("[WeChatMiniGame] 使用当前微信 SDK 接口 WXConvertCore.DoExport(buildWebGL: true)");
            object result = method.Invoke(null, new object[] { true });
            int errorCode = Convert.ToInt32(result);
            Debug.Log("[WeChatMiniGame] 当前 SDK 返回码: " + errorCode + " (0=成功)");
            return errorCode == 0;
        }
        catch (TargetInvocationException exception)
        {
            Debug.LogError("[WeChatMiniGame] 当前 SDK 构建异常: " + exception.InnerException);
            return false;
        }
        catch (Exception exception)
        {
            Debug.LogError("[WeChatMiniGame] 当前 SDK 反射调用异常: " + exception);
            return false;
        }
    }

    private static void OptimizeGeneratedProject(string fallbackOutput)
    {
        string output = ResolveCurrentSdkOutput();
        if (string.IsNullOrWhiteSpace(output))
        {
            output = fallbackOutput;
        }

        string projectConfig = Path.Combine(output, "minigame", "project.config.json");
        if (!File.Exists(projectConfig))
        {
            Debug.LogWarning("[WeChatMiniGame] 未找到生成的 project.config.json，跳过上传优化: " + projectConfig);
            return;
        }

        string json = File.ReadAllText(projectConfig);
        json = json.Replace("\"uploadWithSourceMap\": true", "\"uploadWithSourceMap\": false");
        File.WriteAllText(projectConfig, json);
        Debug.Log("[WeChatMiniGame] 已关闭上传 Source Map: " + projectConfig);
    }

    private static string ResolveCurrentSdkOutput()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type converter = assembly.GetType("WeChatWASM.WXConvertCore", false);
            if (converter == null)
            {
                continue;
            }

            PropertyInfo configProperty = converter.GetProperty("config", BindingFlags.Public | BindingFlags.Static);
            object config = configProperty != null ? configProperty.GetValue(null, null) : null;
            FieldInfo projectConfField = config != null ? config.GetType().GetField("ProjectConf") : null;
            object projectConf = projectConfField != null ? projectConfField.GetValue(config) : null;
            FieldInfo destinationField = projectConf != null ? projectConf.GetType().GetField("DST") : null;
            return destinationField != null ? destinationField.GetValue(projectConf) as string : null;
        }

        return null;
    }
}
