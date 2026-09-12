using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 定义可交互对象向提示系统暴露的状态
public interface IInteractionPromptSource
{
// 推进 当前脚本 的当前步骤
    bool IsInteractionPromptEligible { get; }
    void TriggerInteraction();
}

public class PlayerInteractionPromptController : MonoBehaviour
{
// 保存 promptPrefab 引用
    [SerializeField] private GameObject promptPrefab;
    [SerializeField] private Vector3 worldOffset = new Vector3(0.75f, 0.8f, 0f);
// 设置 PlayerInteractionPromptController 的配置数值
    [SerializeField] private float promptScale = 1f;

// 同步 PlayerInteractionPromptController 的相关数据
    private readonly HashSet<IInteractionPromptSource> sources = new HashSet<IInteractionPromptSource>();
    private GameObject promptInstance;
// 保存 promptCanvas 引用
    private Canvas promptCanvas;
    private RectTransform promptContentRect;
    private Button promptButton;
// 保存 gameplayCamera 引用
    private Camera gameplayCamera;
    private bool promptPositionValid;

// 推进 PlayerInteractionPromptController 的当前步骤
    public static PlayerInteractionPromptController Instance { get; private set; }

// 初始化组件引用和运行状态
    private void Awake()
    {
// 同步 Awake 的状态
        Instance = this;
        EnsurePromptInstance();
    }

// 销毁时释放事件订阅和静态引用
    private void OnDestroy()
    {
// 检查 OnDestroy 的前置条件
        if (Instance == this)
        {
// 同步 OnDestroy 的内部状态
            Instance = null;
        }
    }

// 在显示前同步界面位置
    private void Update()
    {
        if (!Input.GetMouseButtonDown(0) || !IsPointerOverActivePrompt(Input.mousePosition))
        {
            return;
        }

        HandlePromptClicked();
    }

    private void LateUpdate()
    {
// 推进 LateUpdate 中的必要步骤
        EnsurePromptInstance();
        RemoveInvalidSources();
// 推进 LateUpdate 中的必要步骤（LateUpdate）
        UpdatePromptPosition();
        UpdatePromptVisibility();
    }

// 处理 RegisterSource 对应逻辑
    public static void RegisterSource(IInteractionPromptSource source)
    {
// RegisterSource 缺少引用时提前结束
        if (source == null || Instance == null)
        {
// 返回 RegisterSource 的处理结果
            return;
        }

// 使用 RegisterSource 所需功能
        Instance.sources.Add(source);
        Instance.UpdatePromptVisibility();
    }

// 处理 UnregisterSource 对应逻辑
    public static void UnregisterSource(IInteractionPromptSource source)
    {
// 缺少必要引用时退出 UnregisterSource
        if (source == null || Instance == null)
        {
// 返回 UnregisterSource 的处理结果
            return;
        }

// 使用 UnregisterSource 所需功能
        Instance.sources.Remove(source);
        Instance.UpdatePromptVisibility();
    }

// 刷新 RefreshSource 对应状态
    public static void RefreshSource(IInteractionPromptSource source)
    {
// 缺少必要引用时退出 RefreshSource
        if (Instance == null)
        {
// 返回 RefreshSource 的处理结果
            return;
        }

// 使用 RefreshSource 所需功能
        Instance.UpdatePromptVisibility();
    }

// 处理 EnsurePromptInstance 对应逻辑
    private void EnsurePromptInstance()
    {
// 缺少必要引用时退出 EnsurePromptInstance
        if (promptInstance != null || promptPrefab == null)
        {
// 返回 EnsurePromptInstance 的处理结果
            return;
        }

// 同步 EnsurePromptInstance 的内部状态
        promptInstance = Instantiate(promptPrefab);
        promptInstance.name = $"{promptPrefab.name} (Player Prompt)";
// 切换 EnsurePromptInstance 的显示状态
        promptInstance.SetActive(false);
        promptInstance.transform.localScale = Vector3.one * Mathf.Max(0.01f, promptScale);
// 同步 EnsurePromptInstance 的内部状态（EnsurePromptInstance）
        promptCanvas = promptInstance.GetComponentInChildren<Canvas>(true);
        promptContentRect = promptCanvas != null
// 使用 EnsurePromptInstance 所需功能
            ? promptCanvas.transform.Find("PromptContent") as RectTransform
            : null;

// 缺少必要引用时退出 EnsurePromptInstance（EnsurePromptInstance）
        if (promptContentRect == null && promptCanvas != null)
        {
// 完成 EnsurePromptInstance 的主要职责
            GameObject contentObject = new GameObject("PromptContent", typeof(RectTransform));
            promptContentRect = contentObject.transform as RectTransform;
// 使用 EnsurePromptInstance 所需功能（EnsurePromptInstance）
            promptContentRect.SetParent(promptCanvas.transform, false);
            promptContentRect.anchorMin = new Vector2(0.5f, 0.5f);
// 同步 EnsurePromptInstance 的内部状态（EnsurePromptInstance）（promptContentRect）
            promptContentRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptContentRect.pivot = new Vector2(0.5f, 0.5f);
// 在 EnsurePromptInstance 中继续当前处理
            promptContentRect.sizeDelta = Vector2.zero;

// 保存 originalChildren 引用
            List<Transform> originalChildren = new List<Transform>();
            for (int i = 0; i < promptCanvas.transform.childCount; i++)
            {
// 缓存 EnsurePromptInstance 所需引用
                Transform child = promptCanvas.transform.GetChild(i);
                if (child != promptContentRect)
                {
// 在 EnsurePromptInstance 中处理 Add
                    originalChildren.Add(child);
                }
            }

// 遍历全部元素
            foreach (Transform child in originalChildren)
            {
// 在 EnsurePromptInstance 中处理 SetParent
                child.SetParent(promptContentRect, true);
            }
        }

// 检查 EnsurePromptInstance 的前置条件
        if (promptCanvas != null)
        {
// 同步 EnsurePromptInstance 的内部状态（EnsurePromptInstance）（promptCanvas）
            promptCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            promptCanvas.worldCamera = GetGameplayCamera();
        }

        EnsurePromptClickTarget();
    }

