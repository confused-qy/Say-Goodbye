using UnityEngine;
using UnityEngine.SceneManagement;

namespace SayGoodbye.Core
{
    public static class GameBootstrap
    {
        private static bool subscribedToSceneLoads;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateServices()
        {
            if (GameFlowManager.Instance == null)
            {
                GameObject services = new GameObject("GameServices");
                services.AddComponent<GameFlowManager>();
            }

            if (!subscribedToSceneLoads)
            {
                SceneManager.sceneLoaded += HandleSceneLoaded;
                subscribedToSceneLoads = true;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreatePrototypeEntry()
        {
            ShowMenuForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ShowMenuForScene(scene);
        }

        private static void ShowMenuForScene(Scene scene)
        {
            if (scene.name == SceneCatalog.Boot)
            {
                PrototypeMainMenu.Show(GameFlowManager.Instance);
            }
            else if (scene.name == SceneCatalog.Prologue)
            {
                OpeningSequenceOverlay.Show(GameFlowManager.Instance);
            }
        }
    }
}
