using UnityEngine;

// 使用 当前脚本 所需功能
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
// 记录 CluePickup2D 的当前状态
    [SerializeField] private bool disappearAfterPickup = true;

// 说明当前配置
    [Tooltip("开启后该物品完成一次调查后不再响应交互，但物品仍保留在场景中")]
    [SerializeField] private bool onlyInteractOnce;
// 在 CluePickup2D 中继续当前处理
    [Tooltip("一次性交互的唯一 ID；留空时使用物体名")]
    [SerializeField] private string interactionId;

// 在 CluePickup2D 中继续当前处理（CluePickup2D 后续步骤）
    [Tooltip("调查此物品是否计入调查次数（默认计入；线索本身不额外计数）")]
    [SerializeField] private bool countsAsInvestigation = true;

// 配置 封锁（调查次数达阈值后不可再调查） 分组
    [Header("封锁（调查次数达阈值后不可再调查）")]
    [Tooltip("此 Flag 被设置后禁止调查，按 F 改为播放封锁台词（如 lock_home_items）")]
// 同步 CluePickup2D 的相关数据
    [SerializeField] private string lockedByFlag;
    [Tooltip("封锁后的台词，如“这地方我翻遍了……没有更多线索了。”")]
// 保存 lockedDialogue 引用
    [SerializeField] private DialogueData lockedDialogue;

// 配置 交互 UI 分组
    [Header("交互 UI")]
    [SerializeField] private GameObject interactionUI;
// 同步 CluePickup2D 的相关数据（CluePickup2D 后续步骤）
    [SerializeField] private string playerTag = "Player";

// 保存 exclusiveInteractionTarget 引用
    private static GameObject exclusiveInteractionTarget;
    private bool playerInRange;
// 记录 CluePickup2D 的当前状态（CluePickup2D 后续步骤）
    private bool interactionSuppressed;

// 同步 CluePickup2D 的相关数据（CluePickup2D 后续步骤）（54）
    private string PickupFlag => clueToGrant != null ? $"picked_{clueToGrant.ClueId}" : null;
    private string InteractionFlag
    {
// 推进 CluePickup2D 的当前步骤
        get
        {
// 判断 IsNullOrEmpty 对应条件
            string id = string.IsNullOrEmpty(interactionId) ? name : interactionId;
            return $"interacted_{id}";
        }
    }

// 记录 CluePickup2D 的当前状态（CluePickup2D 后续步骤）（67）
    private bool HasCompletedInteraction => onlyInteractOnce
        && GameManager.Instance != null
// 使用 CluePickup2D 所需功能
        && GameManager.Instance.HasFlag(InteractionFlag);

// 设置 SetExclusiveInteractionTarget 的目标状态
    public static void SetExclusiveInteractionTarget(GameObject target)
    {
// 同步 SetExclusiveInteractionTarget 的状态
        exclusiveInteractionTarget = target;
        PlayerInteractionPromptController.RefreshSource(null);
    }

// 处理 ClearExclusiveInteractionTarget 对应逻辑
    public static void ClearExclusiveInteractionTarget(GameObject target)
    {
// 检查 ClearExclusiveInteractionTarget 的前置条件
        if (exclusiveInteractionTarget == target)
        {
// 同步 ClearExclusiveInteractionTarget 的内部状态
            exclusiveInteractionTarget = null;
            PlayerInteractionPromptController.RefreshSource(null);
        }
    }

// 记录 ClearExclusiveInteractionTarget 的当前状态
    private bool IsExclusiveTarget => exclusiveInteractionTarget == null || exclusiveInteractionTarget == gameObject;

// 推进 ClearExclusiveInteractionTarget 的当前步骤
    public bool IsInteractionPromptEligible
    {
// 推进 ClearExclusiveInteractionTarget 的当前步骤（ClearExclusiveInteractionTarget）
        get
        {
// 在 ClearExclusiveInteractionTarget 中处理 检查 ClearExclusiveInteractionTarget 的前置条件
            if (!isActiveAndEnabled || !playerInRange || interactionSuppressed
                || HasCompletedInteraction
// 推进 ClearExclusiveInteractionTarget 的当前步骤（ClearExclusiveInteractionTarget）（exclusiveInteractionTarget）
                || (exclusiveInteractionTarget != null && exclusiveInteractionTarget != gameObject))
            {
// 返回 ClearExclusiveInteractionTarget 的处理结果
                return false;
            }

// 判断 IsNullOrEmpty 对应条件（ClearExclusiveInteractionTarget）
            bool locked = !string.IsNullOrEmpty(lockedByFlag)
                && GameManager.Instance != null
// 使用 ClearExclusiveInteractionTarget 所需功能
                && GameManager.Instance.HasFlag(lockedByFlag);

// 在 ClearExclusiveInteractionTarget 中处理 返回 ClearExclusiveInteractionTarget 的处理结果
            return !locked || lockedDialogue != null;
        }
    }

