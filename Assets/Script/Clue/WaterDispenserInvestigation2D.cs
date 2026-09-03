using UnityEngine;

/// <summary>
/// 饮水机专用调查：饮水机自身的 BoxCollider2D 同时承担玩家重叠检测和交互范围。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class WaterDispenserInvestigation2D : MonoBehaviour
{
    [Header("调查内容")]
    [SerializeField] private DialogueData firstDialogue;
    [SerializeField] private DialogueData revealDialogue;
    [SerializeField] private DialogueData repeatDialogue;
    [SerializeField] private ClueData clueToGrant;

    [Header("场景切换")]
    [SerializeField] private GameObject classroomRoot;
    [SerializeField] private GameObject hiddenRoomRoot;
    [SerializeField] private Vector3 initialPosition = new Vector3(18f, 2f, 0f);
    [SerializeField] private Vector3 movedPosition = new Vector3(12f, 2f, 0f);
    [SerializeField] private string movedFlag = "school_water_dispenser_moved";

    [Header("交互 UI")]
    [SerializeField] private GameObject interactionUI;
    [SerializeField] private string playerTag = "Player";

    private bool playerInRange;
    private bool dialoguePlaying;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        HidePrompt();
    }

    private void Start()
    {
        ApplyState(IsMoved());
        RefreshRoomVisibility();
    }

    private void OnDestroy()
    {
        HidePrompt();
    }

    private void Update()
    {
        if (!playerInRange || dialoguePlaying || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (DialogueUIManager.Instance == null || !DialogueUIManager.Instance.CanOpenDialogue)
        {
            return;
        }

        HidePrompt();
        dialoguePlaying = true;
        DialogueData dialogue = IsMoved() ? repeatDialogue : firstDialogue;
        if (dialogue == null)
        {
            FinishInteraction();
            return;
        }

        DialogueUIManager.Instance.StartDialogue(dialogue, FinishInteraction);
    }

    private void FinishInteraction()
    {
        dialoguePlaying = false;

        if (!IsMoved())
        {
            if (clueToGrant != null && GameManager.Instance != null)
            {
                GameManager.Instance.CollectClue(clueToGrant);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddInvestigation();
                GameManager.Instance.SetFlag(movedFlag);
            }

            // 移动前清除旧位置的重叠状态；玩家必须重新进入移动后的饮水机碰撞体才显示暗室。
            playerInRange = false;
            HidePrompt();
            ApplyState(true);
            RefreshRoomVisibility();

            if (revealDialogue != null)
            {
                dialoguePlaying = true;
                DialogueUIManager.Instance.StartDialogue(revealDialogue, FinishRevealDialogue);
                return;
            }
        }

        ShowPromptIfInRange();
    }

    private void FinishRevealDialogue()
    {
        dialoguePlaying = false;
        ShowPromptIfInRange();
    }

    private void ShowPromptIfInRange()
    {
        if (playerInRange)
        {
            ShowPrompt();
        }
    }

    private bool IsMoved()
    {
        return GameManager.Instance != null && GameManager.Instance.HasFlag(movedFlag);
    }

    private void ApplyState(bool moved)
    {
        transform.position = moved ? movedPosition : initialPosition;
    }

    private void RefreshRoomVisibility()
    {
        bool showHiddenRoom = IsMoved() && playerInRange;

        if (classroomRoot != null)
        {
            classroomRoot.SetActive(!showHiddenRoom);
        }

        if (hiddenRoomRoot != null)
        {
            hiddenRoomRoot.SetActive(showHiddenRoom);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            RefreshRoomVisibility();
            ShowPrompt();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            RefreshRoomVisibility();
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
