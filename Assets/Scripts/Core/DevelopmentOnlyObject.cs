using UnityEngine;

namespace SayGoodbye.Core
{
    [DisallowMultipleComponent]
    public sealed class DevelopmentOnlyObject : MonoBehaviour
    {
        private void Awake()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
