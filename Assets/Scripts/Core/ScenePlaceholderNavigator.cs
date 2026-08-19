using UnityEngine;
using UnityEngine.SceneManagement;

namespace SayGoodbye.Core
{
    [DisallowMultipleComponent]
    public sealed class ScenePlaceholderNavigator : MonoBehaviour
    {
        [SerializeField] private string previousScene;
        [SerializeField] private string nextScene;

        public void Configure(string previous, string next)
        {
            previousScene = previous;
            nextScene = next;
        }

        public void GoPrevious()
        {
            GameFlowManager flow = GameFlowManager.Instance;
            Load(flow != null ? flow.MoveFlowStepForTesting(-1) : previousScene);
        }

        public void GoNext()
        {
            GameFlowManager flow = GameFlowManager.Instance;
            Load(flow != null ? flow.MoveFlowStepForTesting(1) : nextScene);
        }

        public void GoToBoot()
        {
            Load(SceneCatalog.Boot);
        }

        public void ToggleSceneMap()
        {
            TestSceneMap.Toggle(GameFlowManager.Instance);
        }

        public void ShowTaskConfirmation()
        {
            TaskCompletionOverlay.Show(GameFlowManager.Instance);
        }

        private static void Load(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}
