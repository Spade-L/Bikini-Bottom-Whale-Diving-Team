using UnityEngine;

// 调用 RequireComponent
[RequireComponent(typeof(BoxCollider2D))]
public class TimedNPC : MonoBehaviour, IInteractionPromptSource
{
// 更新当前逻辑
    [System.Serializable]
    public class NPCState
    {
// 说明当前配置
        [Tooltip("仅供编辑器辨认，如“第一章·担忧”")]
        public string editorLabel;
// 定义 StoryCondition 方法
        public StoryCondition condition = new StoryCondition();
        public DialogueData dialogue;
    }

// 配置 标识 分组
    [Header("标识")]
    [SerializeField] private string npcId;

// 配置 整体出现条件（可留默认 = 一直出现） 分组
    [Header("整体出现条件（可留默认 = 一直出现）")]
    [Tooltip("不满足则 NPC 隐藏。例：forbiddenFlags 填 lock_npc_talk，调查 18 次后路人消失")]
// 保存 appearCondition 数据
    [SerializeField] private StoryCondition appearCondition = new StoryCondition();

// 配置 状态列表（后面的优先级更高） 分组
    [Header("状态列表（后面的优先级更高）")]
    [SerializeField] private NPCState[] states;

// 配置 离开设定（-1 = 永不离开） 分组
    [Header("离开设定（-1 = 永不离开）")]
    [Tooltip("时间段到达此值时，NPC 离开")]
// 配置 departTimePeriod 数值
    [SerializeField] private int departTimePeriod = -1;
    [Tooltip("此 Flag 已设置则 NPC 不会离开（玩家干涉成功）")]
// 保存 rescueFlag 数据
    [SerializeField] private string rescueFlag;
    [Tooltip("NPC 离开后播放一次的告别对话（可选，需要场景中有其他触发方式则留空）")]
// 保存 fallbackDialogue 引用
    [SerializeField] private DialogueData fallbackDialogue;

// 配置 交互 UI 分组
    [Header("交互 UI")]
    [SerializeField] private GameObject interactionUI;
// 保存 playerTag 数据
    [SerializeField] private string playerTag = "Player";

// 记录 playerInRange 状态
    private bool playerInRange;
    private bool interactionSuppressed;

// 更新当前逻辑
    public bool IsInteractionPromptEligible
    {
// 更新当前逻辑
        get
        {
// 返回当前结果
            return isActiveAndEnabled
                && playerInRange
// 更新当前逻辑
                && !interactionSuppressed
                && ResolveCurrentDialogue() != null;
        }
    }

// 保存 DepartedFlag 数据
    private string DepartedFlag => $"departed_{npcId}";

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
// 判断当前条件
        if (GameManager.Instance != null)
        {
// 更新当前逻辑
            GameManager.Instance.OnTimeAdvanced += HandleStateMayChange;
            GameManager.Instance.OnFlagsChanged += HandleFlagsChanged;
        }

// 执行 RefreshPresence
        RefreshPresence();
    }

// 定义 OnDestroy 方法
    private void OnDestroy()
    {
// 调用 UnregisterSource
        PlayerInteractionPromptController.UnregisterSource(this);

// 判断当前条件
        if (GameManager.Instance != null)
        {
// 更新当前逻辑
            GameManager.Instance.OnTimeAdvanced -= HandleStateMayChange;
            GameManager.Instance.OnFlagsChanged -= HandleFlagsChanged;
        }
    }

// 定义 OnDisable 方法
    private void OnDisable()
    {
// 调用 UnregisterSource
        PlayerInteractionPromptController.UnregisterSource(this);
    }

    // 时间变化
    private void HandleStateMayChange(int _)
    {
// 执行 RefreshPresence
        RefreshPresence();
        PlayerInteractionPromptController.RefreshSource(this);
    }

// 定义 HandleFlagsChanged 方法
    private void HandleFlagsChanged()
    {
// 执行 RefreshPresence
        RefreshPresence();
        PlayerInteractionPromptController.RefreshSource(this);
    }

// 定义 RefreshPresence 方法
    private void RefreshPresence()
    {
// 保存 gm 数据
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
// 返回当前结果
            return;
        }

// 定义 HasFlag 方法
        bool rescued = !string.IsNullOrEmpty(rescueFlag) && gm.HasFlag(rescueFlag);
        bool shouldDepart = departTimePeriod >= 0
// 更新当前逻辑
            && gm.CurrentTimePeriod >= departTimePeriod
            && !rescued;

// 判断当前条件
        if (shouldDepart && !gm.HasFlag(DepartedFlag))
        {
// 更新剧情标记
            gm.SetFlag(DepartedFlag);
        }

// 定义 IsMet 方法
        gameObject.SetActive(!gm.HasFlag(DepartedFlag) && appearCondition.IsMet());
    }

// 定义 GetActiveState 方法
    private NPCState GetActiveState()
    {
// 空引用时直接退出
        if (states == null)
        {
// 返回当前结果
            return null;
        }

// 保存 active 数据
        NPCState active = null;
        foreach (NPCState state in states)
        {
// 判断当前条件
            if (state != null && state.condition.IsMet())
            {
// 更新当前状态
                active = state; // 后面的覆盖前面的
            }
        }

// 返回当前结果
        return active;
    }

// 定义 ResolveCurrentDialogue 方法
    private DialogueData ResolveCurrentDialogue()
    {
// 返回当前结果
        return GetActiveState()?.dialogue ?? fallbackDialogue;
    }

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
            if (playerInRange)
            {
// 执行 ShowPrompt
                ShowPrompt();
            }
        }

// 检测按键输入
        if (!playerInRange || !Input.GetKeyDown(KeyCode.F))
        {
// 返回当前结果
            return;
        }

// 空引用时直接退出
        if (DialogueUIManager.Instance == null || !DialogueUIManager.Instance.CanOpenDialogue)
        {
// 返回当前结果
            return;
        }

// 定义 ResolveCurrentDialogue 方法
        DialogueData dialogue = ResolveCurrentDialogue();

// 判断当前条件
        if (dialogue != null)
        {
// 执行 HidePrompt
            HidePrompt();
            DialogueUIManager.Instance.StartDialogue(dialogue, () =>
            {
// 判断当前条件
                if (playerInRange && gameObject.activeSelf)
                {
// 执行 ShowPrompt
                    ShowPrompt();
                }
// 更新当前逻辑
            });
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
// 调用 RefreshSource
            PlayerInteractionPromptController.RefreshSource(this);
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
