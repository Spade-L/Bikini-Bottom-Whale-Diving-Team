using UnityEngine;

// 调用 RequireComponent
[RequireComponent(typeof(BoxCollider2D))]
public class CluePickup2D : MonoBehaviour, IInteractionPromptSource
{
// 配置 出现条件（可留默认 = 一直出现） 分组
    [Header("出现条件（可留默认 = 一直出现）")]
    [SerializeField] private StoryCondition appearCondition = new StoryCondition();

// 配置 调查内容 分组
    [Header("调查内容")]
    [SerializeField] private DialogueData inspectDialogue;
// 保存 clueToGrant 引用
    [SerializeField] private ClueData clueToGrant;

// 配置 行为 分组
    [Header("行为")]
    [Tooltip("拾取后物品是否从场景消失（false = 可反复调查，但线索只给一次）")]
// 记录 disappearAfterPickup 状态
    [SerializeField] private bool disappearAfterPickup = true;

// 说明当前配置
    [Tooltip("开启后该物品完成一次调查后不再响应交互，但物品仍保留在场景中")]
    [SerializeField] private bool onlyInteractOnce;
// 说明当前配置
    [Tooltip("一次性交互的唯一 ID；留空时使用物体名")]
    [SerializeField] private string interactionId;

// 说明当前配置
    [Tooltip("调查此物品是否计入调查次数（默认计入；线索本身不额外计数）")]
    [SerializeField] private bool countsAsInvestigation = true;

// 配置 封锁（调查次数达阈值后不可再调查） 分组
    [Header("封锁（调查次数达阈值后不可再调查）")]
    [Tooltip("此 Flag 被设置后禁止调查，按 F 改为播放封锁台词（如 lock_home_items）")]
// 保存 lockedByFlag 数据
    [SerializeField] private string lockedByFlag;
    [Tooltip("封锁后的台词，如“这地方我翻遍了……没有更多线索了。”")]
// 保存 lockedDialogue 引用
    [SerializeField] private DialogueData lockedDialogue;

// 配置 交互 UI 分组
    [Header("交互 UI")]
    [SerializeField] private GameObject interactionUI;
// 保存 playerTag 数据
    [SerializeField] private string playerTag = "Player";

// 保存 exclusiveInteractionTarget 引用
    private static GameObject exclusiveInteractionTarget;
    private bool playerInRange;
// 记录 interactionSuppressed 状态
    private bool interactionSuppressed;

// 保存 PickupFlag 数据
    private string PickupFlag => clueToGrant != null ? $"picked_{clueToGrant.ClueId}" : null;
    private string InteractionFlag
    {
// 更新当前逻辑
        get
        {
// 定义 IsNullOrEmpty 方法
            string id = string.IsNullOrEmpty(interactionId) ? name : interactionId;
            return $"interacted_{id}";
        }
    }

// 记录 HasCompletedInteraction 状态
    private bool HasCompletedInteraction => onlyInteractOnce
        && GameManager.Instance != null
// 调用 HasFlag
        && GameManager.Instance.HasFlag(InteractionFlag);

// 定义 SetExclusiveInteractionTarget 方法
    public static void SetExclusiveInteractionTarget(GameObject target)
    {
// 更新当前状态
        exclusiveInteractionTarget = target;
        PlayerInteractionPromptController.RefreshSource(null);
    }

// 定义 ClearExclusiveInteractionTarget 方法
    public static void ClearExclusiveInteractionTarget(GameObject target)
    {
// 判断当前条件
        if (exclusiveInteractionTarget == target)
        {
// 更新当前状态
            exclusiveInteractionTarget = null;
            PlayerInteractionPromptController.RefreshSource(null);
        }
    }

// 记录 IsExclusiveTarget 状态
    private bool IsExclusiveTarget => exclusiveInteractionTarget == null || exclusiveInteractionTarget == gameObject;

// 更新当前逻辑
    public bool IsInteractionPromptEligible
    {
// 更新当前逻辑
        get
        {
// 判断当前条件
            if (!isActiveAndEnabled || !playerInRange || interactionSuppressed
                || HasCompletedInteraction
// 更新当前逻辑
                || (exclusiveInteractionTarget != null && exclusiveInteractionTarget != gameObject))
            {
// 返回当前结果
                return false;
            }

// 定义 IsNullOrEmpty 方法
            bool locked = !string.IsNullOrEmpty(lockedByFlag)
                && GameManager.Instance != null
// 调用 HasFlag
                && GameManager.Instance.HasFlag(lockedByFlag);

// 返回当前结果
            return !locked || lockedDialogue != null;
        }
    }

// 定义 Awake 方法
    private void Awake()
    {
// 获取组件引用
        GetComponent<BoxCollider2D>().isTrigger = true;
        HidePrompt();
    }

// 定义 Start 方法
    private void Start()
    {
// 执行 RefreshVisibility
        RefreshVisibility();

// 判断当前条件
        if (GameManager.Instance != null)
        {
// 更新当前逻辑
            GameManager.Instance.OnTimeAdvanced += HandleTimeAdvanced;
            GameManager.Instance.OnFlagsChanged += HandleFlagsChanged;
// 更新当前逻辑
            GameManager.Instance.OnClueCollected += HandleClueCollected;
        }
    }

// 定义 OnDisable 方法
    private void OnDisable()
    {
// 调用 UnregisterSource
        PlayerInteractionPromptController.UnregisterSource(this);
        ClearExclusiveInteractionTarget(gameObject);
    }

// 定义 OnDestroy 方法
    private void OnDestroy()
    {
// 调用 UnregisterSource
        PlayerInteractionPromptController.UnregisterSource(this);
        ClearExclusiveInteractionTarget(gameObject);

// 判断当前条件
        if (GameManager.Instance != null)
        {
// 更新当前逻辑
            GameManager.Instance.OnTimeAdvanced -= HandleTimeAdvanced;
            GameManager.Instance.OnFlagsChanged -= HandleFlagsChanged;
// 更新当前逻辑
            GameManager.Instance.OnClueCollected -= HandleClueCollected;
        }
    }

// 定义 RefreshVisibility 方法
    private void HandleTimeAdvanced(int _) => RefreshVisibility();
    private void HandleFlagsChanged() => RefreshVisibility();
// 定义 RefreshVisibility 方法
    private void HandleClueCollected(ClueData _) => RefreshVisibility();

// 定义 RefreshVisibility 方法
    private void RefreshVisibility()
    {
// 记录 alreadyPicked 状态
        bool alreadyPicked = disappearAfterPickup
            && PickupFlag != null
// 更新当前逻辑
            && GameManager.Instance != null
            && GameManager.Instance.HasFlag(PickupFlag);

// 定义 IsMet 方法
        gameObject.SetActive(!alreadyPicked && appearCondition.IsMet());
    }

// 定义 IsExclusiveInteractionAllowed 方法
    private bool IsExclusiveInteractionAllowed()
    {
// 返回当前结果
        return IsExclusiveTarget;
    }

// 保存 InvestigationFlag 数据
    private string InvestigationFlag => clueToGrant != null ? $"investigated_{clueToGrant.ClueId}" : $"investigated_{name}";

// 定义 Update 方法
    private void Update()
    {
// 判断当前条件
        if (GameplayInputLock.IsInteractionLocked)
        {
// 更新当前状态
            interactionSuppressed = true;
            HidePrompt();
// 返回当前结果
            return;
        }

// 判断当前条件
        if (interactionSuppressed)
        {
// 更新当前状态
            interactionSuppressed = false;
            PlayerInteractionPromptController.RefreshSource(this);
        }

// 判断当前条件
        if (!IsExclusiveInteractionAllowed() || HasCompletedInteraction)
        {
// 执行 HidePrompt
            HidePrompt();
            return;
        }

// 检测按键输入
        if (!playerInRange || !Input.GetKeyDown(KeyCode.F))
        {
// 返回当前结果
            return;
        }

// 检查对话状态
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.CanOpenDialogue)
        {
// 执行 Inspect
            Inspect();
        }
    }

