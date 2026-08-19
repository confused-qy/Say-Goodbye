using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SayGoodbye.Core
{
    [DisallowMultipleComponent]
    public sealed class TestSceneMap : MonoBehaviour
    {
        private sealed class NodeView
        {
            public SceneAccessNode Node;
            public Image Background;
            public Text Label;
            public Button Button;
        }

        private static TestSceneMap instance;
        private readonly List<NodeView> nodeViews = new List<NodeView>();
        private GameFlowManager flow;
        private Text progressLabel;
        private Text messageLabel;

        public static void Toggle(GameFlowManager gameFlow)
        {
            if (instance != null)
            {
                Destroy(instance.gameObject);
                return;
            }

            Show(gameFlow);
        }

        public static void Show(GameFlowManager gameFlow)
        {
            if (instance != null)
            {
                return;
            }

            GameObject root = new GameObject("TestSceneMap");
            instance = root.AddComponent<TestSceneMap>();
            instance.flow = gameFlow;
            instance.Build();
        }

        private void Build()
        {
            EnsureEventSystem();
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 3000;
            gameObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Image veil = CreateImage("暗色遮罩", transform, new Color(0.015f, 0.02f, 0.025f, 0.94f));
            Stretch(veil.rectTransform);

            Image panel = CreateImage("地图面板", transform, new Color(0.055f, 0.07f, 0.08f, 0.99f));
            SetRect(panel.rectTransform, new Vector2(0.045f, 0.055f), new Vector2(0.955f, 0.945f));
            AddOutline(panel.gameObject, new Color(0.35f, 0.40f, 0.39f, 0.9f));

            Text title = CreateText("标题", panel.transform, "测试场景地图", 42, Color.white, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0.04f, 0.88f), new Vector2(0.38f, 0.97f));

            Text subtitle = CreateText("说明", panel.transform, "地点只会在任务条件满足并确认提交后开放。", 20, GameUiTheme.MutedText, TextAnchor.MiddleLeft);
            SetRect(subtitle.rectTransform, new Vector2(0.04f, 0.83f), new Vector2(0.63f, 0.89f));

            progressLabel = CreateText("进度", panel.transform, string.Empty, 20, new Color(0.92f, 0.75f, 0.49f, 1f), TextAnchor.MiddleRight);
            SetRect(progressLabel.rectTransform, new Vector2(0.62f, 0.88f), new Vector2(0.86f, 0.96f));

            Button close = CreateButton("关闭", panel.transform, "关闭 ×", new Color(0.24f, 0.27f, 0.29f, 1f));
            SetRect(close.GetComponent<RectTransform>(), new Vector2(0.87f, 0.88f), new Vector2(0.96f, 0.96f));
            close.onClick.AddListener(Close);

            string[] groups = { "故事入口", "现实地点", "记忆之家", "心愿活动", "尾声" };
            float[] lefts = { 0.04f, 0.205f, 0.37f, 0.535f, 0.78f };
            float[] rights = { 0.19f, 0.355f, 0.52f, 0.765f, 0.96f };
            for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++)
            {
                string group = groups[groupIndex];
                Text heading = CreateText("分组_" + group, panel.transform, group, 23, Color.white, TextAnchor.MiddleLeft);
                heading.fontStyle = FontStyle.Bold;
                SetRect(heading.rectTransform, new Vector2(lefts[groupIndex], 0.75f), new Vector2(rights[groupIndex], 0.82f));

                int row = 0;
                foreach (SceneAccessNode node in SceneAccessCatalog.Nodes)
                {
                    if (node.GroupName != group)
                    {
                        continue;
                    }

                    float top = 0.735f - row * 0.125f;
                    Button button = CreateButton("场景_" + node.SceneName, panel.transform, node.DisplayName, GameUiTheme.Locked);
                    SetRect(button.GetComponent<RectTransform>(), new Vector2(lefts[groupIndex], top - 0.095f), new Vector2(rights[groupIndex], top));
                    SceneAccessNode captured = node;
                    button.onClick.AddListener(() => Open(captured));
                    nodeViews.Add(new NodeView
                    {
                        Node = node,
                        Background = button.GetComponent<Image>(),
                        Label = button.GetComponentInChildren<Text>(),
                        Button = button
                    });
                    row++;
                }
            }

            Image legend = CreateImage("图例", panel.transform, new Color(0.08f, 0.10f, 0.11f, 1f));
            SetRect(legend.rectTransform, new Vector2(0.04f, 0.055f), new Vector2(0.55f, 0.145f));
            Text legendText = CreateText("图例文字", legend.transform, "● 金色：当前场景    ✓ 青绿：已开放    锁 灰色：未开放", 20, Color.white, TextAnchor.MiddleCenter);
            Stretch(legendText.rectTransform);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Button rewind = CreateButton("回退测试", panel.transform, "定位任务 −1", new Color(0.24f, 0.27f, 0.29f, 1f));
            SetRect(rewind.GetComponent<RectTransform>(), new Vector2(0.59f, 0.055f), new Vector2(0.72f, 0.145f));
            rewind.onClick.AddListener(() => ChangeStep(-1));

            Button advance = CreateButton("推进测试", panel.transform, "定位任务 +1", GameUiTheme.Pending);
            SetRect(advance.GetComponent<RectTransform>(), new Vector2(0.735f, 0.055f), new Vector2(0.87f, 0.145f));
            advance.onClick.AddListener(() => ChangeStep(1));
#endif

            messageLabel = CreateText("反馈", panel.transform, "点击青绿色场景即可跳转。定位任务不会解锁任何场景。", 17, GameUiTheme.MutedText, TextAnchor.MiddleRight);
            SetRect(messageLabel.rectTransform, new Vector2(0.58f, 0.005f), new Vector2(0.96f, 0.05f));

            Refresh();
            if (flow != null)
            {
                flow.StateChanged += Refresh;
            }
        }

        private void ChangeStep(int delta)
        {
            if (flow == null)
            {
                return;
            }

            flow.SetFlowStepForTesting(flow.CurrentFlowStep + delta);
            messageLabel.text = "仅调整了当前任务定位；场景仍须完成并确认前置任务才会开放。";
        }

        private void Open(SceneAccessNode node)
        {
            if (!SceneAccessCatalog.IsUnlocked(node.SceneName, flow))
            {
                messageLabel.text = "“" + node.DisplayName + "”尚未开放。请先提交任务：“"
                    + SceneAccessCatalog.RequirementLabel(node) + "”。";
                return;
            }

            SceneManager.LoadScene(node.SceneName);
        }

        private void Refresh()
        {
            if (progressLabel != null)
            {
                StoryFlowStep step = flow != null ? flow.CurrentTask() : StoryFlowSequence.Get(0);
                progressLabel.text = "当前任务  " + step.TaskTitle;
            }

            foreach (NodeView view in nodeViews)
            {
                SceneAccessState state = SceneAccessCatalog.StateFor(view.Node, flow);
                bool enabled = state != SceneAccessState.Locked;
                view.Button.interactable = true;
                view.Background.color = state == SceneAccessState.Current
                    ? GameUiTheme.Current
                    : enabled ? GameUiTheme.Unlocked : GameUiTheme.Locked;
                view.Label.text = state == SceneAccessState.Current
                    ? "● 当前\n" + view.Node.DisplayName
                    : enabled ? "✓ 已开放\n" + view.Node.DisplayName : "锁  未开放\n" + view.Node.DisplayName;
                view.Label.color = enabled || state == SceneAccessState.Current ? Color.white : GameUiTheme.MutedText;
            }
        }

        private void Close()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (flow != null)
            {
                flow.StateChanged -= Refresh;
            }

            if (instance == this)
            {
                instance = null;
            }
        }

        private static Text CreateText(string objectName, Transform parent, string value, int size, Color color, TextAnchor alignment)
        {
            GameObject target = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            target.transform.SetParent(parent, false);
            Text text = target.GetComponent<Text>();
            text.font = GameUiTheme.ChineseFont;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(string objectName, Transform parent, string label, Color color)
        {
            Image image = CreateImage(objectName, parent, color);
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.88f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            button.colors = colors;
            Text text = CreateText("文字", image.transform, label, 20, Color.white, TextAnchor.MiddleCenter);
            text.fontStyle = FontStyle.Bold;
            Stretch(text.rectTransform);
            return button;
        }

        private static Image CreateImage(string objectName, Transform parent, Color color)
        {
            GameObject target = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            target.transform.SetParent(parent, false);
            Image image = target.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void AddOutline(GameObject target, Color color)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                Object.DontDestroyOnLoad(new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
            }
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
}
