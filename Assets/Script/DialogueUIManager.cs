using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueUIManager : MonoBehaviour
{
    public static DialogueUIManager Instance { get; private set; }

    // 所有 UI 引用均需在 Inspector 配置；缺失引用会降级跳过对应显示
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

    [Header("音效")]
    [Tooltip("对话框弹出时播放一次的音效（不循环）")]
    [SerializeField] private AudioClip openSound;
    [Range(0f, 1f)]
    [SerializeField] private float openSoundVolume = 1f;

    [Header("输入")]
    [SerializeField] private KeyCode advanceKey = KeyCode.F;
    [SerializeField] private float reopenInputDelay = 0.1f;

    // 每段对话的运行时游标；不会写回 ScriptableObject 资产
    private DialogueData currentDialogue;
    private int currentLineIndex;
    private Coroutine typingCoroutine;
    private bool isTyping;
    private Action onDialogueComplete;

    // 关闭时间配合冷却，避免推进最后一行的按键穿透到场景交互
    public bool IsDialogueOpen => dialoguePanel != null && dialoguePanel.activeSelf;
    public bool CanOpenDialogue => !IsDialogueOpen && Time.time >= LastClosedTime + reopenInputDelay;
    public float LastClosedTime { get; private set; } = -999f;

    // 单例初始化发生在 Start 前，供同帧初始化的交互组件查询
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 重复实例自毁，不覆盖已有 Instance，避免场景切换时调用目标不稳定
        Instance = this;
        HidePanel();
    }

    // 输入只在面板打开时消费；打字和换行共享同一推进键
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

    public bool StartDialogue(DialogueData dialogue, Action onComplete = null)
    {
        // 空对话或缺少必要 UI 时明确报告失败，避免调用方误认为对白已完成
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0)
        {
            return false;
        }

        if (dialoguePanel == null || dialogueText == null)
        {
            Debug.LogError("DialogueUIManager 无法开始对白：dialoguePanel 或 dialogueText 未绑定。", this);
            return false;
        }

        currentDialogue = dialogue;
        currentLineIndex = 0;
        onDialogueComplete = onComplete;

        // 激活面板后再显示首行，确保 TMP 的可见字符数据可以正确计算
        dialoguePanel.SetActive(true);

        if (openSound != null && SfxManager.Instance != null)
        {
            SfxManager.Instance.Play(openSound, openSoundVolume);
        }

        ShowCurrentLine();
        return true;
    }

    public void ShowDialogue(string text)
    {
        DialogueData temp = ScriptableObject.CreateInstance<DialogueData>();
        temp.lines = new[] { new DialogueData.Line { text = text } };
        StartDialogue(temp);
    }

    private void ShowCurrentLine()
    {
        DialogueData.Line line = currentDialogue.lines[currentLineIndex];

        // 名字与正文均在展示时解析令牌，故同一资产可随剧情状态显示不同称谓
        if (speakerNameText != null)
        {
            string name = TextTokens.Resolve(line.ResolveSpeakerName());
            bool hasName = !string.IsNullOrEmpty(name);
            speakerNameText.gameObject.SetActive(hasName);
            speakerNameText.text = hasName ? name : string.Empty;
        }

        // 立绘为空时主动隐藏 Image，避免上一行的图片残留
        if (portraitImage != null)
        {
            CharacterData character = ResolveCharacter(line);
            Sprite portrait = character != null
                ? character.GetPortrait(line.expression)
                : null;
            portraitImage.sprite = portrait;
            portraitImage.gameObject.SetActive(portrait != null);
        }

        // 开始新行时隐藏提示，只有全文显示完才提示玩家继续
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

        dialogueText.ForceMeshUpdate();
        int totalChars = dialogueText.textInfo.characterCount;

        // 用 maxVisibleCharacters 而非逐字拼接，避免富文本标签被截断
        float visibleCount = 0f;

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
