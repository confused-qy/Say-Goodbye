using UnityEngine;
using UnityEngine.UI;

namespace SayGoodbye.Core
{
    [DisallowMultipleComponent]
    public sealed class HospitalViewController : MonoBehaviour
    {
        [SerializeField] private GameObject leftView;
        [SerializeField] private GameObject rightView;
        [SerializeField] private Image leftButton;
        [SerializeField] private Image rightButton;
        [SerializeField] private Text feedback;

        public void Configure(GameObject left, GameObject right, Image leftControl, Image rightControl, Text feedbackTarget)
        {
            leftView = left;
            rightView = right;
            leftButton = leftControl;
            rightButton = rightControl;
            feedback = feedbackTarget;
            Refresh("Left");
        }

        private void Start()
        {
            string view = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentHospitalView : "Left";
            Refresh(view);
        }

        public void ShowLeft()
        {
            GameFlowManager flow = GameFlowManager.Instance;
            if (flow != null)
            {
                flow.SetHospitalView("Left");
            }

            Refresh("Left");
            if (feedback != null)
            {
                feedback.text = "已切换到病房左侧。这里可以和林姨交谈、查看信息卡与心愿清单。";
            }
        }

        public void ShowRight()
        {
            GameFlowManager flow = GameFlowManager.Instance;
            string message = "已切换到病房右侧。这里可以查看资料柜、录音机和物品。";
            if (flow != null)
            {
                flow.SetHospitalView("Right");
                StoryFlowStep step = flow.CurrentTask();
                string requirementId = step.FindRequirement("switch_hospital_right") != null
                    ? "switch_hospital_right"
                    : step.FindRequirement("switch_wish_two_right") != null ? "switch_wish_two_right" : string.Empty;
                if (!string.IsNullOrEmpty(requirementId))
                {
                    flow.ReportTaskAction(requirementId, out message);
                }
            }

            Refresh("Right");
            if (feedback != null)
            {
                feedback.text = message;
            }
        }

        private void Refresh(string view)
        {
            bool showLeft = view != "Right";
            if (leftView != null)
            {
                leftView.SetActive(showLeft);
            }

            if (rightView != null)
            {
                rightView.SetActive(!showLeft);
            }

            if (leftButton != null)
            {
                leftButton.color = showLeft ? GameUiTheme.Current : GameUiTheme.Unlocked;
            }

            if (rightButton != null)
            {
                rightButton.color = showLeft ? GameUiTheme.Unlocked : GameUiTheme.Current;
            }
        }
    }
}
