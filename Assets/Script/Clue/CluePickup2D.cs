using UnityEngine;

/// <summary>
/// 场景中可调查/拾取的物品。玩家靠近按 E：
/// 播放调查对话 → 获得线索 → （可选）物品从场景消失。
/// 已拾取状态通过 Flag "picked_<ClueId>" 记录，读档后自动消失。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class CluePickup2D : MonoBehaviour
{
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

    private bool playerInRange;

    private string PickupFlag => clueToGrant != null ? $"picked_{clueToGrant.ClueId}" : null;

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
            GameManager.Instance.OnFlagSet += HandleFlagSet;
            GameManager.Instance.OnClueCollected += HandleClueCollected;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeAdvanced -= HandleTimeAdvanced;
            GameManager.Instance.OnFlagSet -= HandleFlagSet;
            GameManager.Instance.OnClueCollected -= HandleClueCollected;
        }
    }

    private void HandleTimeAdvanced(int _) => RefreshVisibility();
    private void HandleFlagSet(string _) => RefreshVisibility();
    private void HandleClueCollected(ClueData _) => RefreshVisibility();

    private void RefreshVisibility()
    {
        bool alreadyPicked = disappearAfterPickup
            && PickupFlag != null
            && GameManager.Instance != null
            && GameManager.Instance.HasFlag(PickupFlag);

        gameObject.SetActive(!alreadyPicked && appearCondition.IsMet());
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E))
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