// 初始化组件引用和运行状态
    private void Awake()
    {
// 获取 Awake 的组件引用
        GetComponent<BoxCollider2D>().isTrigger = true;
        HidePrompt();
    }

// 读取初始依赖并同步首帧状态
    private void Start()
    {
// 推进 Start 中的必要步骤
        RefreshVisibility();

// 检查 Start 的前置条件
        if (GameManager.Instance != null)
        {
// 推进 Start 的当前步骤
            GameManager.Instance.OnTimeAdvanced += HandleTimeAdvanced;
            GameManager.Instance.OnFlagsChanged += HandleFlagsChanged;
// 推进 Start 的当前步骤（Start）
            GameManager.Instance.OnClueCollected += HandleClueCollected;
        }
    }

// 禁用时取消订阅并清理临时状态
    private void OnDisable()
    {
// 使用 OnDisable 所需功能
        PlayerInteractionPromptController.UnregisterSource(this);
        ClearExclusiveInteractionTarget(gameObject);
    }

// 销毁时释放事件订阅和静态引用
    private void OnDestroy()
    {
// 使用 OnDestroy 所需功能
        PlayerInteractionPromptController.UnregisterSource(this);
        ClearExclusiveInteractionTarget(gameObject);

// 检查 OnDestroy 的前置条件
        if (GameManager.Instance != null)
        {
// 推进 OnDestroy 的当前步骤
            GameManager.Instance.OnTimeAdvanced -= HandleTimeAdvanced;
            GameManager.Instance.OnFlagsChanged -= HandleFlagsChanged;
// 推进 OnDestroy 的当前步骤（OnDestroy）
            GameManager.Instance.OnClueCollected -= HandleClueCollected;
        }
    }

// 响应 HandleTimeAdvanced 状态变化
    private void HandleTimeAdvanced(int _) => RefreshVisibility();
    private void HandleFlagsChanged() => RefreshVisibility();
// 响应 HandleClueCollected 状态变化
    private void HandleClueCollected(ClueData _) => RefreshVisibility();

// 刷新 RefreshVisibility 对应状态
    private void RefreshVisibility()
    {
// 记录 RefreshVisibility 的当前状态
        bool alreadyPicked = disappearAfterPickup
            && PickupFlag != null
// 推进 RefreshVisibility 的当前步骤
            && GameManager.Instance != null
            && GameManager.Instance.HasFlag(PickupFlag);

// 判断 IsMet 对应条件
        gameObject.SetActive(!alreadyPicked && appearCondition.IsMet());
    }

// 判断 IsExclusiveInteractionAllowed 对应条件
    private bool IsExclusiveInteractionAllowed()
    {
// 返回 IsExclusiveInteractionAllowed 的处理结果
        return IsExclusiveTarget;
    }

// 同步 IsExclusiveInteractionAllowed 的相关数据
    private string InvestigationFlag => clueToGrant != null ? $"investigated_{clueToGrant.ClueId}" : $"investigated_{name}";

// 每帧检查输入与状态变化
    private void Update()
    {
// 检查 Update 的前置条件
        if (GameplayInputLock.IsInteractionLocked)
        {
// 同步 Update 的内部状态
            interactionSuppressed = true;
            HidePrompt();
// 返回 Update 的处理结果
            return;
        }

// 检查 Update 的前置条件（Update）
        if (interactionSuppressed)
        {
// 同步 Update 的内部状态（Update）
            interactionSuppressed = false;
            PlayerInteractionPromptController.RefreshSource(this);
        }

// 检查 Update 的前置条件（Update）（if）
        if (!IsExclusiveInteractionAllowed() || HasCompletedInteraction)
        {
// 推进 Update 中的必要步骤
            HidePrompt();
            return;
        }

// 检测按键输入
        if (!playerInRange || !Input.GetKeyDown(KeyCode.F))
        {
// 返回 Update 的处理结果（Update）
            return;
        }

// 检查对话状态
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.CanOpenDialogue)
        {
// 推进 Update 中的必要步骤（Update）
            Inspect();
        }
    }

