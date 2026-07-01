using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class InteractionPromptTrigger2D : MonoBehaviour
{
    [Header("检测设置")]
    [SerializeField] private string playerTag = "Player";

    [Header("交互 UI")]
    [SerializeField] private GameObject interactionUI;
    [SerializeField] private string dialogueText = "这里还没有设置对话内容。";

    private BoxCollider2D boxCollider;
    private bool playerInRange;
    private bool hasInteractedInCurrentRange;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;

        HideInteractionUI();
    }

    private void Update()
    {
        if (!playerInRange || hasInteractedInCurrentRange || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.CanOpenDialogue)
        {
            Interact();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            hasInteractedInCurrentRange = false;
            ShowInteractionUI();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            hasInteractedInCurrentRange = false;
            HideInteractionUI();
        }
    }

    private void Interact()
    {
        hasInteractedInCurrentRange = true;
        HideInteractionUI();
        DialogueUIManager.Instance.ShowDialogue(dialogueText);
    }

    private void ShowInteractionUI()
    {
        if (interactionUI != null)
        {
            interactionUI.SetActive(true);
        }
    }

    private void HideInteractionUI()
    {
        if (interactionUI != null)
        {
            interactionUI.SetActive(false);
        }
    }
}
