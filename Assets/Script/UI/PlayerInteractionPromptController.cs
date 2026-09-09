using System.Collections.Generic;
using UnityEngine;

public interface IInteractionPromptSource
{
    bool IsInteractionPromptEligible { get; }
}

public class PlayerInteractionPromptController : MonoBehaviour
{
    [SerializeField] private GameObject promptPrefab;
    [SerializeField] private Vector3 worldOffset = new Vector3(0.75f, 0.8f, 0f);
    [SerializeField] private float promptScale = 1f;

    private readonly HashSet<IInteractionPromptSource> sources = new HashSet<IInteractionPromptSource>();
    private GameObject promptInstance;
    private Canvas promptCanvas;
    private RectTransform promptContentRect;
    private Camera gameplayCamera;
    private bool promptPositionValid;

    public static PlayerInteractionPromptController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        EnsurePromptInstance();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void LateUpdate()
    {
        EnsurePromptInstance();
        RemoveInvalidSources();
        UpdatePromptPosition();
        UpdatePromptVisibility();
    }

    public static void RegisterSource(IInteractionPromptSource source)
    {
        if (source == null || Instance == null)
        {
            return;
        }

        Instance.sources.Add(source);
        Instance.UpdatePromptVisibility();
    }

    public static void UnregisterSource(IInteractionPromptSource source)
    {
        if (source == null || Instance == null)
        {
            return;
        }

        Instance.sources.Remove(source);
        Instance.UpdatePromptVisibility();
    }

    public static void RefreshSource(IInteractionPromptSource source)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.UpdatePromptVisibility();
    }

    private void EnsurePromptInstance()
    {
        if (promptInstance != null || promptPrefab == null)
        {
            return;
        }

        promptInstance = Instantiate(promptPrefab);
        promptInstance.name = $"{promptPrefab.name} (Player Prompt)";
        promptInstance.SetActive(false);
        promptInstance.transform.localScale = Vector3.one * Mathf.Max(0.01f, promptScale);
        promptCanvas = promptInstance.GetComponentInChildren<Canvas>(true);
        promptContentRect = promptCanvas != null
            ? promptCanvas.transform.Find("PromptContent") as RectTransform
            : null;

        if (promptContentRect == null && promptCanvas != null)
        {
            GameObject contentObject = new GameObject("PromptContent", typeof(RectTransform));
            promptContentRect = contentObject.transform as RectTransform;
            promptContentRect.SetParent(promptCanvas.transform, false);
            promptContentRect.anchorMin = new Vector2(0.5f, 0.5f);
            promptContentRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptContentRect.pivot = new Vector2(0.5f, 0.5f);
            promptContentRect.sizeDelta = Vector2.zero;

            List<Transform> originalChildren = new List<Transform>();
            for (int i = 0; i < promptCanvas.transform.childCount; i++)
            {
                Transform child = promptCanvas.transform.GetChild(i);
                if (child != promptContentRect)
                {
                    originalChildren.Add(child);
                }
            }

            foreach (Transform child in originalChildren)
            {
                child.SetParent(promptContentRect, true);
            }
        }

        if (promptCanvas != null)
        {
            promptCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            promptCanvas.worldCamera = GetGameplayCamera();
        }
    }

    private void RemoveInvalidSources()
    {
        sources.RemoveWhere(source => source == null || (source is MonoBehaviour behaviour && behaviour == null));
    }

    private void UpdatePromptPosition()
    {
        promptPositionValid = false;
        if (promptInstance == null)
        {
            return;
        }

        gameplayCamera = GetGameplayCamera();
        if (gameplayCamera == null)
        {
            return;
        }

        if (promptCanvas != null && promptCanvas.worldCamera != gameplayCamera)
        {
            promptCanvas.worldCamera = gameplayCamera;
        }

        Vector3 screenPosition = gameplayCamera.WorldToScreenPoint(transform.position + worldOffset);
        if (screenPosition.z < 0f)
        {
            return;
        }

        if (promptContentRect != null && promptCanvas != null)
        {
            RectTransform canvasRect = promptCanvas.transform as RectTransform;
            Camera eventCamera = promptCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : promptCanvas.worldCamera;

            if (canvasRect != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPosition, eventCamera, out Vector2 localPosition))
            {
                promptContentRect.localPosition = localPosition;
                promptPositionValid = true;
            }

            return;
        }

        promptInstance.transform.position = screenPosition;
        promptPositionValid = true;
    }

    private void UpdatePromptVisibility()
    {
        if (promptInstance == null)
        {
            return;
        }

        bool globallyUsable = promptPositionValid
            && !GameplayInputLock.IsInteractionLocked
            && (DialogueUIManager.Instance == null || DialogueUIManager.Instance.CanOpenDialogue);
        bool sourceUsable = globallyUsable && HasEligibleSource();
        promptInstance.SetActive(sourceUsable);
    }

    private bool HasEligibleSource()
    {
        foreach (IInteractionPromptSource source in sources)
        {
            if (source != null && source.IsInteractionPromptEligible)
            {
                return true;
            }
        }

        return false;
    }

    private Camera GetGameplayCamera()
    {
        if (gameplayCamera != null && gameplayCamera.isActiveAndEnabled)
        {
            return gameplayCamera;
        }

        gameplayCamera = Camera.main;
        return gameplayCamera;
    }
}
