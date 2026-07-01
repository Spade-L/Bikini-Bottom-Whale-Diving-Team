using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUIManager : MonoBehaviour
{
    public static DialogueUIManager Instance { get; private set; }

    [Header("对话框 UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text tmpDialogueText;
    [SerializeField] private Text legacyDialogueText;

    [Header("关闭设置")]
    [SerializeField] private float closeInputDelay = 0.5f;
    [SerializeField] private float reopenInputDelay = 0.1f;

    private float canCloseTime;

    public bool IsDialogueOpen => dialoguePanel != null && dialoguePanel.activeSelf;
    public bool CanOpenDialogue => !IsDialogueOpen && Time.time >= LastClosedTime + reopenInputDelay;
    public float LastClosedTime { get; private set; } = -999f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        HideDialogue();
    }

    private void Update()
    {
        if (!IsDialogueOpen)
        {
            return;
        }

        if (Time.time >= canCloseTime && Input.GetKeyDown(KeyCode.E))
        {
            HideDialogue();
        }
    }

    public void ShowDialogue(string dialogueText)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        canCloseTime = Time.time + closeInputDelay;

        if (tmpDialogueText != null)
        {
            tmpDialogueText.text = dialogueText;
        }

        if (legacyDialogueText != null)
        {
            legacyDialogueText.text = dialogueText;
        }
    }

    public void HideDialogue()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        LastClosedTime = Time.time;
    }
}
