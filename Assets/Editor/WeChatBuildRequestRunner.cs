using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class WeChatBuildRequestRunner
{
    // Rebuilds the local preview after phase-one scene and art changes.
    private const string RequestFile = "Library/SayGoodbyeWeChatBuild.request";

    static WeChatBuildRequestRunner()
    {
        EditorApplication.delayCall += RunIfRequested;
    }

    private static void RunIfRequested()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += RunIfRequested;
            return;
        }

        string path = Path.GetFullPath(RequestFile);
        if (!File.Exists(path))
        {
            return;
        }

        File.Delete(path);
        Debug.Log("[WeChatMiniGame] Local build request detected. Building every enabled story scene.");
        TuanjieMiniGameBuilder.BuildAllScenesFromEditor();
    }
}
