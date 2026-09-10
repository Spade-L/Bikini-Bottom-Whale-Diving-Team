using System.Collections.Generic;
using UnityEngine;

// 定义 IInteractionPromptSource 类型
public interface IInteractionPromptSource
{
// 更新当前逻辑
    bool IsInteractionPromptEligible { get; }
}

public class PlayerInteractionPromptController : MonoBehaviour
{
// 保存 promptPrefab 引用
    [SerializeField] private GameObject promptPrefab;
    [SerializeField] private Vector3 worldOffset = new Vector3(0.75f, 0.8f, 0f);
// 配置 promptScale 数值
    [SerializeField] private float promptScale = 1f;

// 保存 sources 数据
    private readonly HashSet<IInteractionPromptSource> sources = new HashSet<IInteractionPromptSource>();
    private GameObject promptInstance;
// 保存 promptCanvas 引用
    private Canvas promptCanvas;
    private RectTransform promptContentRect;
// 保存 gameplayCamera 引用
    private Camera gameplayCamera;
    private bool promptPositionValid;

// 更新当前逻辑
    public static PlayerInteractionPromptController Instance { get; private set; }

// 定义 Awake 方法
    private void Awake()
    {
// 更新当前状态
        Instance = this;
        EnsurePromptInstance();
    }

// 定义 OnDestroy 方法
    private void OnDestroy()
    {
// 判断当前条件
        if (Instance == this)
        {
// 更新当前状态
            Instance = null;
        }
    }

// 定义 LateUpdate 方法
    private void LateUpdate()
    {
// 执行 EnsurePromptInstance
        EnsurePromptInstance();
        RemoveInvalidSources();
// 执行 UpdatePromptPosition
        UpdatePromptPosition();
        UpdatePromptVisibility();
    }

// 定义 RegisterSource 方法
    public static void RegisterSource(IInteractionPromptSource source)
    {
// 空引用时直接退出
        if (source == null || Instance == null)
        {
// 返回当前结果
            return;
        }

// 调用 Add
        Instance.sources.Add(source);
        Instance.UpdatePromptVisibility();
    }

// 定义 UnregisterSource 方法
    public static void UnregisterSource(IInteractionPromptSource source)
    {
// 空引用时直接退出
        if (source == null || Instance == null)
        {
// 返回当前结果
            return;
        }

// 调用 Remove
        Instance.sources.Remove(source);
        Instance.UpdatePromptVisibility();
    }

// 定义 RefreshSource 方法
    public static void RefreshSource(IInteractionPromptSource source)
    {
// 空引用时直接退出
        if (Instance == null)
        {
// 返回当前结果
            return;
        }

// 调用 UpdatePromptVisibility
        Instance.UpdatePromptVisibility();
    }

// 定义 EnsurePromptInstance 方法
    private void EnsurePromptInstance()
    {
// 空引用时直接退出
        if (promptInstance != null || promptPrefab == null)
        {
// 返回当前结果
            return;
        }

// 更新当前状态
        promptInstance = Instantiate(promptPrefab);
        promptInstance.name = $"{promptPrefab.name} (Player Prompt)";
// 切换显示状态
        promptInstance.SetActive(false);
        promptInstance.transform.localScale = Vector3.one * Mathf.Max(0.01f, promptScale);
// 更新当前状态
        promptCanvas = promptInstance.GetComponentInChildren<Canvas>(true);
        promptContentRect = promptCanvas != null
// 调用 Find
            ? promptCanvas.transform.Find("PromptContent") as RectTransform
            : null;

// 空引用时直接退出
        if (promptContentRect == null && promptCanvas != null)
        {
// 定义 typeof 方法
            GameObject contentObject = new GameObject("PromptContent", typeof(RectTransform));
            promptContentRect = contentObject.transform as RectTransform;
// 调用 SetParent
            promptContentRect.SetParent(promptCanvas.transform, false);
            promptContentRect.anchorMin = new Vector2(0.5f, 0.5f);
// 更新当前状态
            promptContentRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptContentRect.pivot = new Vector2(0.5f, 0.5f);
// 更新当前状态
            promptContentRect.sizeDelta = Vector2.zero;

// 保存 originalChildren 引用
            List<Transform> originalChildren = new List<Transform>();
            for (int i = 0; i < promptCanvas.transform.childCount; i++)
            {
// 定义 GetChild 方法
                Transform child = promptCanvas.transform.GetChild(i);
                if (child != promptContentRect)
                {
// 调用 Add
                    originalChildren.Add(child);
                }
            }

// 遍历全部元素
            foreach (Transform child in originalChildren)
            {
// 调用 SetParent
                child.SetParent(promptContentRect, true);
            }
        }

// 判断当前条件
        if (promptCanvas != null)
        {
// 更新当前状态
            promptCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            promptCanvas.worldCamera = GetGameplayCamera();
        }
    }

// 定义 RemoveInvalidSources 方法
    private void RemoveInvalidSources()
    {
// 调用 RemoveWhere
        sources.RemoveWhere(source => source == null || (source is MonoBehaviour behaviour && behaviour == null));
    }

// 定义 UpdatePromptPosition 方法
    private void UpdatePromptPosition()
    {
// 更新当前状态
        promptPositionValid = false;
        if (promptInstance == null)
        {
// 返回当前结果
            return;
        }

// 更新当前状态
        gameplayCamera = GetGameplayCamera();
        if (gameplayCamera == null)
        {
// 返回当前结果
            return;
        }

// 判断当前条件
        if (promptCanvas != null && promptCanvas.worldCamera != gameplayCamera)
        {
// 更新当前状态
            promptCanvas.worldCamera = gameplayCamera;
        }

// 定义 WorldToScreenPoint 方法
        Vector3 screenPosition = gameplayCamera.WorldToScreenPoint(transform.position + worldOffset);
        if (screenPosition.z < 0f)
        {
// 返回当前结果
            return;
        }

// 判断当前条件
        if (promptContentRect != null && promptCanvas != null)
        {
// 保存 canvasRect 引用
            RectTransform canvasRect = promptCanvas.transform as RectTransform;
            Camera eventCamera = promptCanvas.renderMode == RenderMode.ScreenSpaceOverlay
// 更新当前逻辑
                ? null
                : promptCanvas.worldCamera;

// 判断当前条件
            if (canvasRect != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(
// 更新当前逻辑
                    canvasRect, screenPosition, eventCamera, out Vector2 localPosition))
            {
// 更新局部位置
                promptContentRect.localPosition = localPosition;
                promptPositionValid = true;
            }

// 返回当前结果
            return;
        }

// 更新当前位置
        promptInstance.transform.position = screenPosition;
        promptPositionValid = true;
    }

// 定义 UpdatePromptVisibility 方法
    private void UpdatePromptVisibility()
    {
// 空引用时直接退出
        if (promptInstance == null)
        {
// 返回当前结果
            return;
        }

// 记录 globallyUsable 状态
        bool globallyUsable = promptPositionValid
            && !ScreenFader.IsFading
// 更新当前逻辑
            && !GameplayInputLock.IsInteractionLocked
            && (DialogueUIManager.Instance == null || DialogueUIManager.Instance.CanOpenDialogue);
// 定义 HasEligibleSource 方法
        bool sourceUsable = globallyUsable && HasEligibleSource();
        promptInstance.SetActive(sourceUsable);
    }

// 定义 HasEligibleSource 方法
    private bool HasEligibleSource()
    {
// 遍历全部元素
        foreach (IInteractionPromptSource source in sources)
        {
// 判断当前条件
            if (source != null && source.IsInteractionPromptEligible)
            {
// 返回当前结果
                return true;
            }
        }

// 返回当前结果
        return false;
    }

// 定义 GetGameplayCamera 方法
    private Camera GetGameplayCamera()
    {
// 判断当前条件
        if (gameplayCamera != null && gameplayCamera.isActiveAndEnabled)
        {
// 返回当前结果
            return gameplayCamera;
        }

// 更新当前状态
        gameplayCamera = Camera.main;
        return gameplayCamera;
    }
}
