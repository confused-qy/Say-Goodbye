using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SayGoodbye.Core
{
    [DisallowMultipleComponent]
    public sealed class TaskCompletionOverlay : MonoBehaviour
    {
        private static TaskCompletionOverlay instance;

        private GameFlowManager flow;
        private Text titleLabel;
        private Text objectiveLabel;
        private Text checklistLabel;
        private Text summaryLabel;
        private Text statusLabel;
        private Button confirmButton;

        public static void Show(GameFlowManager gameFlow)
        {
            if (instance != null)
            {
                instance.Refresh();
                return;
            }

            if (gameFlow == null)
            {
                return;
            }

            GameObject root = new GameObject("TaskCompletionOverlay", typeof(RectTransform));
            instance = root.AddComponent<TaskCompletionOverlay>();
            instance.flow = gameFlow;
            instance.Build();
        }

        private void Build()
        {
            EnsureEventSystem();

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 3200;
            gameObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Image veil = CreateImage("遮罩", transform, new Color(0.01f, 0.015f, 0.02f, 0.82f));
            Stretch(veil.rectTransform);

            RectTransform safeArea = CreateRect("安全区域", transform);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            Image panel = CreateImage("任务确认面板", safeArea, new Color(0.045f, 0.06f, 0.07f, 0.99f));
            SetRect(panel.rectTransform, new Vector2(0.18f, 0.12f), new Vector2(0.82f, 0.88f));
            AddOutline(panel.gameObject, new Color(0.58f, 0.45f, 0.29f, 0.95f));

            Text eyebrow = CreateText("章节", panel.transform, "任务完成确认", 22, GameUiTheme.Current, TextAnchor.MiddleLeft);
            SetRect(eyebrow.rectTransform, new Vector2(0.07f, 0.86f), new Vector2(0.55f, 0.94f));

            titleLabel = CreateText("任务标题", panel.transform, string.Empty, 42, Color.white, TextAnchor.MiddleLeft);
            titleLabel.fontStyle = FontStyle.Bold;
            SetRect(titleLabel.rectTransform, new Vector2(0.07f, 0.74f), new Vector2(0.93f, 0.87f));

            objectiveLabel = CreateText("任务目标", panel.transform, string.Empty, 21, GameUiTheme.MutedText, TextAnchor.UpperLeft);
            SetRect(objectiveLabel.rectTransform, new Vector2(0.07f, 0.64f), new Vector2(0.93f, 0.75f));

            Image checklist = CreateImage("条件清单", panel.transform, new Color(0.075f, 0.095f, 0.105f, 1f));
            SetRect(checklist.rectTransform, new Vector2(0.07f, 0.31f), new Vector2(0.93f, 0.63f));
            checklistLabel = CreateText("条件文字", checklist.transform, string.Empty, 23, Color.white, TextAnchor.UpperLeft);
            checklistLabel.lineSpacing = 1.35f;
            SetRect(checklistLabel.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f));

            summaryLabel = CreateText("完成说明", panel.transform, string.Empty, 20, new Color(0.83f, 0.80f, 0.72f, 1f), TextAnchor.UpperLeft);
            SetRect(summaryLabel.rectTransform, new Vector2(0.07f, 0.20f), new Vector2(0.93f, 0.30f));

            Button cancel = CreateButton("返回", panel.transform, "返回继续调查", new Color(0.22f, 0.25f, 0.27f, 1f));
            SetRect(cancel.GetComponent<RectTransform>(), new Vector2(0.07f, 0.07f), new Vector2(0.37f, 0.17f));
            cancel.onClick.AddListener(Close);

            confirmButton = CreateButton("确认提交", panel.transform, "确认提交并继续", GameUiTheme.Current);
            SetRect(confirmButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0.07f), new Vector2(0.93f, 0.17f));
            confirmButton.onClick.AddListener(Confirm);

            statusLabel = CreateText("确认状态", panel.transform, string.Empty, 17, GameUiTheme.MutedText, TextAnchor.MiddleRight);
            SetRect(statusLabel.rectTransform, new Vector2(0.38f, 0.01f), new Vector2(0.93f, 0.065f));

            flow.StateChanged += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            if (flow == null || titleLabel == null)
            {
                return;
            }

            StoryFlowStep step = flow.CurrentTask();
            titleLabel.text = step.TaskTitle;
            objectiveLabel.text = "当前目标：" + step.Objective;
            summaryLabel.text = "提交后：" + step.CompletionSummary;

            StringBuilder checklist = new StringBuilder();
            if (step.Requirements.Length == 0)
            {
                checklist.Append("这个场景没有待提交任务。");
            }
            else
            {
                foreach (StoryTaskRequirement requirement in step.Requirements)
                {
                    bool done = flow.IsRequirementCompleted(requirement.Id);
                    checklist.Append(done ? "✓  " : "○  ");
                    checklist.Append(requirement.Label);
                    checklist.Append('\n');
                }
            }

            checklistLabel.text = checklist.ToString().TrimEnd();
            bool ready = flow.IsCurrentTaskReady();
            confirmButton.interactable = ready;
            confirmButton.GetComponent<Image>().color = ready ? GameUiTheme.Current : GameUiTheme.Locked;
            statusLabel.text = ready
                ? "条件已满足，请确认后解锁下一任务"
                : "请完成全部条件；未确认前不会解锁下一场景";
            statusLabel.color = ready ? GameUiTheme.Unlocked : GameUiTheme.MutedText;
        }

        private void Confirm()
        {
            string destination;
            string feedback;
            if (!flow.ConfirmCurrentTask(out destination, out feedback))
            {
                statusLabel.text = feedback;
                return;
            }

            string active = SceneManager.GetActiveScene().name;
            Close();
            if (!string.IsNullOrEmpty(destination) && destination != active)
            {
                SceneManager.LoadScene(destination);
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

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject target = new GameObject(objectName, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            return target.GetComponent<RectTransform>();
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
            Text text = CreateText("文字", image.transform, label, 21, Color.white, TextAnchor.MiddleCenter);
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
