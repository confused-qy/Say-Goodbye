using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 在安宁医院的左右固定视角之间切换，并播放淡出、切换、淡入动画。
/// 将本组件挂在 SceneLayer 上；CanvasGroup 会自动添加。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class HospitalViewSwitcher : MonoBehaviour
{
    private enum HospitalView
    {
        Left,
        Right
    }

    [Header("Views")]
    [SerializeField] private GameObject leftView;
    [SerializeField] private GameObject rightView;
    [SerializeField] private HospitalView initialView = HospitalView.Left;

    [Header("Navigation Buttons")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [Tooltip("开启后，左视角只显示向右按钮，右视角只显示向左按钮。")]
    [SerializeField] private bool hideUnavailableButton = true;

    [Header("Fade")]
    [Tooltip("NavigationLayer 上的 CanvasGroup，用于让左右箭头与场景一起淡入淡出。")]
    [SerializeField] private CanvasGroup navigationCanvasGroup;
    [Min(0f)]
    [SerializeField] private float fadeDuration = 0.25f;

    private CanvasGroup sceneCanvasGroup;
    private Coroutine transitionCoroutine;
    private HospitalView currentView;
    private bool isTransitioning;

    private void Awake()
    {
        sceneCanvasGroup = GetComponent<CanvasGroup>();

        if (leftButton != null)
        {
            leftButton.onClick.AddListener(ShowLeftView);
        }

        if (rightButton != null)
        {
            rightButton.onClick.AddListener(ShowRightView);
        }

        SetViewImmediately(initialView);
    }

    private void OnDestroy()
    {
        if (leftButton != null)
        {
            leftButton.onClick.RemoveListener(ShowLeftView);
        }

        if (rightButton != null)
        {
            rightButton.onClick.RemoveListener(ShowRightView);
        }
    }

    public void ShowLeftView()
    {
        SwitchTo(HospitalView.Left);
    }

    public void ShowRightView()
    {
        SwitchTo(HospitalView.Right);
    }

    private void SwitchTo(HospitalView targetView)
    {
        if (isTransitioning || targetView == currentView)
        {
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(TransitionTo(targetView));
    }

    private IEnumerator TransitionTo(HospitalView targetView)
    {
        isTransitioning = true;
        ClearSelectedButton();
        sceneCanvasGroup.blocksRaycasts = false;
        SetNavigationRaycasts(false);

        yield return Fade(1f, 0f);

        ActivateView(targetView);
        RefreshNavigationButtonVisibility();

        yield return Fade(0f, 1f);

        SetFadeAlpha(1f);
        sceneCanvasGroup.blocksRaycasts = true;
        SetNavigationRaycasts(true);
        isTransitioning = false;
        transitionCoroutine = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeDuration <= 0f)
        {
            SetFadeAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        SetFadeAlpha(from);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            SetFadeAlpha(Mathf.Lerp(from, to, progress));
            yield return null;
        }

        SetFadeAlpha(to);
    }

    private void SetViewImmediately(HospitalView targetView)
    {
        ActivateView(targetView);
        SetFadeAlpha(1f);
        sceneCanvasGroup.interactable = true;
        sceneCanvasGroup.blocksRaycasts = true;
        SetNavigationRaycasts(true);
        RefreshNavigationButtonVisibility();
        ClearSelectedButton();
    }

    private void ActivateView(HospitalView targetView)
    {
        currentView = targetView;

        if (leftView != null)
        {
            leftView.SetActive(targetView == HospitalView.Left);
        }

        if (rightView != null)
        {
            rightView.SetActive(targetView == HospitalView.Right);
        }
    }

    private void RefreshNavigationButtonVisibility()
    {
        if (hideUnavailableButton)
        {
            if (leftButton != null)
            {
                leftButton.gameObject.SetActive(currentView == HospitalView.Right);
            }

            if (rightButton != null)
            {
                rightButton.gameObject.SetActive(currentView == HospitalView.Left);
            }
        }
    }

    private void SetFadeAlpha(float alpha)
    {
        sceneCanvasGroup.alpha = alpha;

        if (navigationCanvasGroup != null)
        {
            navigationCanvasGroup.alpha = alpha;
        }
    }

    private void SetNavigationRaycasts(bool value)
    {
        if (navigationCanvasGroup == null)
        {
            return;
        }

        navigationCanvasGroup.blocksRaycasts = value;
    }

    private static void ClearSelectedButton()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (leftView != null && leftView == rightView)
        {
            Debug.LogWarning("左右视角不能引用同一个 GameObject。", this);
        }
    }
#endif
}
