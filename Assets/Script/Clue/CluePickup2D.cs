using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CluePickup2D : MonoBehaviour, IInteractionPromptSource
{
    [Header("出现条件（可留默认 = 一直出现）")]
    [SerializeField] private StoryCondition appearCondition = new StoryCondition();

    [Header("调查内容")]
    [SerializeField] private DialogueData inspectDialogue;
    [SerializeField] private ClueData clueToGrant;

    [Header("行为")]
    [Tooltip("拾取后物品是否从场景消失（false = 可反复调查，但线索只给一次）")]
    [SerializeField] private bool disappearAfterPickup = true;

    [Tooltip("开启后该物品完成一次调查后不再响应交互，但物品仍保留在场景中")]
    [SerializeField] private bool onlyInteractOnce;
    [Tooltip("一次性交互的唯一 ID；留空时使用物体名")]
    [SerializeField] private string interactionId;

    [Tooltip("调查此物品是否计入调查次数（默认计入；线索本身不额外计数）")]
    [SerializeField] private bool countsAsInvestigation = true;

    [Header("封锁（调查次数达阈值后不可再调查）")]
    [Tooltip("此 Flag 被设置后禁止调查，按 F 改为播放封锁台词（如 lock_home_items）")]
    [SerializeField] private string lockedByFlag;
    [Tooltip("封锁后的台词，如“这地方我翻遍了……没有更多线索了。”")]
    [SerializeField] private DialogueData lockedDialogue;

    [Header("交互 UI")]
    [SerializeField] private GameObject interactionUI;
    [SerializeField] private string playerTag = "Player";

    private static GameObject exclusiveInteractionTarget;
    private bool playerInRange;
    private bool interactionSuppressed;

    private string PickupFlag => clueToGrant != null ? $"picked_{clueToGrant.ClueId}" : null;
    private string InteractionFlag
    {
        get
        {
            string id = string.IsNullOrEmpty(interactionId) ? name : interactionId;
            return $"interacted_{id}";
        }
    }

    private bool HasCompletedInteraction => onlyInteractOnce
        && GameManager.Instance != null
        && GameManager.Instance.HasFlag(InteractionFlag);

    public static void SetExclusiveInteractionTarget(GameObject target)
    {
        exclusiveInteractionTarget = target;
        PlayerInteractionPromptController.RefreshSource(null);
    }

    public static void ClearExclusiveInteractionTarget(GameObject target)
    {
        if (exclusiveInteractionTarget == target)
        {
            exclusiveInteractionTarget = null;
            PlayerInteractionPromptController.RefreshSource(null);
        }
    }

    private bool IsExclusiveTarget => exclusiveInteractionTarget == null || exclusiveInteractionTarget == gameObject;

    public bool IsInteractionPromptEligible
    {
        get
        {
            if (!isActiveAndEnabled || !playerInRange || interactionSuppressed
                || HasCompletedInteraction
                || (exclusiveInteractionTarget != null && exclusiveInteractionTarget != gameObject))
            {
                return false;
            }

            bool locked = !string.IsNullOrEmpty(lockedByFlag)
                && GameManager.Instance != null
                && GameManager.Instance.HasFlag(lockedByFlag);

            return !locked || lockedDialogue != null;
        }
    }

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        HidePrompt();
    }

    private void Start()
    {
        RefreshVisibility();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeAdvanced += HandleTimeAdvanced;
            GameManager.Instance.OnFlagsChanged += HandleFlagsChanged;
            GameManager.Instance.OnClueCollected += HandleClueCollected;
        }
    }

    private void OnDisable()
    {
        PlayerInteractionPromptController.UnregisterSource(this);
        ClearExclusiveInteractionTarget(gameObject);
    }

    private void OnDestroy()
    {
        PlayerInteractionPromptController.UnregisterSource(this);
        ClearExclusiveInteractionTarget(gameObject);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeAdvanced -= HandleTimeAdvanced;
            GameManager.Instance.OnFlagsChanged -= HandleFlagsChanged;
            GameManager.Instance.OnClueCollected -= HandleClueCollected;
        }
    }

    private void HandleTimeAdvanced(int _) => RefreshVisibility();
    private void HandleFlagsChanged() => RefreshVisibility();
    private void HandleClueCollected(ClueData _) => RefreshVisibility();

    private void RefreshVisibility()
    {
        bool alreadyPicked = disappearAfterPickup
            && PickupFlag != null
            && GameManager.Instance != null
            && GameManager.Instance.HasFlag(PickupFlag);

        gameObject.SetActive(!alreadyPicked && appearCondition.IsMet());
    }

    private bool IsExclusiveInteractionAllowed()
    {
        return IsExclusiveTarget;
    }

    private string InvestigationFlag => clueToGrant != null ? $"investigated_{clueToGrant.ClueId}" : $"investigated_{name}";

    private void Update()
    {
        if (GameplayInputLock.IsInteractionLocked)
        {
            interactionSuppressed = true;
            HidePrompt();
            return;
        }

        if (interactionSuppressed)
        {
            interactionSuppressed = false;
            PlayerInteractionPromptController.RefreshSource(this);
        }

        if (!IsExclusiveInteractionAllowed() || HasCompletedInteraction)
        {
            HidePrompt();
            return;
        }

        if (!playerInRange || !Input.GetKeyDown(KeyCode.F))
        {
            return;
        }

        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.CanOpenDialogue)
        {
            Inspect();
        }
    }

    private void Inspect()
    {
        if (HasCompletedInteraction)
        {
            return;
        }

        HidePrompt();

        bool locked = !string.IsNullOrEmpty(lockedByFlag)
            && GameManager.Instance != null
            && GameManager.Instance.HasFlag(lockedByFlag);

        if (locked)
        {
            if (lockedDialogue != null)
            {
                DialogueUIManager.Instance.StartDialogue(lockedDialogue, () =>
                {
                    if (playerInRange)
                    {
                        PlayerInteractionPromptController.RefreshSource(this);
                    }
                });
            }
            return;
        }

        if (countsAsInvestigation && GameManager.Instance != null
            && !GameManager.Instance.HasFlag(InvestigationFlag))
        {
            GameManager.Instance.AddInvestigation();
            GameManager.Instance.SetFlag(InvestigationFlag);
        }

        if (inspectDialogue != null)
        {
            DialogueUIManager.Instance.StartDialogue(inspectDialogue, OnInspectFinished);
        }
        else
        {
            OnInspectFinished();
        }
    }

    private void OnInspectFinished()
    {
        if (clueToGrant != null && GameManager.Instance != null)
        {
            GameManager.Instance.CollectClue(clueToGrant);

            if (disappearAfterPickup)
            {
                GameManager.Instance.SetFlag(PickupFlag);
                gameObject.SetActive(false);
                return;
            }
        }

        if (onlyInteractOnce && GameManager.Instance != null)
        {
            GameManager.Instance.SetFlag(InteractionFlag);
        }

        if (playerInRange)
        {
            PlayerInteractionPromptController.RefreshSource(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            PlayerInteractionPromptController.RegisterSource(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            PlayerInteractionPromptController.UnregisterSource(this);
        }
    }

    private void ShowPrompt()
    {
        PlayerInteractionPromptController.RefreshSource(this);
    }

    private void HidePrompt()
    {
        PlayerInteractionPromptController.RefreshSource(this);
    }
}
