using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// 可沿单一方向拖动的 UI 抽屉。
/// 将本组件挂在带有透明 Image 的“拖拽区域”上，移动目标指定为“抽屉整体”。
/// EventSystem 的拖拽事件同时支持鼠标和手机触摸。
/// </summary>
[DisallowMultipleComponent]
public sealed class DraggableDrawer : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private enum PullDirection
    {
        Down,
        Up,
        Left,
        Right
    }

    [Header("Drawer")]
    [Tooltip("需要移动的抽屉整体，例如 Hierarchy 中的“抽屉1”。")]
    [SerializeField] private RectTransform drawerRoot;
    [SerializeField] private PullDirection pullDirection = PullDirection.Down;
    [Min(1f)]
    [SerializeField] private float openDistance = 260f;
    [Range(0f, 1f)]
    [SerializeField] private float openThreshold = 0.5f;
    [Min(0f)]
    [SerializeField] private float snapDuration = 0.2f;
    [SerializeField] private bool startOpened;

    [Header("Item (Optional)")]
    [Tooltip("可选：抽屉中道具的 CanvasGroup。道具会随抽屉拉开逐渐显示，完全打开后才能点击。")]
    [SerializeField] private CanvasGroup drawerItemCanvasGroup;
    [Range(0f, 0.95f)]
    [SerializeField] private float itemRevealStart = 0.35f;

    [Header("Events")]
    [SerializeField] private UnityEvent onOpened;
    [SerializeField] private UnityEvent onClosed;

    private RectTransform drawerParent;
    private Vector2 closedPosition;
    private Vector2 dragStartPointerPosition;
    private Vector2 dragStartDrawerPosition;
    private Coroutine snapCoroutine;
    private int activePointerId = int.MinValue;
    private bool isInitialized;
    private bool isOpen;

    public bool IsOpen => isOpen;
    public float OpenProgress => GetOpenProgress();

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized || drawerRoot == null)
        {
            return;
        }

        drawerParent = drawerRoot.parent as RectTransform;
        if (drawerParent == null)
        {
            Debug.LogError("抽屉整体必须放在一个 UI RectTransform 父物体下。", this);
            return;
        }

        closedPosition = drawerRoot.anchoredPosition;
        isInitialized = true;
        SetProgressImmediately(startOpened ? 1f : 0f);
        isOpen = startOpened;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Initialize();
        if (!isInitialized || activePointerId != int.MinValue)
        {
            return;
        }

        if (!TryGetLocalPointerPosition(eventData, out dragStartPointerPosition))
        {
            return;
        }

        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }

        activePointerId = eventData.pointerId;
        dragStartDrawerPosition = drawerRoot.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isInitialized || eventData.pointerId != activePointerId)
        {
            return;
        }

        if (!TryGetLocalPointerPosition(eventData, out Vector2 pointerPosition))
        {
            return;
        }

        Vector2 direction = GetDirectionVector();
        Vector2 pointerDelta = pointerPosition - dragStartPointerPosition;
        float distanceAlongDirection = Vector2.Dot(pointerDelta, direction);
        Vector2 desiredPosition = dragStartDrawerPosition + direction * distanceAlongDirection;
        float desiredProgress = Vector2.Dot(desiredPosition - closedPosition, direction) / openDistance;

        SetProgressImmediately(Mathf.Clamp01(desiredProgress));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isInitialized || eventData.pointerId != activePointerId)
        {
            return;
        }

        activePointerId = int.MinValue;
        bool shouldOpen = GetOpenProgress() >= openThreshold;
        StartSnap(shouldOpen ? 1f : 0f, true);
    }

    /// <summary>供存档读取或其他剧情脚本直接打开抽屉。</summary>
    public void Open(bool animate = true)
    {
        Initialize();
        if (!isInitialized)
        {
            return;
        }

        if (animate)
        {
            StartSnap(1f, true);
        }
        else
        {
            SetProgressImmediately(1f);
            SetOpenState(true, false);
        }
    }

    /// <summary>供存档读取或其他剧情脚本直接关闭抽屉。</summary>
    public void Close(bool animate = true)
    {
        Initialize();
        if (!isInitialized)
        {
            return;
        }

        if (animate)
        {
            StartSnap(0f, true);
        }
        else
        {
            SetProgressImmediately(0f);
            SetOpenState(false, false);
        }
    }

    private void StartSnap(float targetProgress, bool invokeEvent)
    {
        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
        }

        snapCoroutine = StartCoroutine(SnapTo(targetProgress, invokeEvent));
    }

    private IEnumerator SnapTo(float targetProgress, bool invokeEvent)
    {
        float startProgress = GetOpenProgress();

        if (snapDuration <= 0f || Mathf.Approximately(startProgress, targetProgress))
        {
            SetProgressImmediately(targetProgress);
            SetOpenState(targetProgress >= 1f, invokeEvent);
            snapCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < snapDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / snapDuration);
            float easedTime = 1f - Mathf.Pow(1f - normalizedTime, 3f);
            SetProgressImmediately(Mathf.Lerp(startProgress, targetProgress, easedTime));
            yield return null;
        }

        SetProgressImmediately(targetProgress);
        SetOpenState(targetProgress >= 1f, invokeEvent);
        snapCoroutine = null;
    }

    private void SetProgressImmediately(float progress)
    {
        progress = Mathf.Clamp01(progress);
        drawerRoot.anchoredPosition = closedPosition + GetDirectionVector() * (openDistance * progress);
        RefreshItem(progress);
    }

    private float GetOpenProgress()
    {
        if (!isInitialized || openDistance <= 0f)
        {
            return 0f;
        }

        float distance = Vector2.Dot(drawerRoot.anchoredPosition - closedPosition, GetDirectionVector());
        return Mathf.Clamp01(distance / openDistance);
    }

    private void RefreshItem(float progress)
    {
        if (drawerItemCanvasGroup == null)
        {
            return;
        }

        float visibleProgress = Mathf.InverseLerp(itemRevealStart, 1f, progress);
        drawerItemCanvasGroup.alpha = visibleProgress;

        bool canInteract = progress >= 0.99f;
        drawerItemCanvasGroup.interactable = canInteract;
        drawerItemCanvasGroup.blocksRaycasts = canInteract;
    }

    private void SetOpenState(bool open, bool invokeEvent)
    {
        bool stateChanged = isOpen != open;
        isOpen = open;

        if (!invokeEvent || !stateChanged)
        {
            return;
        }

        if (open)
        {
            onOpened?.Invoke();
        }
        else
        {
            onClosed?.Invoke();
        }
    }

    private bool TryGetLocalPointerPosition(PointerEventData eventData, out Vector2 localPosition)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            drawerParent,
            eventData.position,
            eventData.pressEventCamera,
            out localPosition);
    }

    private Vector2 GetDirectionVector()
    {
        switch (pullDirection)
        {
            case PullDirection.Up:
                return Vector2.up;
            case PullDirection.Left:
                return Vector2.left;
            case PullDirection.Right:
                return Vector2.right;
            default:
                return Vector2.down;
        }
    }

    private void OnDisable()
    {
        activePointerId = int.MinValue;

        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        openDistance = Mathf.Max(1f, openDistance);
        snapDuration = Mathf.Max(0f, snapDuration);

        if (drawerRoot != null && transform.IsChildOf(drawerRoot) == false)
        {
            Debug.LogWarning("拖拽区域通常应当是抽屉整体的子物体。", this);
        }
    }
#endif
}