    private void EnsurePromptClickTarget()
    {
        if (promptInstance == null || promptCanvas == null || promptContentRect == null)
        {
            return;
        }

        if (promptCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            promptCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (promptButton == null)
        {
            promptButton = promptContentRect.GetComponent<Button>();
            if (promptButton == null)
            {
                promptButton = promptContentRect.gameObject.AddComponent<Button>();
            }

            promptButton.transition = Selectable.Transition.None;
        }

        promptButton.onClick.RemoveListener(HandlePromptClicked);
        promptButton.onClick.AddListener(HandlePromptClicked);
    }

    public static bool IsPointerOverActivePrompt(Vector2 screenPosition)
    {
        PlayerInteractionPromptController instance = Instance;
        if (instance == null || instance.promptInstance == null || !instance.promptInstance.activeInHierarchy)
        {
            return false;
        }

        Camera eventCamera = instance.promptCanvas != null
            && instance.promptCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? instance.promptCanvas.worldCamera
            : null;

        Graphic[] graphics = instance.promptInstance.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (graphic != null && graphic.raycastTarget
                && RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, screenPosition, eventCamera))
            {
                return true;
            }
        }

        return false;
    }

    private void HandlePromptClicked()
    {
        if (promptInstance == null || !promptInstance.activeInHierarchy)
        {
            return;
        }

        if (DialogueUIManager.Instance != null && !DialogueUIManager.Instance.CanOpenDialogue)
        {
            return;
        }

        foreach (IInteractionPromptSource source in sources)
        {
            if (source != null && source.IsInteractionPromptEligible)
            {
                source.TriggerInteraction();
                return;
            }
        }
    }

// 处理 RemoveInvalidSources 对应逻辑
    private void RemoveInvalidSources()
    {
// 使用 RemoveInvalidSources 所需功能
        sources.RemoveWhere(source => source == null || (source is MonoBehaviour behaviour && behaviour == null));
    }

