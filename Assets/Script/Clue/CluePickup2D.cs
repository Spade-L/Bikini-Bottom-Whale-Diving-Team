using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CluePickup2D : MonoBehaviour, IInteractionPromptSource
{
    // 流程可安全中断
    [Header("出现条件（可留默认 = 一直出现）")]
    [SerializeField] private StoryCondition appearCondition = new StoryCondition();

    [Header("调查内容")]
    [SerializeField] private DialogueData inspectDialogue;
    [SerializeField] private ClueData clueToGrant;

    [Header("行为")]
    [Tooltip("拾取后物品是否从场景消失（false = 可反复调查，但线索只给一次）")]
    [SerializeField] private bool disappearAfterPickup = true;

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

    // 仅表示触发器范围；物件隐藏后 Unity 不再接收后续触发回调
    private bool playerInRange;
    private bool interactionSuppressed;

    // 未配置线索时返回 null，避免生成无意义的 picked_ Flag
    private string PickupFlag => clueToGrant != null ? $"picked_{clueToGrant.ClueId}" : null;

    // 仅在当前调查能够响应时显示集中提示
    public bool IsInteractionPromptEligible
    {
        get
        {
            if (!isActiveAndEnabled || !playerInRange || interactionSuppressed)
            {
                return false;
            }

            bool locked = !string.IsNullOrEmpty(lockedByFlag)
                && GameManager.Instance != null
                && GameManager.Instance.HasFlag(lockedByFlag);

            return !locked || lockedDialogue != null;
        }
    }

    // Awake 先于 Start 执行，确保触发器和提示初始状态在第一帧输入前准备完毕
    private void Awake()
    {
        // RequireComponent 保证组件存在；这里强制触发器模式以支持靠近检测
        GetComponent<BoxCollider2D>().isTrigger = true;
        HidePrompt();
    }

    // Start 时读取 GameManager 的当前存档状态，并在其存在时订阅变化事件
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

    // 必须与 Start 成对退订，防止物件销毁后仍被全局事件回调
    private void OnDisable()
    {
        PlayerInteractionPromptController.UnregisterSource(this);
    }

    private void OnDestroy()
    {
        PlayerInteractionPromptController.UnregisterSource(this);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeAdvanced -= HandleTimeAdvanced;
            GameManager.Instance.OnFlagsChanged -= HandleFlagsChanged;
            GameManager.Instance.OnClueCollected -= HandleClueCollected;
        }
    }

    // 条件可依赖时间、Flag 或线索，任一相关状态变化都重新计算可见性
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

    // 调查次数按物件只计一次；与拾取消失 Flag 分开，支持可重复调查物件
    private string InvestigationFlag => clueToGrant != null ? $"investigated_{clueToGrant.ClueId}" : $"investigated_{name}";

    // 只在玩家位于触发器内且按键刚按下时发起调查
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

        if (!playerInRange || !Input.GetKeyDown(KeyCode.F))
        {
            return;
        }

        // 对话管理器的冷却时间阻止“结束对话”的同一次按键立即重新触发调查
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.CanOpenDialogue)
        {
            Inspect();
        }
    }

    private void Inspect()
    {
        HidePrompt();

        // 封锁检查：达到阈值后此物品不再提供调查，只播封锁台词（不计数）
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

    // 对话完成回调或无对话的直接路径共用此处，保证发放时机一致
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

        if (playerInRange)
        {
            PlayerInteractionPromptController.RefreshSource(this);
        }
    }

    // Tag 可在 Inspector 配置，项目中的玩家对象必须使用相同 Tag
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            PlayerInteractionPromptController.RegisterSource(this);
        }
    }

    // 离开触发区域后清除记录
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
