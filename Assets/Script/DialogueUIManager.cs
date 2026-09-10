using System;
using System.Collections;
using TMPro;
using UnityEngine;

// 定义 DialogueUIManager 类型
public class DialogueUIManager : MonoBehaviour
{
    public static DialogueUIManager Instance { get; private set; }

    // 所有 UI 引用均需在 Inspector 配置；缺失引用会降级跳过对应显示
    [Header("对话框 UI")]
    [SerializeField] private GameObject dialoguePanel;
// 保存 dialogueText 数据
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text speakerNameText;
// 保存 continueIndicator 引用
    [SerializeField] private GameObject continueIndicator;

// 配置 立绘（对话框左侧） 分组
    [Header("立绘（对话框左侧）")]
    [SerializeField] private UnityEngine.UI.Image portraitImage;

// 配置 主角立绘（说话人为「我」时自动使用） 分组
    [Header("主角立绘（说话人为「我」时自动使用）")]
    [Tooltip("男主角（哥哥线）立绘资产")]
// 保存 protagonistMale 数据
    [SerializeField] private CharacterData protagonistMale;
    [Tooltip("女主角（姐姐线）立绘资产，未设置 gender_female flag 时用男主角")]
// 保存 protagonistFemale 数据
    [SerializeField] private CharacterData protagonistFemale;
    [Tooltip("触发主角立绘的说话人名字")]
// 保存 protagonistSpeakerName 数据
    [SerializeField] private string protagonistSpeakerName = "我";

// 配置 打字机 分组
    [Header("打字机")]
    [SerializeField] private float charsPerSecond = 30f;

// 配置 音效 分组
    [Header("音效")]
    [Tooltip("对话框弹出时播放一次的音效（不循环）")]
// 保存 openSound 引用
    [SerializeField] private AudioClip openSound;
    [Range(0f, 1f)]
// 配置 openSoundVolume 数值
    [SerializeField] private float openSoundVolume = 1f;

// 配置 输入 分组
    [Header("输入")]
    [SerializeField] private KeyCode advanceKey = KeyCode.F;
// 配置 reopenInputDelay 数值
    [SerializeField] private float reopenInputDelay = 0.1f;

    // 每段对话的运行时游标；不会写回 ScriptableObject 资产
    private DialogueData currentDialogue;
    private int currentLineIndex;
// 保存 typingCoroutine 数据
    private Coroutine typingCoroutine;
    private bool isTyping;
// 保存 onDialogueComplete 数据
    private Action onDialogueComplete;

    // 关闭时间配合冷却，避免推进最后一行的按键穿透到场景交互
    public bool IsDialogueOpen => dialoguePanel != null && dialoguePanel.activeSelf;
    public bool CanOpenDialogue => !IsDialogueOpen && Time.time >= LastClosedTime + reopenInputDelay;
// 更新当前逻辑
    public float LastClosedTime { get; private set; } = -999f;