// 定义 Inspect 方法
    private void Inspect()
    {
// 判断当前条件
        if (HasCompletedInteraction)
        {
// 返回当前结果
            return;
        }

// 执行 HidePrompt
        HidePrompt();

// 定义 IsNullOrEmpty 方法
        bool locked = !string.IsNullOrEmpty(lockedByFlag)
            && GameManager.Instance != null
// 调用 HasFlag
            && GameManager.Instance.HasFlag(lockedByFlag);

// 判断当前条件
        if (locked)
        {
// 判断当前条件
            if (lockedDialogue != null)
            {
// 开始当前对话
                DialogueUIManager.Instance.StartDialogue(lockedDialogue, () =>
                {
// 判断当前条件
                    if (playerInRange)
                    {
// 调用 RefreshSource
                        PlayerInteractionPromptController.RefreshSource(this);
                    }
// 更新当前逻辑
                });
            }
// 返回当前结果
            return;
        }

// 判断当前条件
        if (countsAsInvestigation && GameManager.Instance != null
            && !GameManager.Instance.HasFlag(InvestigationFlag))
        {
// 累加调查次数
            GameManager.Instance.AddInvestigation();
            GameManager.Instance.SetFlag(InvestigationFlag);
        }

// 判断当前条件
        if (inspectDialogue != null)
        {
// 开始当前对话
            DialogueUIManager.Instance.StartDialogue(inspectDialogue, OnInspectFinished);
        }
// 处理其他分支
        else
        {
// 执行 OnInspectFinished
            OnInspectFinished();
        }
    }

// 定义 OnInspectFinished 方法
    private void OnInspectFinished()
    {
// 判断当前条件
        if (clueToGrant != null && GameManager.Instance != null)
        {
// 记录当前线索
            GameManager.Instance.CollectClue(clueToGrant);

// 判断当前条件
            if (disappearAfterPickup)
            {
// 更新剧情标记
                GameManager.Instance.SetFlag(PickupFlag);
                gameObject.SetActive(false);
// 返回当前结果
                return;
            }
        }

// 判断当前条件
        if (onlyInteractOnce && GameManager.Instance != null)
        {
// 更新剧情标记
            GameManager.Instance.SetFlag(InteractionFlag);
        }

// 判断当前条件
        if (playerInRange)
        {
// 调用 RefreshSource
            PlayerInteractionPromptController.RefreshSource(this);
        }
    }

// 定义 OnTriggerEnter2D 方法
    private void OnTriggerEnter2D(Collider2D other)
    {
// 判断当前条件
        if (other.CompareTag(playerTag))
        {
// 更新当前状态
            playerInRange = true;
            PlayerInteractionPromptController.RegisterSource(this);
        }
    }

// 定义 OnTriggerExit2D 方法
    private void OnTriggerExit2D(Collider2D other)
    {
// 判断当前条件
        if (other.CompareTag(playerTag))
        {
// 更新当前状态
            playerInRange = false;
            PlayerInteractionPromptController.UnregisterSource(this);
        }
    }

// 定义 ShowPrompt 方法
    private void ShowPrompt()
    {
// 调用 RefreshSource
        PlayerInteractionPromptController.RefreshSource(this);
    }

// 定义 HidePrompt 方法
    private void HidePrompt()
    {
// 调用 RefreshSource
        PlayerInteractionPromptController.RefreshSource(this);
    }
}