// 处理 Inspect 对应逻辑
    private void Inspect()
    {
// 检查 Inspect 的前置条件
        if (HasCompletedInteraction)
        {
// 返回 Inspect 的处理结果
            return;
        }

// 推进 Inspect 中的必要步骤
        HidePrompt();

// 判断 IsNullOrEmpty 对应条件（Inspect）
        bool locked = !string.IsNullOrEmpty(lockedByFlag)
            && GameManager.Instance != null
// 使用 Inspect 所需功能
            && GameManager.Instance.HasFlag(lockedByFlag);

// 检查 Inspect 的前置条件（Inspect）
        if (locked)
        {
// 检查 Inspect 的前置条件（Inspect）（if）
            if (lockedDialogue != null)
            {
// 开始当前对话
                DialogueUIManager.Instance.StartDialogue(lockedDialogue, () =>
                {
// 在 Inspect 中继续当前处理
                    if (playerInRange)
                    {
// 使用 Inspect 所需功能（Inspect）
                        PlayerInteractionPromptController.RefreshSource(this);
                    }
// 推进 Inspect 的当前步骤
                });
            }
// 返回 Inspect 的处理结果（Inspect）
            return;
        }

// 在 Inspect 中继续当前处理（Inspect 后续步骤）
        if (countsAsInvestigation && GameManager.Instance != null
            && !GameManager.Instance.HasFlag(InvestigationFlag))
        {
// 累加调查次数
            GameManager.Instance.AddInvestigation();
            GameManager.Instance.SetFlag(InvestigationFlag);
        }

// 在 Inspect 中继续当前处理（Inspect 后续步骤）（298）
        if (inspectDialogue != null)
        {
// 在 Inspect 中继续当前处理（Inspect 后续步骤）（301）
            DialogueUIManager.Instance.StartDialogue(inspectDialogue, OnInspectFinished);
        }
// 处理 Inspect 的备用分支
        else
        {
// 推进 Inspect 中的必要步骤（Inspect）
            OnInspectFinished();
        }
    }

// 响应 OnInspectFinished 生命周期
    private void OnInspectFinished()
    {
// 检查 OnInspectFinished 的前置条件
        if (clueToGrant != null && GameManager.Instance != null)
        {
// 记录当前线索
            GameManager.Instance.CollectClue(clueToGrant);

// 检查 OnInspectFinished 的前置条件（OnInspectFinished）
            if (disappearAfterPickup)
            {
// 更新剧情标记
                GameManager.Instance.SetFlag(PickupFlag);
                gameObject.SetActive(false);
// 返回 OnInspectFinished 的处理结果
                return;
            }
        }

// 检查 OnInspectFinished 的前置条件（OnInspectFinished）（if）
        if (onlyInteractOnce && GameManager.Instance != null)
        {
// 在 OnInspectFinished 中继续当前处理
            GameManager.Instance.SetFlag(InteractionFlag);
        }

// 在 OnInspectFinished 中继续当前处理（OnInspectFinished 后续步骤）
        if (playerInRange)
        {
// 使用 OnInspectFinished 所需功能
            PlayerInteractionPromptController.RefreshSource(this);
        }
    }

// 玩家进入范围后登记可交互状态
    private void OnTriggerEnter2D(Collider2D other)
    {
// 检查 OnTriggerEnter2D 的前置条件
        if (other.CompareTag(playerTag))
        {
// 同步 OnTriggerEnter2D 的内部状态
            playerInRange = true;
            PlayerInteractionPromptController.RegisterSource(this);
        }
    }

// 玩家离开范围后移除可交互状态
    private void OnTriggerExit2D(Collider2D other)
    {
// 检查 OnTriggerExit2D 的前置条件
        if (other.CompareTag(playerTag))
        {
// 同步 OnTriggerExit2D 的内部状态
            playerInRange = false;
            PlayerInteractionPromptController.UnregisterSource(this);
        }
    }

// 显示 ShowPrompt 对应界面
    private void ShowPrompt()
    {
// 使用 ShowPrompt 所需功能
        PlayerInteractionPromptController.RefreshSource(this);
    }

// 隐藏 HidePrompt 对应界面
    private void HidePrompt()
    {
// 使用 HidePrompt 所需功能
        PlayerInteractionPromptController.RefreshSource(this);
    }
}