// 刷新 UpdatePromptPosition 对应状态
    private void UpdatePromptPosition()
    {
// 同步 UpdatePromptPosition 的内部状态
        promptPositionValid = false;
        if (promptInstance == null)
        {
// 返回 UpdatePromptPosition 的处理结果
            return;
        }

// 同步 UpdatePromptPosition 的内部状态（UpdatePromptPosition）
        gameplayCamera = GetGameplayCamera();
        if (gameplayCamera == null)
        {
// 返回 UpdatePromptPosition 的处理结果（UpdatePromptPosition）
            return;
        }

// 检查 UpdatePromptPosition 的前置条件
        if (promptCanvas != null && promptCanvas.worldCamera != gameplayCamera)
        {
// 同步 UpdatePromptPosition 的内部状态（UpdatePromptPosition）（promptCanvas）
            promptCanvas.worldCamera = gameplayCamera;
        }

// 完成 UpdatePromptPosition 的主要职责
        Vector3 screenPosition = gameplayCamera.WorldToScreenPoint(transform.position + worldOffset);
        if (screenPosition.z < 0f)
        {
// 返回 UpdatePromptPosition 的处理结果（UpdatePromptPosition）（return）
            return;
        }

// 检查 UpdatePromptPosition 的前置条件（UpdatePromptPosition）
        if (promptContentRect != null && promptCanvas != null)
        {
// 保存 canvasRect 引用
            RectTransform canvasRect = promptCanvas.transform as RectTransform;
            Camera eventCamera = promptCanvas.renderMode == RenderMode.ScreenSpaceOverlay
// 推进 UpdatePromptPosition 的当前步骤
                ? null
                : promptCanvas.worldCamera;

// 检查 UpdatePromptPosition 的前置条件（UpdatePromptPosition）（if）
            if (canvasRect != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(
// 推进 UpdatePromptPosition 的当前步骤（UpdatePromptPosition）
                    canvasRect, screenPosition, eventCamera, out Vector2 localPosition))
            {
// 更新局部位置
                promptContentRect.localPosition = localPosition;
                promptPositionValid = true;
            }

// 在 UpdatePromptPosition 中继续当前处理
            return;
        }

// 更新当前位置
        promptInstance.transform.position = screenPosition;
        promptPositionValid = true;
    }

// 刷新 UpdatePromptVisibility 对应状态
    private void UpdatePromptVisibility()
    {
// 缺少必要引用时退出 UpdatePromptVisibility
        if (promptInstance == null)
        {
// 返回 UpdatePromptVisibility 的处理结果
            return;
        }

// 记录 UpdatePromptVisibility 的当前状态
        bool globallyUsable = promptPositionValid
            && !ScreenFader.IsFading
            && !ScreenFader.IsCovering
// 推进 UpdatePromptVisibility 的当前步骤
            && !GameplayInputLock.IsInteractionLocked
            && (DialogueUIManager.Instance == null || DialogueUIManager.Instance.CanOpenDialogue);
// 判断 HasEligibleSource 对应条件
        bool sourceUsable = globallyUsable && HasEligibleSource();
        if (promptButton != null)
        {
            promptButton.interactable = sourceUsable;
        }
        promptInstance.SetActive(sourceUsable);
    }

// 在 UpdatePromptVisibility 中处理 HasEligibleSource
    private bool HasEligibleSource()
    {
// 在 HasEligibleSource 中继续当前处理
        foreach (IInteractionPromptSource source in sources)
        {
// 检查 HasEligibleSource 的前置条件
            if (source != null && source.IsInteractionPromptEligible)
            {
// 返回 HasEligibleSource 的处理结果
                return true;
            }
        }

// 返回 HasEligibleSource 的处理结果（HasEligibleSource）
        return false;
    }

// 获取 GetGameplayCamera 所需引用
    private Camera GetGameplayCamera()
    {
// 检查 GetGameplayCamera 的前置条件
        if (gameplayCamera != null && gameplayCamera.isActiveAndEnabled)
        {
// 返回 GetGameplayCamera 的处理结果
            return gameplayCamera;
        }

// 同步 GetGameplayCamera 的内部状态
        gameplayCamera = Camera.main;
        return gameplayCamera;
    }
}