    // 单例初始化发生在 Start 前，供同帧初始化的交互组件查询
    private void Awake()
    {
// 判断当前条件
        if (Instance != null && Instance != this)
        {
// 清理当前对象
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
// 检测按键输入
        if (!IsDialogueOpen || !Input.GetKeyDown(advanceKey))
        {
// 返回当前结果
            return;
        }

// 判断当前条件
        if (isTyping)
        {
// 执行 SkipTyping
            SkipTyping();
        }
// 处理其他分支
        else
        {
// 执行 AdvanceLine
            AdvanceLine();
        }
    }

    public bool StartDialogue(DialogueData dialogue, Action onComplete = null)
    {
        // 空对话或缺少必要 UI 时明确报告失败，避免调用方误认为对白已完成
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0)
        {
// 返回当前结果
            return false;
        }

        if (dialoguePanel == null || dialogueText == null)
        {
// 输出调试信息
            Debug.LogError("DialogueUIManager 无法开始对白：dialoguePanel 或 dialogueText 未绑定。", this);
            return false;
        }

// 更新当前状态
        currentDialogue = dialogue;
        currentLineIndex = 0;
// 更新当前状态
        onDialogueComplete = onComplete;

        // 激活面板后再显示首行，确保 TMP 的可见字符数据可以正确计算
        dialoguePanel.SetActive(true);

// 判断当前条件
        if (openSound != null && SfxManager.Instance != null)
        {
// 调用 Play
            SfxManager.Instance.Play(openSound, openSoundVolume);
        }

        ShowCurrentLine();
// 返回当前结果
        return true;
    }

// 定义 ShowDialogue 方法
    public void ShowDialogue(string text)
    {
        DialogueData temp = ScriptableObject.CreateInstance<DialogueData>();
// 更新当前状态
        temp.lines = new[] { new DialogueData.Line { text = text } };
        StartDialogue(temp);
    }

// 定义 ShowCurrentLine 方法
    private void ShowCurrentLine()
    {
// 保存 line 引用
        DialogueData.Line line = currentDialogue.lines[currentLineIndex];

        // 名字与正文均在展示时解析令牌，故同一资产可随剧情状态显示不同称谓
        if (speakerNameText != null)
        {
// 定义 ResolveSpeakerName 方法
            string name = TextTokens.Resolve(line.ResolveSpeakerName());
            bool hasName = !string.IsNullOrEmpty(name);
// 切换显示状态
            speakerNameText.gameObject.SetActive(hasName);
            speakerNameText.text = hasName ? name : string.Empty;
        }

        // 立绘为空时主动隐藏 Image，避免上一行的图片残留
        if (portraitImage != null)
        {
// 保存 character 数据
            CharacterData character = ResolveCharacter(line);
            Sprite portrait = character != null
// 调用 GetPortrait
                ? character.GetPortrait(line.expression)
                : null;
// 更新显示图像
            portraitImage.sprite = portrait;
            portraitImage.gameObject.SetActive(portrait != null);
        }

        // 开始新行时隐藏提示，只有全文显示完才提示玩家继续
        if (continueIndicator != null)
        {
// 切换显示状态
            continueIndicator.SetActive(false);
        }

// 判断当前条件
        if (typingCoroutine != null)
        {
// 执行 StopCoroutine
            StopCoroutine(typingCoroutine);
        }

// 启动当前协程
        typingCoroutine = StartCoroutine(TypeLine(TextTokens.Resolve(line.text)));
    }

// 定义 ResolveCharacter 方法
    private CharacterData ResolveCharacter(DialogueData.Line line)
    {
        if (line.character != null)
        {
// 返回当前结果
            return line.character;
        }

// 判断当前条件
        if (line.speakerName == protagonistSpeakerName)
        {
            bool female = GameManager.Instance != null
// 调用 HasFlag
                && GameManager.Instance.HasFlag(TextTokens.FemaleFlag);
            return female && protagonistFemale != null ? protagonistFemale : protagonistMale;
        }

// 返回当前结果
        return null;
    }

// 定义 TypeLine 方法
    private IEnumerator TypeLine(string text)
    {
        isTyping = true;
// 更新界面文本
        dialogueText.text = text;
        dialogueText.maxVisibleCharacters = 0;

// 调用 ForceMeshUpdate
        dialogueText.ForceMeshUpdate();
        int totalChars = dialogueText.textInfo.characterCount;

        // 用 maxVisibleCharacters 而非逐字拼接，避免富文本标签被截断
        float visibleCount = 0f;

// 等待条件变化
        while (dialogueText.maxVisibleCharacters < totalChars)
        {
// 更新当前逻辑
            visibleCount += charsPerSecond * Time.deltaTime;
            dialogueText.maxVisibleCharacters = Mathf.Min(totalChars, Mathf.FloorToInt(visibleCount));
// 等待下一步
            yield return null;
        }

// 执行 FinishTyping
        FinishTyping();
    }

// 定义 SkipTyping 方法
    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
// 执行 StopCoroutine
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

// 更新当前状态
        dialogueText.maxVisibleCharacters = int.MaxValue;
        FinishTyping();
    }

// 定义 FinishTyping 方法
    private void FinishTyping()
    {
// 更新当前状态
        isTyping = false;
        typingCoroutine = null;

// 判断当前条件
        if (continueIndicator != null)
        {
// 切换显示状态
            continueIndicator.SetActive(true);
        }
    }

// 定义 AdvanceLine 方法
    private void AdvanceLine()
    {
// 更新当前逻辑
        currentLineIndex++;

        if (currentLineIndex < currentDialogue.lines.Length)
        {
// 执行 ShowCurrentLine
            ShowCurrentLine();
        }
        else
        {
// 执行 EndDialogue
            EndDialogue();
        }
    }

// 定义 EndDialogue 方法
    private void EndDialogue()
    {
        DialogueData finished = currentDialogue;
// 保存 callback 数据
        Action callback = onDialogueComplete;

// 更新当前状态
        currentDialogue = null;
        onDialogueComplete = null;
// 执行 HidePanel
        HidePanel();

// 调用 ApplyCompletionEffects
        finished.ApplyCompletionEffects();
        callback?.Invoke();
    }

// 定义 HidePanel 方法
    private void HidePanel()
    {
// 判断当前条件
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

// 更新当前状态
        LastClosedTime = Time.time;
    }
}
