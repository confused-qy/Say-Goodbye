using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SayGoodbye.Core
{
    [DisallowMultipleComponent]
    public sealed class OpeningSequenceOverlay : MonoBehaviour
    {
        private static readonly string[] Titles =
        {
            "广场上的傍晚",
            "诊断",
            "回到老房子",
            "凌晨两点四十分",
            "安宁疗护",
            "收拾行李",
            "三号病床",
            "陈心怡"
        };

        private static readonly string[] Bodies =
        {
            "傍晚的广场上，林淑珍跟着音乐跳舞。\n她突然捂住肋部倒地。\n邻居喊道：“林姐晕了！快打120！”",
            "诊室里，医生把报告推到她面前。\n“肺癌晚期，有骨转移。最好尽快通知家属。”\n林淑珍沉默片刻：“别告诉我女儿，她在外地。”",
            "黄昏照进空荡的老房子。\n她站在桌前，轻轻抚摸那张泛黄的全家福。",
            "深夜，她疼得蜷缩在床上，手紧紧抓着床单。\n闹钟停在凌晨2:40。\n“疼……太疼了……”",
            "第二天，她扶着墙来到医生办公室。\n“有没有办法让我少疼一点？”\n医生建议她考虑以缓解疼痛为主的安宁疗护病房。",
            "她把全家福包进毛巾，擦净旧录音机，一起放进布袋。\n站在门口时，她最后回望了一眼老房子。",
            "安宁病房里，她把全家福和录音机放在床头。\n医生查房：“3床林淑珍，今天感觉怎么样？”\n“还行，今天没那么疼了。”",
            "我叫陈心怡，是安宁疗护病房的医务社工。\n我会通过访谈了解患者的心理、家庭和未了心愿，陪他们认真完成道谢、道歉、道爱与道别。\n今天，先从倾听林姨开始。"
        };

        private static OpeningSequenceOverlay instance;
        private GameFlowManager flow;
        private Text chapterLabel;
        private Text titleLabel;
        private Text bodyLabel;
        private Text pageLabel;
        private Button previousButton;
        private Button nextButton;
        private int page;

        public static void Show(GameFlowManager gameFlow)
        {
            if (instance != null)
            {
                return;
            }

            GameObject root = new GameObject("OpeningSequence");
            instance = root.AddComponent<OpeningSequenceOverlay>();
            instance.flow = gameFlow;
            instance.Build();
        }

        private void Build()
        {
            EnsureEventSystem();
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2500;
            gameObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Image veil = CreateImage("开幕遮罩", transform, new Color(0.02f, 0.027f, 0.034f, 0.58f));
            Stretch(veil.rectTransform);

            RectTransform safeArea = CreateRect("安全区域", transform);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            Image card = CreateImage("叙事卡片", safeArea, new Color(0.045f, 0.058f, 0.066f, 0.96f));
            SetRect(card.rectTransform, new Vector2(0.10f, 0.12f), new Vector2(0.56f, 0.88f));
            AddOutline(card.gameObject, new Color(0.54f, 0.42f, 0.29f, 0.9f));

            Image accent = CreateImage("暖色线", card.transform, GameUiTheme.Current);
            SetRect(accent.rectTransform, new Vector2(0f, 0f), new Vector2(0.018f, 1f));

            chapterLabel = CreateText("章节", card.transform, "序章 · 开幕", 24, new Color(0.91f, 0.73f, 0.48f, 1f), TextAnchor.MiddleLeft);
            SetRect(chapterLabel.rectTransform, new Vector2(0.09f, 0.80f), new Vector2(0.88f, 0.90f));

            titleLabel = CreateText("幕标题", card.transform, string.Empty, 52, Color.white, TextAnchor.MiddleLeft);
            titleLabel.fontStyle = FontStyle.Bold;
            SetRect(titleLabel.rectTransform, new Vector2(0.09f, 0.62f), new Vector2(0.88f, 0.80f));

            bodyLabel = CreateText("幕正文", card.transform, string.Empty, 29, new Color(0.89f, 0.90f, 0.88f, 1f), TextAnchor.UpperLeft);
            bodyLabel.lineSpacing = 1.35f;
            SetRect(bodyLabel.rectTransform, new Vector2(0.09f, 0.30f), new Vector2(0.88f, 0.61f));

            pageLabel = CreateText("页数", card.transform, string.Empty, 19, GameUiTheme.MutedText, TextAnchor.MiddleLeft);
            SetRect(pageLabel.rectTransform, new Vector2(0.09f, 0.20f), new Vector2(0.32f, 0.27f));

            previousButton = CreateButton("上一幕", card.transform, "上一幕", new Color(0.23f, 0.26f, 0.28f, 1f));
            SetRect(previousButton.GetComponent<RectTransform>(), new Vector2(0.09f, 0.07f), new Vector2(0.36f, 0.18f));
            previousButton.onClick.AddListener(Previous);

            nextButton = CreateButton("下一幕", card.transform, "下一幕", GameUiTheme.Current);
            SetRect(nextButton.GetComponent<RectTransform>(), new Vector2(0.40f, 0.07f), new Vector2(0.88f, 0.18f));
            nextButton.onClick.AddListener(Next);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Button skip = CreateButton("跳过开幕", safeArea, "跳过开幕（测试）", new Color(0.08f, 0.10f, 0.11f, 0.82f));
            SetRect(skip.GetComponent<RectTransform>(), new Vector2(0.84f, 0.055f), new Vector2(0.95f, 0.12f));
            skip.onClick.AddListener(EnterHospital);
#endif

            Refresh();
        }

        private void Previous()
        {
            page = Mathf.Max(0, page - 1);
            Refresh();
        }

        private void Next()
        {
            if (page >= Titles.Length - 1)
            {
                EnterHospital();
                return;
            }

            page++;
            Refresh();
        }

        private void EnterHospital()
        {
            if (flow != null)
            {
                string feedback;
                flow.ReportTaskAction("read_prologue", out feedback);
                TaskCompletionOverlay.Show(flow);
            }
        }

        private void Refresh()
        {
            titleLabel.text = Titles[page];
            bodyLabel.text = Bodies[page];
            pageLabel.text = "0" + (page + 1) + "  /  0" + Titles.Length;
            previousButton.gameObject.SetActive(page > 0);
            nextButton.GetComponentInChildren<Text>().text = page == Titles.Length - 1 ? "完成开幕并确认" : "下一幕";
        }

        private void OnDestroy()
        {
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

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject target = new GameObject(objectName, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            return target.GetComponent<RectTransform>();
        }

        private static Button CreateButton(string objectName, Transform parent, string label, Color color)
        {
            Image image = CreateImage(objectName, parent, color);
            Button button = image.gameObject.AddComponent<Button>();
            Text text = CreateText("文字", image.transform, label, 23, Color.white, TextAnchor.MiddleCenter);
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
