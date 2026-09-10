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

    [Header("移动")]
    [SerializeField] private Vector3 initialLocalPosition = new Vector3(18f, 8f, 0f);
    [SerializeField] private Vector3 movedLocalPosition = new Vector3(12f, 8f, 0f);
    [SerializeField] private string movedFlag = "school_water_dispenser_moved";
    [SerializeField] private string roomReadyFlag = "school_water_dispenser_room_ready";

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
        ApplyState(IsRoomReady());
    }

    private void OnDisable()
    {
        overlappingPlayerColliders.Clear();
        PlayerInteractionPromptController.UnregisterSource(this);
        CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
    }

    private void OnDestroy()
    {
        PlayerInteractionPromptController.UnregisterSource(this);
        CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
        HidePrompt();
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

        bool completingFirst = !HasCompletedFirstInteraction();
        bool completingMove = !completingFirst && !IsRoomReady();
        DialogueData dialogue = completingFirst
            ? firstDialogue
            : completingMove ? revealDialogue : repeatDialogue;

        HidePrompt();
        dialoguePlaying = true;
        if (dialogue == null)
        {
            FinishInteraction(completingFirst, completingMove);
            return;
        }

        DialogueUIManager.Instance.StartDialogue(
            dialogue,
            () => FinishInteraction(completingFirst, completingMove));
    }

    private void FinishInteraction(bool completingFirst, bool completingMove)
    {
        dialoguePlaying = false;

        if (completingFirst)
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
        }
        else if (completingMove)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetFlag(roomReadyFlag);
            }

            overlappingPlayerColliders.Clear();
            CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
            PlayerInteractionPromptController.UnregisterSource(this);
            HidePrompt();
            ApplyState(true);
        }

        ShowPromptIfInRange();
    }

    private void ShowPromptIfInRange()
    {
        if (playerInRange)
        {
            ShowPrompt();
        }
    }

    private bool HasCompletedFirstInteraction()
    {
        return GameManager.Instance != null && GameManager.Instance.HasFlag(movedFlag);
    }

    private bool IsRoomReady()
    {
        return GameManager.Instance != null && GameManager.Instance.HasFlag(roomReadyFlag);
    }

    private void ApplyState(bool moved)
    {
        transform.localPosition = moved ? movedLocalPosition : initialLocalPosition;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            bool wasInRange = playerInRange;
            overlappingPlayerColliders.Add(other);
            if (!wasInRange)
            {
                PlayerInteractionPromptController.RegisterSource(this);
                CluePickup2D.SetExclusiveInteractionTarget(gameObject);
            }
            PlayerInteractionPromptController.RefreshSource(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            overlappingPlayerColliders.Remove(other);
            if (!playerInRange)
            {
                PlayerInteractionPromptController.UnregisterSource(this);
                CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
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