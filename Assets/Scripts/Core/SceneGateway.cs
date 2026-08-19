using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SayGoodbye.Core
{
    [DisallowMultipleComponent]
    public sealed class SceneGateway : MonoBehaviour
    {
        [SerializeField] private string targetScene;
        [SerializeField] private string requiredTaskId;
        [SerializeField] private string displayName;
        [SerializeField] private Image background;
        [SerializeField] private Text label;
        [SerializeField] private Text feedback;

        public void Configure(string sceneName, string requiredTask, string title, Image image, Text text, Text feedbackTarget)
        {
            targetScene = sceneName;
            requiredTaskId = requiredTask;
            displayName = title;
            background = image;
            label = text;
            feedback = feedbackTarget;
            Refresh();
        }

        private void OnEnable()
        {
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.StateChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.StateChanged -= Refresh;
            }
        }

        public void Enter()
        {
            GameFlowManager flow = GameFlowManager.Instance;
            if (flow != null && !flow.IsTaskConfirmed(requiredTaskId))
            {
                if (feedback != null)
                {
                    StoryFlowStep required = StoryFlowSequence.FindById(requiredTaskId);
                    feedback.text = required != null
                        ? "这里暂时无法进入。请先提交任务：“" + required.TaskTitle + "”。"
                        : "这里暂时无法进入。继续完成当前心愿后会开放。";
                }
                return;
            }

            if (!string.IsNullOrWhiteSpace(targetScene))
            {
                SceneManager.LoadScene(targetScene);
            }
        }

        private void Refresh()
        {
            bool current = string.Equals(SceneManager.GetActiveScene().name, targetScene, System.StringComparison.Ordinal);
            bool unlocked = GameFlowManager.Instance == null || GameFlowManager.Instance.IsTaskConfirmed(requiredTaskId);
            if (background != null)
            {
                background.color = current ? GameUiTheme.Current : unlocked ? GameUiTheme.Unlocked : GameUiTheme.Locked;
            }

            if (label != null)
            {
                label.text = current ? "● " + displayName : unlocked ? "→ " + displayName : "锁  " + displayName;
                label.color = unlocked || current ? Color.white : GameUiTheme.MutedText;
            }
        }
    }
}
