using System;
using System.Collections.Generic;
using System.IO;
using SayGoodbye.Core;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SceneScaffoldBuilder
{
    private const int ScaffoldVersion = 6;
    private const string VersionKey = "SayGoodbye.SceneScaffoldVersion";
    private const string PreviewKey = "SayGoodbye.SceneScaffoldPreviewVersion";

    private enum LayoutKind
    {
        Boot, Prologue, Hospital, LivingRoom, Bedroom, Corridor, Kitchen,
        Guitar, Makeup, Sunflower, Cooking, FamilyPuzzle, EndingComic, Epilogue, Complete
    }

    private sealed class SceneDefinition
    {
        public readonly string Path;
        public readonly string Code;
        public readonly string Title;
        public readonly string Chapter;
        public readonly string Objective;
        public readonly LayoutKind Layout;

        public SceneDefinition(string path, string code, string title, string chapter, string objective, LayoutKind layout)
        {
            Path = path;
            Code = code;
            Title = title;
            Chapter = chapter;
            Objective = objective;
            Layout = layout;
        }
    }

    private sealed class SceneContext
    {
        public RectTransform Stage;
        public Text Feedback;
        public Font Font;
        public Text SceneTitle;
        public Text Objective;
        public bool HasBackdrop;
    }

    private static readonly Color Background = new Color(0.035f, 0.047f, 0.063f, 1f);
    private static readonly Color StageColor = new Color(0.075f, 0.095f, 0.115f, 1f);
    private static readonly Color WallColor = new Color(0.16f, 0.18f, 0.19f, 1f);
    private static readonly Color FloorColor = new Color(0.12f, 0.105f, 0.09f, 1f);
    private static readonly Color ObjectColor = new Color(0.19f, 0.25f, 0.27f, 1f);
    private static readonly Color HotspotColor = new Color(0.16f, 0.39f, 0.4f, 1f);
    private static readonly Color AccentColor = new Color(0.76f, 0.47f, 0.31f, 1f);
    private static readonly Color MutedText = new Color(0.64f, 0.68f, 0.7f, 1f);

    private const string ExteriorArt = "Assets/Art/AI_Draft/bg_hospice_exterior.png";
    private const string HospitalArt = "Assets/Art/AI_Draft/bg_hospital_ward.png";
    private const string LivingRoomArt = "Assets/Art/AI_Draft/bg_living_room.png";
    private const string BedroomArt = "Assets/Art/AI_Draft/bg_bedroom.png";
    private const string CorridorArt = "Assets/Art/AI_Draft/bg_hospital_corridor.png";
    private const string KitchenArt = "Assets/Art/AI_Draft/bg_kitchen.png";

    private static readonly SceneDefinition[] Definitions =
    {
        new SceneDefinition("Assets/Scenes/Main/00_Boot.scene", "00", "好好说再见", "标题", "开始或继续故事。", LayoutKind.Boot),
        new SceneDefinition("Assets/Scenes/Main/01_Prologue.scene", "01", "序章", "开幕", "阅读林淑珍入住安宁病房前的七段经历。", LayoutKind.Prologue),
        new SceneDefinition("Assets/Scenes/Areas/02_Hospital.scene", "02", "安宁病房", "三条心愿", "在左、右病房视角完成访谈、调查与道具交互。", LayoutKind.Hospital),
        new SceneDefinition("Assets/Scenes/Areas/03_LivingRoom.scene", "03", "记忆中的客厅", "记忆空间", "寻找电话、向日葵线索、储物盒与磁带机。", LayoutKind.LivingRoom),
        new SceneDefinition("Assets/Scenes/Areas/04_Bedroom.scene", "04", "记忆中的卧室", "记忆空间", "调查手写歌词、镜子与旧胭脂。", LayoutKind.Bedroom),
        new SceneDefinition("Assets/Scenes/Areas/05_Corridor.scene", "05", "医院走廊", "故事桥段", "等待女儿到达，再一起返回病房。", LayoutKind.Corridor),
        new SceneDefinition("Assets/Scenes/Areas/06_Kitchen.scene", "06", "记忆中的厨房", "心愿三", "使用各个工作区准备记忆中的饭菜。", LayoutKind.Kitchen),
        new SceneDefinition("Assets/Scenes/Minigames/10_Guitar.scene", "10", "吉他编曲", "心愿活动", "按照提示弹奏正确的琴弦顺序。", LayoutKind.Guitar),
        new SceneDefinition("Assets/Scenes/Minigames/11_Makeup.scene", "11", "整理妆容", "心愿活动", "按正确顺序完成妆容。", LayoutKind.Makeup),
        new SceneDefinition("Assets/Scenes/Minigames/12_Sunflower.scene", "12", "向日葵机关", "心愿活动", "切换相邻花朵，点亮所有向日葵。", LayoutKind.Sunflower),
        new SceneDefinition("Assets/Scenes/Minigames/13_Cooking.scene", "13", "记忆料理", "心愿活动", "让食材依次经过备料、烹饪与摆盘。", LayoutKind.Cooking),
        new SceneDefinition("Assets/Scenes/Minigames/14_FamilyPuzzle.scene", "14", "全家福拼图", "心愿活动", "重新拼好那张全家福。", LayoutKind.FamilyPuzzle),
        new SceneDefinition("Assets/Scenes/Main/20_EndingComic.scene", "20", "好好告别", "尾声", "依次阅读母女和解、拥抱与合影。", LayoutKind.EndingComic),
        new SceneDefinition("Assets/Scenes/Main/21_Epilogue.scene", "21", "三个月后", "尾声", "回到空病房，播放最后一段录音。", LayoutKind.Epilogue),
        new SceneDefinition("Assets/Scenes/Main/22_GameComplete.scene", "22", "告别", "完成", "重温故事，或回到标题。", LayoutKind.Complete)
    };

    static SceneScaffoldBuilder()
    {
        EditorApplication.delayCall += BuildAutomatically;
    }

    [MenuItem("Tools/Say Goodbye/Rebuild Game Scene Greyboxes")]
    public static void RebuildFromMenu()
    {
        Build(true);
    }

    private static void BuildAutomatically()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += BuildAutomatically;
            return;
        }

        if (EditorPrefs.GetInt(VersionKey, 0) < ScaffoldVersion || !HasAllScenes())
        {
            Build(true);
        }
        else
        {
            EnsureBuildSettings();
            OpenHospitalPreviewOnce();
        }
    }

    private static void Build(bool overwrite)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[SceneScaffold] Exit Play Mode before rebuilding game scenes.");
            return;
        }

        EnsureArtImportSettings();
        Scene activeScene = PreserveWorkingScene(SceneManager.GetActiveScene());

        if (overwrite)
        {
            foreach (SceneDefinition definition in Definitions)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(definition.Path) != null)
                {
                    AssetDatabase.DeleteAsset(definition.Path);
                }
            }
        }

        for (int index = 0; index < Definitions.Length; index++)
        {
            SceneDefinition definition = Definitions[index];
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(definition.Path) != null)
            {
                continue;
            }

            string previous = index > 0 ? Path.GetFileNameWithoutExtension(Definitions[index - 1].Path) : string.Empty;
            string next = index < Definitions.Length - 1 ? Path.GetFileNameWithoutExtension(Definitions[index + 1].Path) : string.Empty;
            CreateScene(definition, previous, next);
        }

        if (activeScene.IsValid() && activeScene.isLoaded)
        {
            SceneManager.SetActiveScene(activeScene);
        }

        EnsureBuildSettings();
        EditorPrefs.SetInt(VersionKey, ScaffoldVersion);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SceneScaffold] Spatial game greyboxes are ready. Text is used only for objects and hotspots.");
        OpenHospitalPreviewOnce();
    }

    private static void OpenHospitalPreviewOnce()
    {
        if (EditorPrefs.GetInt(PreviewKey, 0) >= ScaffoldVersion || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        SceneAsset hospital = AssetDatabase.LoadAssetAtPath<SceneAsset>(Definitions[2].Path);
        if (hospital == null)
        {
            return;
        }

        EditorSceneManager.OpenScene(Definitions[2].Path, OpenSceneMode.Single);
        EditorPrefs.SetInt(PreviewKey, ScaffoldVersion);
        Debug.Log("[SceneScaffold] Opened the hospital gameplay greybox for review.");
    }

    private static Scene PreserveWorkingScene(Scene scene)
    {
        bool isUntitled = scene.IsValid() && string.IsNullOrEmpty(scene.path);
        bool isGeneratedScene = scene.IsValid() && IsGeneratedPath(scene.path);
        if (!isUntitled && !isGeneratedScene)
        {
            return scene;
        }

        const string directory = "Assets/Scenes/Working";
        Directory.CreateDirectory(directory);
        string path = AssetDatabase.GenerateUniqueAssetPath(directory + "/Before_Greybox_Rebuild.scene");
        if (!EditorSceneManager.SaveScene(scene, path))
        {
            throw new InvalidOperationException("The active working scene could not be preserved before greybox generation.");
        }

        Debug.Log("[SceneScaffold] Preserved the previous working scene at " + path);
        return scene;
    }

    private static bool IsGeneratedPath(string path)
    {
        foreach (SceneDefinition definition in Definitions)
        {
            if (string.Equals(path, definition.Path, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAllScenes()
    {
        foreach (SceneDefinition definition in Definitions)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(definition.Path) == null)
            {
                return false;
            }
        }

        return true;
    }

    private static void CreateScene(SceneDefinition definition, string previousScene, string nextScene)
    {
        string directory = Path.GetDirectoryName(definition.Path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            GameObject root = new GameObject("GameSceneRoot");
            ScenePlaceholderNavigator navigator = root.AddComponent<ScenePlaceholderNavigator>();
            SceneFlowPresenter presenter = root.AddComponent<SceneFlowPresenter>();
            navigator.Configure(previousScene, nextScene);

            CreateCamera();
            CreateEventSystem();
            Canvas canvas = CreateCanvas();
            SceneContext context = CreateChrome(canvas, definition, navigator);
            presenter.Configure(context.SceneTitle, context.Objective, context.Feedback);
            context.HasBackdrop = CreateBackdrop(context.Stage, definition.Layout);
            BuildLayout(context, definition.Layout);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, definition.Path);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static SceneContext CreateChrome(Canvas canvas, SceneDefinition definition, ScenePlaceholderNavigator navigator)
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/SayGoodbyeChineseSubset.ttf");
        if (font == null)
        {
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/汇文明朝体.otf");
        }
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        Image background = CreateImage("Background", canvas.transform, Background);
        Stretch(background.rectTransform);

        RectTransform safeArea = CreateRect("GameSafeArea", canvas.transform);
        Stretch(safeArea);
        safeArea.gameObject.AddComponent<SafeAreaFitter>();

        Image header = CreateImage("HUD_Header", safeArea, new Color(0.06f, 0.08f, 0.1f, 1f));
        SetRect(header.rectTransform, new Vector2(0.02f, 0.905f), new Vector2(0.98f, 0.985f));
        AddOutline(header.gameObject, new Color(0.23f, 0.28f, 0.31f, 1f));

        Text title = CreateText("SceneTitle", header.transform, font, "场景 " + definition.Code + "  /  " + definition.Title, 30, Color.white, TextAnchor.MiddleLeft);
        title.fontStyle = FontStyle.Bold;
        SetRect(title.rectTransform, new Vector2(0.025f, 0f), new Vector2(0.52f, 1f));

        Text objective = CreateText("Objective", header.transform, font, definition.Chapter + "  /  " + definition.Objective, 19, MutedText, TextAnchor.MiddleRight);
        SetRect(objective.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.975f, 1f));

        Image stage = CreateImage("GameViewport", safeArea, StageColor);
        SetRect(stage.rectTransform, new Vector2(0.02f, 0.155f), new Vector2(0.98f, 0.895f));
        AddOutline(stage.gameObject, new Color(0.22f, 0.27f, 0.29f, 1f));
        stage.gameObject.AddComponent<RectMask2D>();

        Image feedbackPanel = CreateImage("InteractionFeedback", safeArea, new Color(0.07f, 0.095f, 0.11f, 1f));
        SetRect(feedbackPanel.rectTransform, new Vector2(0.27f, 0.035f), new Vector2(0.72f, 0.125f));
        Text feedback = CreateText("FeedbackText", feedbackPanel.transform, font, "选择可交互物，或打开场景地图。", 18, MutedText, TextAnchor.MiddleCenter);
        Stretch(feedback.rectTransform);

        Button titleButton = CreateButton("ReturnToTitle", safeArea, font, "标题", ObjectColor);
        SetRect(titleButton.GetComponent<RectTransform>(), new Vector2(0.025f, 0.035f), new Vector2(0.105f, 0.125f));
        UnityEventTools.AddPersistentListener(titleButton.onClick, navigator.GoToBoot);

        Button map = CreateButton("SceneMap", safeArea, font, "测试场景地图", GameUiTheme.Unlocked);
        SetRect(map.GetComponent<RectTransform>(), new Vector2(0.115f, 0.035f), new Vector2(0.255f, 0.125f));
        UnityEventTools.AddPersistentListener(map.onClick, navigator.ToggleSceneMap);
        map.gameObject.AddComponent<DevelopmentOnlyObject>();

        Button task = CreateButton("TaskConfirmation", safeArea, font, "任务确认", GameUiTheme.Pending);
        SetRect(task.GetComponent<RectTransform>(), new Vector2(0.76f, 0.035f), new Vector2(0.975f, 0.125f));
        UnityEventTools.AddPersistentListener(task.onClick, navigator.ShowTaskConfirmation);

        return new SceneContext
        {
            Stage = stage.rectTransform,
            Feedback = feedback,
            Font = font,
            SceneTitle = title,
            Objective = objective
        };
    }

    private static void BuildLayout(SceneContext c, LayoutKind layout)
    {
        switch (layout)
        {
            case LayoutKind.Boot: BuildBoot(c); break;
            case LayoutKind.Prologue: BuildPrologue(c); break;
            case LayoutKind.Hospital: BuildHospital(c, false); break;
            case LayoutKind.LivingRoom: BuildLivingRoom(c); break;
            case LayoutKind.Bedroom: BuildBedroom(c); break;
            case LayoutKind.Corridor: BuildCorridor(c); break;
            case LayoutKind.Kitchen: BuildKitchen(c); break;
            case LayoutKind.Guitar: BuildGuitar(c); break;
            case LayoutKind.Makeup: BuildMakeup(c); break;
            case LayoutKind.Sunflower: BuildSunflower(c); break;
            case LayoutKind.Cooking: BuildCooking(c); break;
            case LayoutKind.FamilyPuzzle: BuildFamilyPuzzle(c); break;
            case LayoutKind.EndingComic: BuildEndingComic(c); break;
            case LayoutKind.Epilogue: BuildHospital(c, true); break;
            case LayoutKind.Complete: BuildComplete(c); break;
        }
    }

    private static bool CreateBackdrop(RectTransform stage, LayoutKind layout)
    {
        string path = null;
        switch (layout)
        {
            case LayoutKind.Boot:
            case LayoutKind.Prologue:
            case LayoutKind.Complete:
                path = ExteriorArt;
                break;
            case LayoutKind.Hospital:
            case LayoutKind.Epilogue:
            case LayoutKind.EndingComic:
                path = HospitalArt;
                break;
            case LayoutKind.LivingRoom:
                path = LivingRoomArt;
                break;
            case LayoutKind.Bedroom:
                path = BedroomArt;
                break;
            case LayoutKind.Corridor:
                path = CorridorArt;
                break;
            case LayoutKind.Kitchen:
            case LayoutKind.Cooking:
                path = KitchenArt;
                break;
        }

        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogWarning("[SceneScaffold] 未找到 AI 场景底图: " + path);
            return false;
        }

        Image backdrop = CreateImage("第一阶段美术底图", stage, Color.white);
        backdrop.sprite = sprite;
        backdrop.preserveAspect = true;
        RectTransform backdropRect = backdrop.rectTransform;
        backdropRect.anchorMin = new Vector2(0.5f, 0.5f);
        backdropRect.anchorMax = new Vector2(0.5f, 0.5f);
        backdropRect.pivot = new Vector2(0.5f, 0.5f);
        backdropRect.anchoredPosition = Vector2.zero;
        backdropRect.sizeDelta = Vector2.zero;
        AspectRatioFitter aspect = backdrop.gameObject.AddComponent<AspectRatioFitter>();
        aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        aspect.aspectRatio = sprite.rect.width / sprite.rect.height;

        Image shade = CreateImage("交互可读性遮罩", stage, new Color(0.02f, 0.03f, 0.035f, 0.12f));
        Stretch(shade.rectTransform);
        return true;
    }

    private static void EnsureArtImportSettings()
    {
        string[] paths = { ExteriorArt, HospitalArt, LivingRoomArt, BedroomArt, CorridorArt, KitchenArt };
        foreach (string path in paths)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            bool changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.mipmapEnabled
                || importer.maxTextureSize != 2048;
            if (!changed)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.compressionQuality = 55;
            importer.crunchedCompression = true;
            importer.SaveAndReimport();
        }
    }

    private static void BuildBoot(SceneContext c)
    {
        Label(c, "安宁医院 · 夜", 0.69f, 0.08f, 0.94f, 0.16f, 22, new Color(0.88f, 0.76f, 0.58f, 1f), TextAnchor.MiddleRight);
    }

    private static void BuildPrologue(SceneContext c)
    {
        Label(c, "七段开幕与社工独白读完后，需要确认完成，才会进入安宁病房。", 0.46f, 0.10f, 0.93f, 0.20f, 20, new Color(0.90f, 0.83f, 0.72f, 0.9f), TextAnchor.MiddleRight);
    }

    private static void BuildHospital(SceneContext c, bool epilogue)
    {
        if (epilogue)
        {
            Hotspot(c, "空病床", 0.49f, 0.14f, 0.89f, 0.52f, ObjectColor);
            Hotspot(c, "最后的录音机", 0.87f, 0.23f, 0.97f, 0.42f, AccentColor);
            Hotspot(c, "已经清空的文件柜", 0.09f, 0.23f, 0.24f, 0.69f, ObjectColor);
            Hotspot(c, "空椅子", 0.05f, 0.06f, 0.23f, 0.25f, ObjectColor);
            Label(c, "房间已经整理完毕，只有录音机仍留在床边。", 0.60f, 0.70f, 0.94f, 0.82f, 22, MutedText, TextAnchor.MiddleCenter);
            Gateway(c, "完成告别", SceneCatalog.GameComplete, "Epilogue", 0.79f, 0.86f, 0.98f, 0.97f);
            return;
        }

        RectTransform leftView = CreateRect("病房左侧内容", c.Stage);
        Stretch(leftView);
        RectTransform rightView = CreateRect("病房右侧内容", c.Stage);
        Stretch(rightView);

        Hotspot(c, leftView, "病床 · 林淑珍", 0.47f, 0.13f, 0.88f, 0.53f, HotspotColor);
        Hotspot(c, leftView, "床尾卡", 0.68f, 0.50f, 0.82f, 0.65f, AccentColor);
        Hotspot(c, leftView, "心愿清单", 0.12f, 0.49f, 0.28f, 0.73f, AccentColor);
        Hotspot(c, leftView, "工作电话", 0.09f, 0.23f, 0.23f, 0.45f, HotspotColor);
        Hotspot(c, leftView, "音乐志愿者 · 小熊", 0.28f, 0.16f, 0.46f, 0.62f, AccentColor);

        Hotspot(c, rightView, "床头录音机", 0.77f, 0.24f, 0.94f, 0.45f, AccentColor);
        Hotspot(c, rightView, "床头抽屉", 0.78f, 0.08f, 0.95f, 0.24f, ObjectColor);
        Hotspot(c, rightView, "文件柜", 0.09f, 0.21f, 0.27f, 0.70f, HotspotColor);
        Hotspot(c, rightView, "全套化妆品", 0.31f, 0.24f, 0.47f, 0.43f, AccentColor);
        Hotspot(c, rightView, "访客椅", 0.50f, 0.08f, 0.67f, 0.28f, ObjectColor);

        Button leftButton = CreateButton("查看病房左侧", c.Stage, c.Font, "病房左侧", GameUiTheme.Current);
        SetRect(leftButton.GetComponent<RectTransform>(), new Vector2(0.36f, 0.87f), new Vector2(0.49f, 0.97f));
        Button rightButton = CreateButton("查看病房右侧", c.Stage, c.Font, "病房右侧", GameUiTheme.Unlocked);
        SetRect(rightButton.GetComponent<RectTransform>(), new Vector2(0.51f, 0.87f), new Vector2(0.64f, 0.97f));

        HospitalViewController views = c.Stage.gameObject.AddComponent<HospitalViewController>();
        views.Configure(leftView.gameObject, rightView.gameObject, leftButton.GetComponent<Image>(), rightButton.GetComponent<Image>(), c.Feedback);
        UnityEventTools.AddPersistentListener(leftButton.onClick, views.ShowLeft);
        UnityEventTools.AddPersistentListener(rightButton.onClick, views.ShowRight);

        Gateway(c, "进入记忆客厅", SceneCatalog.LivingRoom, "Wish1_HospitalTape", 0.02f, 0.86f, 0.19f, 0.97f);
        Gateway(c, "前往医院走廊", SceneCatalog.Corridor, "Wish3_Start", 0.79f, 0.86f, 0.98f, 0.97f);
    }

    private static void BuildLivingRoom(SceneContext c)
    {
        Hotspot(c, "旧沙发", 0.02f, 0.07f, 0.39f, 0.42f, ObjectColor);
        Hotspot(c, "客厅旧柜子", 0.10f, 0.40f, 0.27f, 0.74f, HotspotColor);
        Hotspot(c, "老电话", 0.59f, 0.30f, 0.68f, 0.49f, AccentColor);
        Hotspot(c, "向日葵盆栽", 0.54f, 0.55f, 0.67f, 0.82f, HotspotColor);
        Hotspot(c, "上锁的储物盒", 0.69f, 0.27f, 0.79f, 0.42f, AccentColor);
        Hotspot(c, "磁带播放机", 0.77f, 0.31f, 0.87f, 0.49f, HotspotColor);
        Hotspot(c, "四点钟", 0.42f, 0.61f, 0.51f, 0.79f, AccentColor);
        Hotspot(c, "全家福相框", 0.31f, 0.57f, 0.41f, 0.76f, HotspotColor);
        Gateway(c, "返回安宁病房", SceneCatalog.Hospital, "Prologue", 0.02f, 0.86f, 0.18f, 0.97f);
        Gateway(c, "进入卧室", SceneCatalog.Bedroom, "Wish1_MemoryTape", 0.83f, 0.86f, 0.98f, 0.97f);
        Gateway(c, "进入厨房", SceneCatalog.Kitchen, "Wish3_Clock", 0.66f, 0.86f, 0.82f, 0.97f);
    }

    private static void BuildBedroom(SceneContext c)
    {
        Hotspot(c, "旧木床", 0.06f, 0.12f, 0.52f, 0.56f, ObjectColor);
        Hotspot(c, "手写歌词", 0.30f, 0.31f, 0.44f, 0.45f, AccentColor);
        Hotspot(c, "梳妆镜", 0.82f, 0.40f, 0.96f, 0.79f, HotspotColor);
        Hotspot(c, "旧胭脂", 0.78f, 0.27f, 0.89f, 0.40f, AccentColor);
        Hotspot(c, "衣柜", 0.52f, 0.33f, 0.73f, 0.83f, ObjectColor);
        Hotspot(c, "墙上的照片", 0.36f, 0.61f, 0.49f, 0.78f, HotspotColor);
        Gateway(c, "返回客厅", SceneCatalog.LivingRoom, "Wish1_HospitalTape", 0.02f, 0.86f, 0.18f, 0.97f);
    }

    private static void BuildCorridor(SceneContext c)
    {
        Hotspot(c, "病房门", 0.05f, 0.18f, 0.23f, 0.78f, HotspotColor);
        Hotspot(c, "林姨的女儿", 0.39f, 0.10f, 0.58f, 0.47f, AccentColor);
        Hotspot(c, "护士站", 0.72f, 0.30f, 0.91f, 0.62f, ObjectColor);
        Label(c, "等待 · 停顿 · 一起进入病房", 0.31f, 0.70f, 0.69f, 0.82f, 22, Color.white, TextAnchor.MiddleCenter);
        Gateway(c, "返回安宁病房", SceneCatalog.Hospital, "Prologue", 0.02f, 0.86f, 0.18f, 0.97f);
    }

    private static void BuildKitchen(SceneContext c)
    {
        Hotspot(c, "水槽", 0.18f, 0.34f, 0.34f, 0.54f, HotspotColor);
        Hotspot(c, "切菜板", 0.40f, 0.40f, 0.57f, 0.55f, AccentColor);
        Hotspot(c, "灶台与汤锅", 0.69f, 0.33f, 0.88f, 0.62f, HotspotColor);
        Hotspot(c, "食材篮", 0.06f, 0.33f, 0.19f, 0.55f, AccentColor);
        Hotspot(c, "餐桌 · 摆盘", 0.46f, 0.05f, 0.82f, 0.29f, ObjectColor);
        Hotspot(c, "冰箱", 0.88f, 0.27f, 0.98f, 0.78f, ObjectColor);
        Hotspot(c, "旧食谱", 0.58f, 0.56f, 0.70f, 0.78f, new Color(0.3f, 0.25f, 0.2f, 1f));
        Gateway(c, "返回客厅", SceneCatalog.LivingRoom, "Wish1_HospitalTape", 0.02f, 0.86f, 0.18f, 0.97f);
        Gateway(c, "开始料理", SceneCatalog.Cooking, "Wish3_Kitchen", 0.82f, 0.86f, 0.98f, 0.97f);
    }

    private static void BuildGuitar(SceneContext c)
    {
        Block(c, "旋律提示： ＿  ＿  ＿  ＿  ＿", 0.16f, 0.84f, 0.84f, 0.95f, ObjectColor);
        string[] strings = { "一弦", "二弦", "三弦", "四弦", "五弦", "六弦" };
        for (int i = 0; i < strings.Length; i++)
        {
            float top = 0.76f - i * 0.105f;
            Hotspot(c, strings[i], 0.12f, top - 0.065f, 0.88f, top, i == 0 ? AccentColor : HotspotColor);
        }
        Hotspot(c, "重新尝试", 0.12f, 0.05f, 0.34f, 0.14f, ObjectColor);
        Hotspot(c, "完成演奏", 0.66f, 0.05f, 0.88f, 0.14f, AccentColor);
        Gateway(c, "返回病房", SceneCatalog.Hospital, "Prologue", 0.02f, 0.86f, 0.15f, 0.97f);
    }

    private static void BuildMakeup(SceneContext c)
    {
        Block(c, "镜子", 0.08f, 0.1f, 0.54f, 0.92f, WallColor);
        Hotspot(c, "面部 · 在这里上妆", 0.18f, 0.22f, 0.44f, 0.78f, new Color(0.42f, 0.31f, 0.27f, 1f));
        Block(c, "步骤顺序", 0.6f, 0.77f, 0.92f, 0.91f, ObjectColor);
        Hotspot(c, "粉底", 0.62f, 0.56f, 0.9f, 0.7f, HotspotColor);
        Hotspot(c, "胭脂", 0.62f, 0.37f, 0.9f, 0.51f, AccentColor);
        Hotspot(c, "口红", 0.62f, 0.18f, 0.9f, 0.32f, HotspotColor);
        Hotspot(c, "确认妆容", 0.62f, 0.04f, 0.9f, 0.13f, AccentColor);
        Gateway(c, "返回病房", SceneCatalog.Hospital, "Prologue", 0.02f, 0.86f, 0.15f, 0.97f);
    }

    private static void BuildSunflower(SceneContext c)
    {
        Label(c, "点击一朵花，也会切换它旁边的花", 0.2f, 0.88f, 0.8f, 0.97f, 22, Color.white, TextAnchor.MiddleCenter);
        int number = 1;
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                float left = 0.27f + column * 0.16f;
                float bottom = 0.58f - row * 0.2f;
                Hotspot(c, "向日葵 " + number + "\n未点亮", left, bottom, left + 0.13f, bottom + 0.16f, number == 5 ? AccentColor : HotspotColor);
                number++;
            }
        }
        Hotspot(c, "重新开始", 0.38f, 0.04f, 0.62f, 0.14f, ObjectColor);
        Hotspot(c, "确认全部盛开", 0.65f, 0.04f, 0.88f, 0.14f, AccentColor);
        Gateway(c, "返回客厅", SceneCatalog.LivingRoom, "Wish1_HospitalTape", 0.02f, 0.86f, 0.15f, 0.97f);
    }

    private static void BuildCooking(SceneContext c)
    {
        Hotspot(c, "处理全部食材", 0.05f, 0.25f, 0.25f, 0.76f, HotspotColor);
        Block(c, ">", 0.26f, 0.43f, 0.31f, 0.58f, Background);
        Hotspot(c, "完成红烧鱼", 0.32f, 0.25f, 0.51f, 0.76f, AccentColor);
        Block(c, "+", 0.52f, 0.43f, 0.57f, 0.58f, Background);
        Hotspot(c, "完成番茄炒蛋", 0.58f, 0.25f, 0.78f, 0.76f, HotspotColor);
        Block(c, ">", 0.79f, 0.43f, 0.84f, 0.58f, Background);
        Hotspot(c, "六点摆盘", 0.85f, 0.25f, 0.97f, 0.76f, AccentColor);
        Label(c, "依次完成备料、两道菜和摆盘；完成后再提交任务。", 0.22f, 0.06f, 0.82f, 0.16f, 21, MutedText, TextAnchor.MiddleCenter);
        Gateway(c, "返回厨房", SceneCatalog.Kitchen, "Wish3_Clock", 0.02f, 0.86f, 0.15f, 0.97f);
    }

    private static void BuildFamilyPuzzle(SceneContext c)
    {
        Block(c, "全家福拼图区", 0.05f, 0.08f, 0.64f, 0.92f, WallColor);
        int number = 1;
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                float left = 0.08f + column * 0.18f;
                float bottom = 0.63f - row * 0.25f;
                Block(c, "位置 " + number, left, bottom, left + 0.16f, bottom + 0.21f, ObjectColor);
                number++;
            }
        }
        Block(c, "照片碎片", 0.69f, 0.12f, 0.95f, 0.88f, ObjectColor);
        Hotspot(c, "碎片 甲", 0.72f, 0.64f, 0.83f, 0.79f, HotspotColor);
        Hotspot(c, "碎片 乙", 0.82f, 0.43f, 0.93f, 0.58f, AccentColor);
        Hotspot(c, "碎片 丙", 0.72f, 0.22f, 0.83f, 0.37f, HotspotColor);
        Hotspot(c, "确认拼图完成", 0.70f, 0.07f, 0.94f, 0.17f, AccentColor);
        Gateway(c, "返回客厅", SceneCatalog.LivingRoom, "Wish1_HospitalTape", 0.02f, 0.86f, 0.15f, 0.97f);
    }

    private static void BuildEndingComic(SceneContext c)
    {
        Hotspot(c, "第一格 · 母女和解", 0.03f, 0.53f, 0.48f, 0.95f, HotspotColor);
        Hotspot(c, "第二格 · 最后的拥抱", 0.52f, 0.53f, 0.97f, 0.95f, AccentColor);
        Hotspot(c, "第三格 · 拍摄合照", 0.03f, 0.05f, 0.48f, 0.47f, HotspotColor);
        Hotspot(c, "第四格 · 新的全家福", 0.52f, 0.05f, 0.97f, 0.47f, AccentColor);
        Gateway(c, "三个月后", SceneCatalog.Epilogue, "Ending", 0.79f, 0.86f, 0.98f, 0.97f);
    }

    private static void BuildComplete(SceneContext c)
    {
        Block(c, "完整的全家福", 0.34f, 0.48f, 0.66f, 0.86f, ObjectColor);
        Label(c, "谢谢你，陪她好好说了再见", 0.23f, 0.27f, 0.77f, 0.4f, 42, Color.white, TextAnchor.MiddleCenter);
        Gateway(c, "重新回到序章", SceneCatalog.Prologue, string.Empty, 0.28f, 0.09f, 0.48f, 0.21f);
    }

    private static void RoomShell(SceneContext c, string roomName)
    {
        Block(c, roomName + " WALL", 0.02f, 0.37f, 0.98f, 0.98f, WallColor);
        Block(c, "FLOOR", 0.02f, 0.02f, 0.98f, 0.37f, FloorColor);
        Block(c, "WINDOW", 0.42f, 0.67f, 0.61f, 0.93f, new Color(0.25f, 0.34f, 0.38f, 1f));
    }

    private static void Panel(SceneContext c, string label, float left, float bottom, float right, float top)
    {
        Block(c, label, left, bottom, right, top, new Color(0.13f, 0.16f, 0.18f, 1f));
    }

    private static Image Block(SceneContext c, string label, float left, float bottom, float right, float top, Color color)
    {
        Color displayColor = color;
        if (c.HasBackdrop)
        {
            displayColor.a = Mathf.Min(displayColor.a, 0.76f);
        }
        Image image = CreateImage(SafeName(label), c.Stage, displayColor);
        SetRect(image.rectTransform, new Vector2(left, bottom), new Vector2(right, top));
        AddOutline(image.gameObject, new Color(0.4f, 0.46f, 0.48f, 0.8f));
        Text text = CreateText("Label", image.transform, c.Font, label, 20, Color.white, TextAnchor.MiddleCenter);
        SetRect(text.rectTransform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
        return image;
    }

    private static Button Hotspot(SceneContext c, string label, float left, float bottom, float right, float top, Color color)
    {
        return Hotspot(c, c.Stage, label, left, bottom, right, top, color);
    }

    private static Button Hotspot(SceneContext c, Transform parent, string label, float left, float bottom, float right, float top, Color color)
    {
        Color displayColor = color;
        if (c.HasBackdrop)
        {
            displayColor.a = Mathf.Min(displayColor.a, 0.76f);
        }

        Image image = CreateImage(SafeName(label), parent, displayColor);
        SetRect(image.rectTransform, new Vector2(left, bottom), new Vector2(right, top));
        AddOutline(image.gameObject, new Color(0.4f, 0.46f, 0.48f, 0.8f));
        Text text = CreateText("Label", image.transform, c.Font, label, 20, Color.white, TextAnchor.MiddleCenter);
        SetRect(text.rectTransform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
        Button button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.82f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        button.colors = colors;

        ScenePlaceholderHotspot hotspot = image.gameObject.AddComponent<ScenePlaceholderHotspot>();
        hotspot.Configure(label.Replace("\n", " "), c.Feedback);
        UnityEventTools.AddPersistentListener(button.onClick, hotspot.Select);
        return button;
    }

    private static Button Gateway(SceneContext c, string label, string targetScene, string requiredTaskId, float left, float bottom, float right, float top)
    {
        Button button = CreateButton("出口_" + SafeName(label), c.Stage, c.Font, label, GameUiTheme.Locked);
        SetRect(button.GetComponent<RectTransform>(), new Vector2(left, bottom), new Vector2(right, top));
        Image background = button.GetComponent<Image>();
        Text text = button.GetComponentInChildren<Text>();
        SceneGateway gateway = button.gameObject.AddComponent<SceneGateway>();
        gateway.Configure(targetScene, requiredTaskId, label, background, text, c.Feedback);
        UnityEventTools.AddPersistentListener(button.onClick, gateway.Enter);
        return button;
    }

    private static Text Label(SceneContext c, string value, float left, float bottom, float right, float top, int size, Color color, TextAnchor alignment)
    {
        Text text = CreateText(SafeName(value), c.Stage, c.Font, value, size, color, alignment);
        SetRect(text.rectTransform, new Vector2(left, bottom), new Vector2(right, top));
        return text;
    }

    private static string SafeName(string value)
    {
        return value.Replace("\n", "_").Replace("/", "_").Replace(" ", "_");
    }

    private static void EnsureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        foreach (SceneDefinition definition in Definitions)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(definition.Path) != null)
            {
                scenes.Add(new EditorBuildSettingsScene(definition.Path, true));
            }
        }

        if (scenes.Count == Definitions.Length)
        {
            EditorBuildSettings.scenes = scenes.ToArray();
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(Definitions[0].Path);
        }
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Background;
        camera.orthographic = true;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("GameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static Text CreateText(string objectName, Transform parent, Font font, string value, int size, Color color, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Button CreateButton(string objectName, Transform parent, Font font, string label, Color color)
    {
        Image image = CreateImage(objectName, parent, color);
        AddOutline(image.gameObject, new Color(0.32f, 0.39f, 0.41f, 1f));
        Button button = image.gameObject.AddComponent<Button>();
        Text text = CreateText("Label", image.transform, font, label, 18, Color.white, TextAnchor.MiddleCenter);
        text.fontStyle = FontStyle.Bold;
        Stretch(text.rectTransform);
        return button;
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject target = new GameObject(objectName, typeof(RectTransform));
        target.transform.SetParent(parent, false);
        return target.GetComponent<RectTransform>();
    }

    private static void AddOutline(GameObject target, Color color)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private static void Stretch(RectTransform rect)
    {
        SetRect(rect, Vector2.zero, Vector2.one);
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
