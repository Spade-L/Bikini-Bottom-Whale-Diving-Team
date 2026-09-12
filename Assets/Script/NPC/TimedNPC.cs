using UnityEngine;

// 使用 当前脚本 所需功能
[RequireComponent(typeof(BoxCollider2D))]
public class TimedNPC : MonoBehaviour, IInteractionPromptSource
{
// 推进 TimedNPC 的当前步骤
    [System.Serializable]
    public class NPCState
    {
// 说明当前配置
        [Tooltip("仅供编辑器辨认，如“第一章·担忧”")]
        public string editorLabel;
// 完成 NPCState 的主要职责
        public StoryCondition condition = new StoryCondition();
        public DialogueData dialogue;
    }

// 配置 标识 分组
    [Header("标识")]
    [SerializeField] private string npcId;

// 配置 整体出现条件（可留默认 = 一直出现） 分组
    [Header("整体出现条件（可留默认 = 一直出现）")]
    [Tooltip("不满足则 NPC 隐藏。例：forbiddenFlags 填 lock_npc_talk，调查 18 次后路人消失")]
// 同步 NPCState 的相关数据
    [SerializeField] private StoryCondition appearCondition = new StoryCondition();

// 配置 状态列表（后面的优先级更高） 分组
    [Header("状态列表（后面的优先级更高）")]
    [SerializeField] private NPCState[] states;

// 配置 离开设定（-1 = 永不离开） 分组
    [Header("离开设定（-1 = 永不离开）")]
    [Tooltip("时间段到达此值时，NPC 离开")]
// 设置 NPCState 的配置数值
    [SerializeField] private int departTimePeriod = -1;
    [Tooltip("此 Flag 已设置则 NPC 不会离开（玩家干涉成功）")]
// 同步 NPCState 的相关数据（NPCState 后续步骤）
    [SerializeField] private string rescueFlag;
    [Tooltip("NPC 离开后播放一次的告别对话（可选，需要场景中有其他触发方式则留空）")]
// 保存 fallbackDialogue 引用
    [SerializeField] private DialogueData fallbackDialogue;

// 配置 交互 UI 分组
    [Header("交互 UI")]
    [SerializeField] private GameObject interactionUI;
// 同步 NPCState 的相关数据（NPCState 后续步骤）（47）
    [SerializeField] private string playerTag = "Player";

// 记录 NPCState 的当前状态
    private bool playerInRange;
    private bool interactionSuppressed;

// 推进 NPCState 的当前步骤
    public bool IsInteractionPromptEligible
    {
// 推进 NPCState 的当前步骤（NPCState）
        get
        {
// 返回 NPCState 的处理结果
            return isActiveAndEnabled
                && playerInRange
// 推进 NPCState 的当前步骤（NPCState）（interactionSuppressed）
                && !interactionSuppressed
                && ResolveCurrentDialogue() != null;
        }
    }

// 同步 NPCState 的相关数据（NPCState 后续步骤）（69）
    private string DepartedFlag => $"departed_{npcId}";

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
// 检查 Start 的前置条件
        if (GameManager.Instance != null)
        {
// 推进 Start 的当前步骤
            GameManager.Instance.OnTimeAdvanced += HandleStateMayChange;
            GameManager.Instance.OnFlagsChanged += HandleFlagsChanged;
        }

// 推进 Start 中的必要步骤
        RefreshPresence();
    }

// 销毁时释放事件订阅和静态引用
    private void OnDestroy()
    {
// 使用 OnDestroy 所需功能
        PlayerInteractionPromptController.UnregisterSource(this);

// 检查 OnDestroy 的前置条件
        if (GameManager.Instance != null)
        {
// 推进 OnDestroy 的当前步骤
            GameManager.Instance.OnTimeAdvanced -= HandleStateMayChange;
            GameManager.Instance.OnFlagsChanged -= HandleFlagsChanged;
        }
    }

// 禁用时取消订阅并清理临时状态
    private void OnDisable()
    {
// 使用 OnDisable 所需功能
        PlayerInteractionPromptController.UnregisterSource(this);
    }

    // 响应 HandleStateMayChange 状态变化
    private void HandleStateMayChange(int _)
    {
// 推进 HandleStateMayChange 中的必要步骤
        RefreshPresence();
        PlayerInteractionPromptController.RefreshSource(this);
    }

// 剧情标记变化后同步当前界面与交互
    private void HandleFlagsChanged()
    {
// 推进 HandleFlagsChanged 中的必要步骤
        RefreshPresence();
        PlayerInteractionPromptController.RefreshSource(this);
    }

// 刷新 RefreshPresence 对应状态
    private void RefreshPresence()
    {
// 同步 RefreshPresence 的相关数据
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
// 返回 RefreshPresence 的处理结果
            return;
        }

// 判断 HasFlag 对应条件
        bool rescued = !string.IsNullOrEmpty(rescueFlag) && gm.HasFlag(rescueFlag);
        bool shouldDepart = departTimePeriod >= 0
// 推进 RefreshPresence 的当前步骤
            && gm.CurrentTimePeriod >= departTimePeriod
            && !rescued;

// 检查 RefreshPresence 的前置条件
        if (shouldDepart && !gm.HasFlag(DepartedFlag))
        {
// 更新剧情标记
            gm.SetFlag(DepartedFlag);
        }

// 判断 IsMet 对应条件
        gameObject.SetActive(!gm.HasFlag(DepartedFlag) && appearCondition.IsMet());
    }

// 获取 GetActiveState 所需引用
    private NPCState GetActiveState()
    {
// GetActiveState 缺少引用时提前结束
        if (states == null)
        {
// 返回 GetActiveState 的处理结果
            return null;
        }

// 同步 GetActiveState 的相关数据
        NPCState active = null;
        foreach (NPCState state in states)
        {
// 检查 GetActiveState 的前置条件
            if (state != null && state.condition.IsMet())
            {
// 同步 GetActiveState 的状态
                active = state; // 后面的覆盖前面的
            }
        }

// 返回 GetActiveState 的处理结果（GetActiveState）
        return active;
    }

// 解析 ResolveCurrentDialogue 对应结果
    private DialogueData ResolveCurrentDialogue()
    {
// 返回 ResolveCurrentDialogue 的处理结果
        return GetActiveState()?.dialogue ?? fallbackDialogue;
    }

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
            if (playerInRange)
            {
// 推进 Update 中的必要步骤
                ShowPrompt();
            }
        }

// 检测按键输入
        if (!playerInRange || !Input.GetKeyDown(KeyCode.F))
        {
// 返回 Update 的处理结果（Update）
            return;
        }

        TriggerInteraction();
    }

    public void TriggerInteraction()
    {
        if (GameplayInputLock.IsInteractionLocked || !IsInteractionPromptEligible)
        {
            return;
        }

        if (DialogueUIManager.Instance == null || !DialogueUIManager.Instance.CanOpenDialogue)
        {
            return;
        }

// 解析 ResolveCurrentDialogue 对应结果（Update）
        DialogueData dialogue = ResolveCurrentDialogue();

// 检查 Update 的前置条件（Update）（if）
        if (dialogue != null)
        {
// 推进 Update 中的必要步骤（Update）
            HidePrompt();
            DialogueUIManager.Instance.StartDialogue(dialogue, () =>
            {
// 在 Update 中继续当前处理
                if (playerInRange && gameObject.activeSelf)
                {
// 在 Update 中处理 ShowPrompt
                    ShowPrompt();
                }
// 推进 Update 的当前步骤
            });
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
// 使用 OnTriggerEnter2D 所需功能
            PlayerInteractionPromptController.RefreshSource(this);
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
