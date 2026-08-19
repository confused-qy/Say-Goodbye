using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SayGoodbye.Core
{
    [DisallowMultipleComponent]
    public sealed class SceneFlowPresenter : MonoBehaviour
    {
        [SerializeField] private Text sceneTitle;
        [SerializeField] private Text objective;
        [SerializeField] private Text feedback;
        private GameFlowManager flow;

        public void Configure(Text titleTarget, Text objectiveTarget, Text feedbackTarget)
        {
            sceneTitle = titleTarget;
            objective = objectiveTarget;
            feedback = feedbackTarget;
        }

        private void Start()
        {
            flow = GameFlowManager.Instance;
            if (flow != null)
            {
                flow.StateChanged += Refresh;
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (flow != null)
            {
                flow.StateChanged -= Refresh;
            }
        }

        private void Refresh()
        {
            string activeScene = SceneManager.GetActiveScene().name;
            int stepIndex = flow != null ? flow.CurrentFlowStep : StoryFlowSequence.FindFirstForScene(activeScene);
            StoryFlowStep step = StoryFlowSequence.Get(stepIndex);

            if (sceneTitle != null)
            {
                sceneTitle.text = "场景  /  " + SceneAccessCatalog.DisplayNameFor(activeScene);
            }

            if (objective != null)
            {
                int completed = flow != null ? flow.CompletedRequirementCount(step) : 0;
                objective.text = ChapterName(step.Chapter) + "  /  " + step.Objective
                    + (step.Requirements.Length > 0 ? "  [" + completed + "/" + step.Requirements.Length + "]" : string.Empty);
            }

            if (feedback != null)
            {
                feedback.text = flow != null && flow.IsCurrentTaskReady()
                    ? "目标已经全部满足，请点击“任务确认”提交。"
                    : "当前目标：" + step.Objective;
            }
        }

        private static string ChapterName(GameChapter chapter)
        {
            switch (chapter)
            {
                case GameChapter.WishOne: return "心愿一 · 岁月之歌";
                case GameChapter.WishTwo: return "心愿二 · 我的美丽";
                case GameChapter.WishThree: return "心愿三 · 好好相见，好好告别";
                case GameChapter.EndingComic: return "好好告别";
                case GameChapter.Epilogue: return "三个月后";
                case GameChapter.GameCompleted: return "完成";
                default: return "序章";
            }
        }
    }
}
