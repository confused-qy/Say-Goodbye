using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SayGoodbye.Core
{
    [DisallowMultipleComponent]
    public sealed class PrototypeMainMenu : MonoBehaviour
    {
        private static PrototypeMainMenu instance;
        private GameFlowManager flow;
        private Font uiFont;

        public static void Show(GameFlowManager gameFlow)
        {
            if (instance != null || gameFlow == null)
            {
                return;
            }

            GameObject root = new GameObject("PrototypeMainMenu");
            instance = root.AddComponent<PrototypeMainMenu>();
            instance.flow = gameFlow;
            instance.Build();
        }

        private void Build()
        {
            uiFont = GameUiTheme.ChineseFont;
            EnsureEventSystem();

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            gameObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Image background = CreateImage("Background", transform, new Color(0.025f, 0.035f, 0.045f, 0.68f));
            Stretch(background.rectTransform);

            RectTransform safeArea = CreateRect("安全区域", transform);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            Image contentShade = CreateImage("ContentShade", safeArea, new Color(0.025f, 0.035f, 0.045f, 0.88f));
            SetRect(contentShade.rectTransform, new Vector2(0f, 0f), new Vector2(0.72f, 1f));

            Image accent = CreateImage("Accent", safeArea, new Color(0.76f, 0.47f, 0.31f, 1f));
            SetRect(accent.rectTransform, new Vector2(0.13f, 0.2f), new Vector2(0.145f, 0.8f));

            Text eyebrow = CreateText("Eyebrow", safeArea, "关于照护、记忆与告别的故事", 28, new Color(0.76f, 0.66f, 0.56f, 1f), TextAnchor.MiddleLeft);
            SetRect(eyebrow.rectTransform, new Vector2(0.19f, 0.72f), new Vector2(0.82f, 0.79f));

            Text title = CreateText("Title", safeArea, "好好\n说再见", 108, Color.white, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            title.lineSpacing = 0.82f;
            SetRect(title.rectTransform, new Vector2(0.19f, 0.4f), new Vector2(0.68f, 0.72f));

            Text status = CreateText("Status", safeArea, "第一阶段叙事原型  /  场景与任务确认测试", 24, new Color(0.68f, 0.72f, 0.73f, 1f), TextAnchor.MiddleLeft);
            SetRect(status.rectTransform, new Vector2(0.19f, 0.32f), new Vector2(0.68f, 0.38f));

            Button start = CreateButton("StartButton", safeArea, "开始故事", GameUiTheme.Current);
            SetRect(start.GetComponent<RectTransform>(), new Vector2(0.19f, 0.18f), new Vector2(0.46f, 0.29f));
            start.onClick.AddListener(StartPrototype);

            if (flow.HasSave)
            {
                Button continueButton = CreateButton("ContinueButton", safeArea, "继续", new Color(0.18f, 0.32f, 0.32f, 1f));
                SetRect(continueButton.GetComponent<RectTransform>(), new Vector2(0.48f, 0.18f), new Vector2(0.66f, 0.29f));
                continueButton.onClick.AddListener(ContinuePrototype);
            }

            Text note = CreateText("Note", safeArea, "中文占位 · 任务确认解锁 · 安全区适配 · 自动存档", 20, new Color(0.52f, 0.57f, 0.59f, 1f), TextAnchor.MiddleRight);
            SetRect(note.rectTransform, new Vector2(0.55f, 0.04f), new Vector2(0.94f, 0.1f));
        }

        private void StartPrototype()
        {
            flow.StartNewGame();
            SceneManager.LoadScene(SceneCatalog.Prologue);
        }

        private void ContinuePrototype()
        {
            if (flow.LoadGame())
            {
                SceneManager.LoadScene(StoryFlowSequence.Get(flow.CurrentFlowStep).SceneName);
            }
        }

        private void Dismiss()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private Text CreateText(string objectName, Transform parent, string value, int fontSize, Color color, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = uiFont;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Button CreateButton(string objectName, Transform parent, string label, Color color)
        {
            Image image = CreateImage(objectName, parent, color);
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            button.colors = colors;

            Text text = CreateText("Label", image.transform, label, 25, Color.white, TextAnchor.MiddleCenter);
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

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(eventSystem);
        }

        private static void Stretch(RectTransform rect)
        {
            SetRect(rect, Vector2.zero, Vector2.one);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
