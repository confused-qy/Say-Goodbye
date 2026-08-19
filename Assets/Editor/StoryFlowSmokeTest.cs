using System.IO;
using SayGoodbye.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StoryFlowSmokeTest
{
    // Loads every story node using the current localized scene scaffolds.
    private const string SaveKey = "SayGoodbye.Save.v1";
    private const string RunningKey = "SayGoodbye.FlowSmoke.Running";
    private const string IndexKey = "SayGoodbye.FlowSmoke.Index";
    private const string HadSaveKey = "SayGoodbye.FlowSmoke.HadSave";
    private const string SavedJsonKey = "SayGoodbye.FlowSmoke.SaveJson";
    private const string UnlockGateCheckedKey = "SayGoodbye.FlowSmoke.UnlockGateChecked";
    private const string RequestFile = "Library/SayGoodbyeFlowSmoke.request";

    private static bool updateAttached;
    private static double stepStartedAt;

    static StoryFlowSmokeTest()
    {
        EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        EditorApplication.delayCall += StartIfRequested;

        if (SessionState.GetBool(RunningKey, false) && EditorApplication.isPlaying)
        {
            AttachUpdate();
        }
    }

    [MenuItem("Tools/Say Goodbye/Run Full Story Flow Smoke Test")]
    public static void RunFromMenu()
    {
        StartTest();
    }

    private static void StartIfRequested()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += StartIfRequested;
            return;
        }

        string requestPath = Path.GetFullPath(RequestFile);
        if (File.Exists(requestPath))
        {
            File.Delete(requestPath);
            StartTest();
        }
    }

    private static void StartTest()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(RunningKey, false))
        {
            Debug.LogWarning("[FlowSmoke] A Play Mode session is already active.");
            return;
        }

        SessionState.SetBool(RunningKey, true);
        SessionState.SetInt(IndexKey, 0);
        SessionState.SetBool(UnlockGateCheckedKey, false);
        bool hadSave = PlayerPrefs.HasKey(SaveKey);
        SessionState.SetBool(HadSaveKey, hadSave);
        SessionState.SetString(SavedJsonKey, hadSave ? PlayerPrefs.GetString(SaveKey) : string.Empty);

        SceneAsset boot = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Main/00_Boot.scene");
        if (boot == null)
        {
            Fail("Boot scene is missing.");
            return;
        }

        EditorSceneManager.playModeStartScene = boot;
        Debug.Log("[FlowSmoke] Starting " + StoryFlowSequence.Count + "-step runtime scene walkthrough.");
        EditorApplication.isPlaying = true;
    }

    private static void HandlePlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(RunningKey, false))
        {
            return;
        }

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            AttachUpdate();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            DetachUpdate();
        }
    }

    private static void AttachUpdate()
    {
        if (updateAttached)
        {
            return;
        }

        updateAttached = true;
        stepStartedAt = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
    }

    private static void DetachUpdate()
    {
        if (!updateAttached)
        {
            return;
        }

        updateAttached = false;
        EditorApplication.update -= Tick;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            return;
        }

        int index = SessionState.GetInt(IndexKey, 0);
        StoryFlowStep step = StoryFlowSequence.Get(index);
        GameFlowManager flow = GameFlowManager.Instance;

        if (flow == null)
        {
            if (EditorApplication.timeSinceStartup - stepStartedAt > 12d)
            {
                Fail("Timed out waiting for GameFlowManager at step " + index + ".");
            }
            return;
        }

        if (!SessionState.GetBool(UnlockGateCheckedKey, false))
        {
            flow.StartNewGame();
            flow.SetFlowStepForTesting(StoryFlowSequence.Count - 1);
            if (SceneAccessCatalog.IsUnlocked(SceneCatalog.GameComplete, flow))
            {
                Fail("Changing the numeric test step unlocked a protected scene without task confirmation.");
                return;
            }

            flow.SetFlowStepForTesting(0);
            SessionState.SetBool(UnlockGateCheckedKey, true);
            Debug.Log("[FlowSmoke] PASS explicit task-confirmation unlock gate.");
            stepStartedAt = EditorApplication.timeSinceStartup;
            return;
        }

        if (flow.CurrentFlowStep != index)
        {
            flow.SetFlowStepForTesting(index);
            stepStartedAt = EditorApplication.timeSinceStartup;
            return;
        }

        if (SceneManager.GetActiveScene().name != step.SceneName)
        {
            if (EditorApplication.timeSinceStartup - stepStartedAt > 12d)
            {
                Fail("Timed out waiting for step " + index + " / " + step.SceneName);
            }
            return;
        }

        SceneFlowPresenter presenter = Object.FindObjectOfType<SceneFlowPresenter>();
        ScenePlaceholderNavigator navigator = Object.FindObjectOfType<ScenePlaceholderNavigator>();
        Text objective = GameObject.Find("Objective") != null ? GameObject.Find("Objective").GetComponent<Text>() : null;
        if (presenter == null || navigator == null || objective == null || !objective.text.Contains(step.Objective))
        {
            if (EditorApplication.timeSinceStartup - stepStartedAt > 12d)
            {
                Fail("Runtime components or objective text failed at step " + index + " / " + step.Id);
            }
            return;
        }

        ScenePlaceholderHotspot[] hotspots = Object.FindObjectsOfType<ScenePlaceholderHotspot>(true);
        foreach (StoryTaskRequirement requirement in step.Requirements)
        {
            if (requirement.HotspotNames.Length == 0)
            {
                continue;
            }

            bool found = false;
            foreach (ScenePlaceholderHotspot hotspot in hotspots)
            {
                if (requirement.Matches(hotspot.HotspotName))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Fail("Task requirement has no matching hotspot at " + step.Id + " / " + requirement.Label);
                return;
            }
        }

        Debug.Log("[FlowSmoke] PASS " + index.ToString("00") + " / " + step.Id + " / " + step.SceneName);
        index++;
        if (index >= StoryFlowSequence.Count)
        {
            Complete();
            return;
        }

        SessionState.SetInt(IndexKey, index);
        StoryFlowStep next = StoryFlowSequence.Get(index);
        flow.SetFlowStepForTesting(index);
        stepStartedAt = EditorApplication.timeSinceStartup;
        SceneManager.LoadScene(next.SceneName);
    }

    private static void Complete()
    {
        Debug.Log("[FlowSmoke] COMPLETE: " + StoryFlowSequence.Count + "/" + StoryFlowSequence.Count + " story steps loaded and validated.");
        RestoreSave();
        SessionState.SetBool(RunningKey, false);
        DetachUpdate();
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
            return;
        }

        EditorApplication.isPlaying = false;
    }

    private static void Fail(string message)
    {
        Debug.LogError("[FlowSmoke] FAILED: " + message);
        RestoreSave();
        SessionState.SetBool(RunningKey, false);
        DetachUpdate();
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(1);
        }
    }

    private static void RestoreSave()
    {
        if (SessionState.GetBool(HadSaveKey, false))
        {
            PlayerPrefs.SetString(SaveKey, SessionState.GetString(SavedJsonKey, string.Empty));
        }
        else
        {
            PlayerPrefs.DeleteKey(SaveKey);
        }

        PlayerPrefs.Save();
    }
}
