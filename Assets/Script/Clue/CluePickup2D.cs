using UnityEngine;

/// <summary>
/// 场景中可调查/拾取的物品。玩家靠近按 E：
/// 播放调查对话 → 获得线索 → （可选）物品从场景消失。
/// 已拾取状态通过 Flag "picked_<ClueId>" 记录，读档后自动消失。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class CluePickup2D : MonoBehaviour
{
    // 条件集中求值。
    // 刷新不改条件。
    // 消失配置影响可见性。
    // Flag 使用线索 ID。
    // 范围不决定出现。
    // 多处会隐藏提示。
    // 进入后等待确认。
    // 锁定优先处理。
    // 调查可计入统计。
    // 无对话仍可领取。
    // 对话后发放线索。
    // 空管理器不发放。
    // 不消失物件可复查。
    // 拾取后立即隐藏。
    // 事件响应外部变化。
    // 销毁时退订事件。
    // 时间可改变条件。
    // Flag 可改变条件。
    // 线索可改变条件。
    // E 键发起交互。
    // 防止输入穿透。
    // 强制触发器模式。
    // Tag 可配置。
    // 离开时关闭提示。
    // 回调后检查范围。
    // 普通流程同样检查。
    // 空线索无 Flag。
    // 条件改变可重显。
    // 停用不会删除配置。
    // 文本交给管理器。
    // 避免重复计数。
    // 锁定不隐藏物件。
    // ID 用于持久化。
    // 刷新可重复调用。
    // 空提示引用安全。
    // 支持读档恢复。
    // 提示随范围变化。
    // 拾取状态可追踪。
    // 条件可组合。
    // 流程可安全中断。
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
    [Tooltip("此 Flag 被设置后禁止调查，按 E 改为播放封锁台词（如 lock_home_items）")]
    [SerializeField] private string lockedByFlag;
    [Tooltip("封锁后的台词，如“这地方我翻遍了……没有更多线索了。”")]
    [SerializeField] private DialogueData lockedDialogue;

    [Header("交互 UI")]
    [SerializeField] private GameObject interactionUI;
    [SerializeField] private string playerTag = "Player";

    // 仅表示触发器范围；物件隐藏后 Unity 不再接收后续触发回调。
    private bool playerInRange;

    // 未配置线索时返回 null，避免生成无意义的 picked_ Flag。
    private string PickupFlag => clueToGrant != null ? $"picked_{clueToGrant.ClueId}" : null;

    // Awake 先于 Start 执行，确保触发器和提示初始状态在第一帧输入前准备完毕。
    private void Awake()
    {
        // RequireComponent 保证组件存在；这里强制触发器模式以支持靠近检测。
        GetComponent<BoxCollider2D>().isTrigger = true;
        HidePrompt();
    }

    // Start 时读取 GameManager 的当前存档状态，并在其存在时订阅变化事件。
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

    // 必须与 Start 成对退订，防止物件销毁后仍被全局事件回调。
    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeAdvanced -= HandleTimeAdvanced;
            GameManager.Instance.OnFlagsChanged -= HandleFlagsChanged;
            GameManager.Instance.OnClueCollected -= HandleClueCollected;
        }
    }

    // 条件可依赖时间、Flag 或线索，任一相关状态变化都重新计算可见性。
    private void HandleTimeAdvanced(int _) => RefreshVisibility();
    private void HandleFlagsChanged() => RefreshVisibility();
    private void HandleClueCollected(ClueData _) => RefreshVisibility();

    /// <summary>
    /// 将“已拾取且配置为消失”与出现条件合并为最终激活状态。
    /// 不消失的物件仍可重复调查，但 CollectClue 的去重由 GameManager 负责。
    /// </summary>
    private void RefreshVisibility()
    {
        bool alreadyPicked = disappearAfterPickup
            && PickupFlag != null
            && GameManager.Instance != null
            && GameManager.Instance.HasFlag(PickupFlag);

        gameObject.SetActive(!alreadyPicked && appearCondition.IsMet());
    }

    // 只在玩家位于触发器内且按键刚按下时发起调查。
    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        // 对话管理器的冷却时间阻止“结束对话”的同一次按键立即重新触发调查。
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.CanOpenDialogue)
        {
            Inspect();
        }
    }

    /// <summary>
    /// 开始调查流程：封锁台词优先，其余情况先计数再播放调查内容。
    /// 线索在调查对话结束的回调中发放，避免玩家跳过或中断时提前获得。
    /// </summary>
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
                        ShowPrompt();
                    }
                });
            }
            return;
        }

        if (countsAsInvestigation && GameManager.Instance != null)
        {
            GameManager.Instance.AddInvestigation();
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

    // 对话完成回调或无对话的直接路径共用此处，保证发放时机一致。
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
            ShowPrompt();
        }
    }

    // Tag 可在 Inspector 配置，项目中的玩家对象必须使用相同 Tag。
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            ShowPrompt();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            HidePrompt();
        }
    }

    private void ShowPrompt()
    {
        if (interactionUI != null)
        {
            interactionUI.SetActive(true);
        }
    }

    private void HidePrompt()
    {
        if (interactionUI != null)
        {
            interactionUI.SetActive(false);
        }
    }
}
