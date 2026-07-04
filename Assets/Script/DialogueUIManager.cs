using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 对话 UI：支持多行对话、打字机效果、说话人名字。
/// 按 E 推进：打字中 → 立刻显示全文；已显示全文 → 下一行；最后一行 → 关闭并应用剧情效果。
/// </summary>
public class DialogueUIManager : MonoBehaviour
{
    public static DialogueUIManager Instance { get; private set; }

    [Header("对话框 UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private GameObject continueIndicator;

    [Header("立绘（对话框左侧）")]
    [SerializeField] private UnityEngine.UI.Image portraitImage;

    [Header("主角立绘（说话人为「我」时自动使用）")]
    [Tooltip("男主角（哥哥线）立绘资产")]
    [SerializeField] private CharacterData protagonistMale;
    [Tooltip("女主角（姐姐线）立绘资产，未设置 gender_female flag 时用男主角")]
    [SerializeField] private CharacterData protagonistFemale;
    [Tooltip("触发主角立绘的说话人名字")]
    [SerializeField] private string protagonistSpeakerName = "我";

    [Header("打字机")]
    [SerializeField] private float charsPerSecond = 30f;

    [Header("输入")]
    [SerializeField] private KeyCode advanceKey = KeyCode.E;
    [SerializeField] private float reopenInputDelay = 0.1f;

    private DialogueData currentDialogue;
    private int currentLineIndex;
    private Coroutine typingCoroutine;
    private bool isTyping;
    private Action onDialogueComplete;

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
        HidePanel();
    }

    private void Update()
    {
        if (!IsDialogueOpen || !Input.GetKeyDown(advanceKey))
        {
            return;
        }

        if (isTyping)
        {
            SkipTyping();
        }
        else
        {
            AdvanceLine();
        }
    }

    /// <summary>播放一段 DialogueData 对话。onComplete 在对话关闭后调用。</summary>
    public void StartDialogue(DialogueData dialogue, Action onComplete = null)
    {
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0)
        {
            return;
        }

        currentDialogue = dialogue;
        currentLineIndex = 0;
        onDialogueComplete = onComplete;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        ShowCurrentLine();
    }

    /// <summary>兼容旧接口：显示单条纯文本。</summary>
    public void ShowDialogue(string text)
    {
        DialogueData temp = ScriptableObject.CreateInstance<DialogueData>();
        temp.lines = new[] { new DialogueData.Line { text = text } };
        StartDialogue(temp);
    }

    private void ShowCurrentLine()
    {
        DialogueData.Line line = currentDialogue.lines[currentLineIndex];

        if (speakerNameText != null)
        {
            string name = TextTokens.Resolve(line.ResolveSpeakerName());
            bool hasName = !string.IsNullOrEmpty(name);
            speakerNameText.gameObject.SetActive(hasName);
            speakerNameText.text = hasName ? name : string.Empty;
        }

        if (portraitImage != null)
        {
            CharacterData character = ResolveCharacter(line);
            Sprite portrait = character != null
                ? character.GetPortrait(line.expression)
                : null;
            portraitImage.sprite = portrait;
            portraitImage.gameObject.SetActive(portrait != null);
        }

        if (continueIndicator != null)
        {
            continueIndicator.SetActive(false);
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeLine(TextTokens.Resolve(line.text)));
    }

    /// <summary>
    /// 决定本行用哪个立绘：行里直接指定的优先；
    /// 否则说话人叫「我」时自动用主角立绘（按 gender_female flag 选男/女版）。
    /// </summary>
    private CharacterData ResolveCharacter(DialogueData.Line line)
    {
        if (line.character != null)
        {
            return line.character;
        }

        if (line.speakerName == protagonistSpeakerName)
        {
            bool female = GameManager.Instance != null
                && GameManager.Instance.HasFlag(TextTokens.FemaleFlag);
            return female && protagonistFemale != null ? protagonistFemale : protagonistMale;
        }

        return null;
    }

    private IEnumerator TypeLine(string text)
    {
        isTyping = true;
        dialogueText.text = text;
        dialogueText.maxVisibleCharacters = 0;

        // 用 maxVisibleCharacters 而非逐字拼接，避免富文本标签被截断
        float visibleCount = 0f;
        int totalChars = dialogueText.GetParsedText().Length;

        while (dialogueText.maxVisibleCharacters < totalChars)
        {
            visibleCount += charsPerSecond * Time.deltaTime;
            dialogueText.maxVisibleCharacters = Mathf.Min(totalChars, Mathf.FloorToInt(visibleCount));
            yield return null;
        }

        FinishTyping();
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
        FinishTyping();
    }

    private void FinishTyping()
    {
        isTyping = false;
        typingCoroutine = null;

        if (continueIndicator != null)
        {
            continueIndicator.SetActive(true);
        }
    }

    private void AdvanceLine()
    {
        currentLineIndex++;

        if (currentLineIndex < currentDialogue.lines.Length)
        {
            ShowCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        DialogueData finished = currentDialogue;
        Action callback = onDialogueComplete;

        currentDialogue = null;
        onDialogueComplete = null;
        HidePanel();

        finished.ApplyCompletionEffects();
        callback?.Invoke();
    }

    private void HidePanel()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        LastClosedTime = Time.time;
    }
}
