using UnityEngine;

/// <summary>
/// 随时间变化、可能永久离开的 NPC。
///
/// 工作方式：
/// 1. states 按顺序排列，每个状态有自己的出现条件和对话（同一 NPC 不同时间段说不同的话）。
/// 2. 每次时间推进/Flag 变化时，取「最后一个条件满足的状态」为当前状态。
/// 3. 若设置了 departTimePeriod：时间到达后，除非 rescueFlag 已被设置（玩家完成了干涉），
///    否则 NPC 永久消失，并设置 "departed_<npcId>" flag 供后续剧情引用
///    （比如其他 NPC 提起“TA 已经走了”）。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class TimedNPC : MonoBehaviour
{
    [System.Serializable]
    public class NPCState
    {
        [Tooltip("仅供编辑器辨认，如“第一章·担忧”")]
        public string editorLabel;
        public StoryCondition condition = new StoryCondition();
        public DialogueData dialogue;
    }

    [Header("标识")]
    [SerializeField] private string npcId;

    [Header("整体出现条件（可留默认 = 一直出现）")]
    [Tooltip("不满足则 NPC 隐藏。例：forbiddenFlags 填 lock_npc_talk，调查 18 次后路人消失")]
    [SerializeField] private StoryCondition appearCondition = new StoryCondition();

    [Header("状态列表（后面的优先级更高）")]
    [SerializeField] private NPCState[] states;

    [Header("离开设定（-1 = 永不离开）")]
    [Tooltip("时间段到达此值时，NPC 离开")]
    [SerializeField] private int departTimePeriod = -1;
    [Tooltip("此 Flag 已设置则 NPC 不会离开（玩家干涉成功）")]
    [SerializeField] private string rescueFlag;
    [Tooltip("NPC 离开后播放一次的告别对话（可选，需要场景中有其他触发方式则留空）")]
    [SerializeField] private DialogueData fallbackDialogue;

    [Header("交互 UI")]
    [SerializeField] private GameObject interactionUI;
    [SerializeField] private string playerTag = "Player";

    private bool playerInRange;

    private string DepartedFlag => $"departed_{npcId}";

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        HidePrompt();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeAdvanced += HandleStateMayChange;
            GameManager.Instance.OnFlagSet += HandleFlagSet;
        }

        RefreshPresence();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeAdvanced -= HandleStateMayChange;
            GameManager.Instance.OnFlagSet -= HandleFlagSet;
        }
    }

    private void HandleStateMayChange(int _) => RefreshPresence();
    private void HandleFlagSet(string _) => RefreshPresence();

    /// <summary>判定 NPC 当前是否在场，并同步离开 Flag。</summary>
    private void RefreshPresence()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            return;
        }

        bool rescued = !string.IsNullOrEmpty(rescueFlag) && gm.HasFlag(rescueFlag);
        bool shouldDepart = departTimePeriod >= 0
            && gm.CurrentTimePeriod >= departTimePeriod
            && !rescued;

        if (shouldDepart && !gm.HasFlag(DepartedFlag))
        {
            gm.SetFlag(DepartedFlag);
        }

        gameObject.SetActive(!gm.HasFlag(DepartedFlag) && appearCondition.IsMet());
    }

    private NPCState GetActiveState()
    {
        if (states == null)
        {
            return null;
        }

        NPCState active = null;
        foreach (NPCState state in states)
        {
            if (state != null && state.condition.IsMet())
            {
                active = state; // 后面的覆盖前面的
            }
        }

        return active;
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (DialogueUIManager.Instance == null || !DialogueUIManager.Instance.CanOpenDialogue)
        {
            return;
        }

        NPCState state = GetActiveState();
        DialogueData dialogue = state != null ? state.dialogue : fallbackDialogue;

        if (dialogue != null)
        {
            HidePrompt();
            DialogueUIManager.Instance.StartDialogue(dialogue, () =>
            {
                if (playerInRange && gameObject.activeSelf)
                {
                    ShowPrompt();
                }
            });
        }
    }

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
