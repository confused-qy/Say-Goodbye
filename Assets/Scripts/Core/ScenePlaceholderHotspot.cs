using UnityEngine;
using UnityEngine.UI;

namespace SayGoodbye.Core
{
    [DisallowMultipleComponent]
    public sealed class ScenePlaceholderHotspot : MonoBehaviour
    {
        [SerializeField] private string hotspotName;
        [SerializeField] private Text feedbackText;

        public string HotspotName { get { return hotspotName; } }

        public void Configure(string label, Text target)
        {
            hotspotName = label;
            feedbackText = target;
        }

        public void Select()
        {
            GameFlowManager flow = GameFlowManager.Instance;
            string feedback;
            if (flow != null)
            {
                flow.ReportHotspotInteraction(hotspotName, out feedback);
            }
            else
            {
                feedback = "已查看：“" + hotspotName + "”。";
            }

            if (feedbackText != null)
            {
                feedbackText.text = feedback;
            }
        }
    }
}
