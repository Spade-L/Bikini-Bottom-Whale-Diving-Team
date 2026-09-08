using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TimedNPC : MonoBehaviour, IInteractionPromptSource
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
    private bool interactionSuppressed;

    public bool IsInteractionPromptEligible
    {
        get
        {
            return isActiveAndEnabled
                && playerInRange
                && !interactionSuppressed
                && ResolveCurrentDialogue() != null;
        }
    }

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
            GameManager.Instance.OnFlagsChanged += HandleFlagsChanged;
        }

        RefreshPresence();
    }

    private void OnDestroy()
    {
        PlayerInteractionPromptController.UnregisterSource(this);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeAdvanced -= HandleStateMayChange;
            GameManager.Instance.OnFlagsChanged -= HandleFlagsChanged;
        }
    }

    private void OnDisable()
    {
        PlayerInteractionPromptController.UnregisterSource(this);
    }

    // 时间变化
    private void HandleStateMayChange(int _)
    {
        RefreshPresence();
        PlayerInteractionPromptController.RefreshSource(this);
    }

    private void HandleFlagsChanged()
    {
        RefreshPresence();
        PlayerInteractionPromptController.RefreshSource(this);
    }

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

    private DialogueData ResolveCurrentDialogue()
    {
        return GetActiveState()?.dialogue ?? fallbackDialogue;
    }

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
            if (playerInRange)
            {
                ShowPrompt();
            }
        }

        if (!playerInRange || !Input.GetKeyDown(KeyCode.F))
        {
            return;
        }

        if (DialogueUIManager.Instance == null || !DialogueUIManager.Instance.CanOpenDialogue)
        {
            return;
        }

        DialogueData dialogue = ResolveCurrentDialogue();

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
            PlayerInteractionPromptController.RegisterSource(this);
            PlayerInteractionPromptController.RefreshSource(this);
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
