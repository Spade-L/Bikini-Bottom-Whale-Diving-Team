using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class WaterDispenserInvestigation2D : MonoBehaviour, IInteractionPromptSource
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

    private readonly HashSet<Collider2D> overlappingPlayerColliders = new HashSet<Collider2D>();
    private bool playerInRange => overlappingPlayerColliders.Count > 0;
    private bool dialoguePlaying;
    private bool interactionSuppressed;

    public bool IsInteractionPromptEligible
    {
        get
        {
            return isActiveAndEnabled
                && playerInRange
                && !dialoguePlaying
                && !interactionSuppressed;
        }
    }

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

    // 销毁时解除事件关系
    private void OnDestroy()
    {
        PlayerInteractionPromptController.UnregisterSource(this);
        HidePrompt();
    }

    private void OnDisable()
    {
        PlayerInteractionPromptController.UnregisterSource(this);
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
            ShowPromptIfInRange();
        }

        if (!playerInRange || dialoguePlaying || !Input.GetKeyDown(KeyCode.F))
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

            // 移动前清除旧位置的重叠状态；玩家必须重新进入移动后的饮水机碰撞体才显示暗室
            overlappingPlayerColliders.Clear();
            PlayerInteractionPromptController.UnregisterSource(this);
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

    // 进入触发区域后记录对象
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            bool wasInRange = playerInRange;
            overlappingPlayerColliders.Add(other);
            RefreshRoomVisibility();
            if (!wasInRange)
            {
                PlayerInteractionPromptController.RegisterSource(this);
            }
            PlayerInteractionPromptController.RefreshSource(this);
        }
    }

    // 离开触发区域后清除记录
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            overlappingPlayerColliders.Remove(other);
            RefreshRoomVisibility();
            if (!playerInRange)
            {
                PlayerInteractionPromptController.UnregisterSource(this);
            }
            PlayerInteractionPromptController.RefreshSource(this);
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
