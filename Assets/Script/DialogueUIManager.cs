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
    // 单例提供访问。
    // 空面板视为关闭。
    // UI 可单独降级。
    // 主角可自动识别。
    // 女主需配置资产。
    // 单位为字符每秒。
    // 音效按条件播放。
    // 延迟防止穿透。
    // 只保存运行时状态。
    // 索引指向当前行。
    // 协程可随时停止。
    // 回调用于衔接。
    // 统一记录关闭时间。
    // 重复实例自毁。
    // 初始隐藏面板。
    // 开启时处理输入。
    // 空对话不启动。
    // 每次从首行开始。
    // 激活后显示首行。
    // 临时对话不保存。
    // 空名字隐藏栏位。
    // 空角色隐藏立绘。
    // 全文后显示提示。
    // 换行停止旧协程。
    // 显示时解析令牌。
    // 行角色优先。
    // 使用原始说话人名。
    // 角色负责表情回退。
    // 富文本按可见数显示。
    // 强制刷新字符数。
    // 速度不依赖帧率。
    // 跳过复用完成流程。
    // 完成后清空协程。
    // 末行先缓存状态。
    // 先关闭再生效。
    // 生效后调用回调。
    // 此处记录冷却。
    // 下行覆盖旧文本。
    // 不处理其他输入。
    // 样式由预制体定义。
    // 不支持并行对话。
    // 效果由数据执行。
    // 关闭即不可见。
    // 边界允许重开。
    // 统一管理面板。
    // 输入按行推进。
    // 文本支持跳过。
    // 立绘随行切换。
    // 指示器按状态显示。
    // 冷却防止重触发。
    // 回调在结束后执行。
    public static DialogueUIManager Instance { get; private set; }

    // 所有 UI 引用均需在 Inspector 配置；缺失引用会降级跳过对应显示。
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
    [SerializeField] private KeyCode advanceKey = KeyCode.E;
    [SerializeField] private float reopenInputDelay = 0.1f;

    // 每段对话的运行时游标；不会写回 ScriptableObject 资产。
    private DialogueData currentDialogue;
    private int currentLineIndex;
    private Coroutine typingCoroutine;
    private bool isTyping;
    private Action onDialogueComplete;

    // 关闭时间配合冷却，避免推进最后一行的按键穿透到场景交互。
    public bool IsDialogueOpen => dialoguePanel != null && dialoguePanel.activeSelf;
    public bool CanOpenDialogue => !IsDialogueOpen && Time.time >= LastClosedTime + reopenInputDelay;
    public float LastClosedTime { get; private set; } = -999f;

    // 单例初始化发生在 Start 前，供同帧初始化的交互组件查询。
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 重复实例自毁，不覆盖已有 Instance，避免场景切换时调用目标不稳定。
        Instance = this;
        HidePanel();
    }

    // 输入只在面板打开时消费；打字和换行共享同一推进键。
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

    /// <summary>
    /// 开始播放一个非空对话。
    /// 该方法不检查 CanOpenDialogue；调用方负责在场景交互处避免覆盖当前对话。
    /// </summary>
    public void StartDialogue(DialogueData dialogue, Action onComplete = null)
    {
        // 空对话不打开面板，也不会调用完成回调。
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0)
        {
            return;
        }

        currentDialogue = dialogue;
        currentLineIndex = 0;
        onDialogueComplete = onComplete;

        // 激活面板后再显示首行，确保 TMP 的可见字符数据可以正确计算。
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        if (openSound != null && SfxManager.Instance != null)
        {
            SfxManager.Instance.Play(openSound, openSoundVolume);
        }

        ShowCurrentLine();
    }

    /// <summary>
    /// 兼容旧调用点：临时构造仅含一行的运行时资产。
    /// 该临时资产不保存到 Project，也不配置完成效果。
    /// </summary>
    public void ShowDialogue(string text)
    {
        DialogueData temp = ScriptableObject.CreateInstance<DialogueData>();
        temp.lines = new[] { new DialogueData.Line { text = text } };
        StartDialogue(temp);
    }

    /// <summary>
    /// 用当前行刷新名字、立绘、指示器并启动打字协程。
    /// 切换行前会停止旧协程，以避免其继续写入新行的 TMP 组件。
    /// </summary>
    private void ShowCurrentLine()
    {
        DialogueData.Line line = currentDialogue.lines[currentLineIndex];

        // 名字与正文均在展示时解析令牌，故同一资产可随剧情状态显示不同称谓。
        if (speakerNameText != null)
        {
            string name = TextTokens.Resolve(line.ResolveSpeakerName());
            bool hasName = !string.IsNullOrEmpty(name);
            speakerNameText.gameObject.SetActive(hasName);
            speakerNameText.text = hasName ? name : string.Empty;
        }

        // 立绘为空时主动隐藏 Image，避免上一行的图片残留。
        if (portraitImage != null)
        {
            CharacterData character = ResolveCharacter(line);
            Sprite portrait = character != null
                ? character.GetPortrait(line.expression)
                : null;
            portraitImage.sprite = portrait;
            portraitImage.gameObject.SetActive(portrait != null);
        }

        // 开始新行时隐藏提示，只有全文显示完才提示玩家继续。
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

        // 强制立即重建文本网格，否则 GetParsedText / textInfo 可能还是上一行的旧数据，
        // 导致 totalChars 偏小、打字机提前停下（文字只显示一半）。
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
