using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 管理对话显示、逐字播放和完成回调
public class DialogueUIManager : MonoBehaviour
{
    public static DialogueUIManager Instance { get; private set; }

    // 所有 UI 引用均需在 Inspector 配置；缺失引用会降级跳过对应显示
    [Header("对话框 UI")]
    [SerializeField] private GameObject dialoguePanel;
// 同步 DialogueUIManager 的相关数据
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
// 同步 DialogueUIManager 的相关数据（DialogueUIManager 后续步骤）
    [SerializeField] private CharacterData protagonistMale;
    [Tooltip("女主角（姐姐线）立绘资产，未设置 gender_female flag 时用男主角")]
// 同步 DialogueUIManager 的相关数据（DialogueUIManager 后续步骤）（29）
    [SerializeField] private CharacterData protagonistFemale;
    [Tooltip("触发主角立绘的说话人名字")]
// 同步 DialogueUIManager 的相关数据（DialogueUIManager 后续步骤）（32）
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
// 设置 DialogueUIManager 的配置数值
    [SerializeField] private float openSoundVolume = 1f;

// 配置 输入 分组
    [Header("输入")]
    [SerializeField] private KeyCode advanceKey = KeyCode.F;
// 设置 DialogueUIManager 的配置数值（DialogueUIManager 后续步骤）
    [SerializeField] private float reopenInputDelay = 0.1f;

    // 每段对话的运行时游标；不会写回 ScriptableObject 资产
    private DialogueData currentDialogue;
    private int currentLineIndex;
// 同步 DialogueUIManager 的相关数据（DialogueUIManager 后续步骤）（57）
    private Coroutine typingCoroutine;
    private bool isTyping;
    private Button clickAdvanceButton;
// 同步 DialogueUIManager 的相关数据（DialogueUIManager 后续步骤）（60）
    private Action onDialogueComplete;

    // 关闭时间配合冷却，避免推进最后一行的按键穿透到场景交互
    public bool IsDialogueOpen => dialoguePanel != null && dialoguePanel.activeSelf;
    public bool CanOpenDialogue => !IsDialogueOpen && Time.time >= LastClosedTime + reopenInputDelay;
// 推进 DialogueUIManager 的当前步骤
    public float LastClosedTime { get; private set; } = -999f;

    // 初始化组件引用和运行状态
    private void Awake()
    {
// 检查 Awake 的前置条件
        if (Instance != null && Instance != this)
        {
// 清理当前对象
            Destroy(gameObject);
            return;
        }

        // 重复实例自毁，不覆盖已有 Instance，避免场景切换时调用目标不稳定
        Instance = this;
        ResolveMissingReferences();
        EnsureClickAdvancer();
        HidePanel();
    }

    private void ResolveMissingReferences()
    {
        if (dialoguePanel == null)
        {
            Transform panel = FindInActiveScene("Dialogue");
            if (panel != null)
            {
                dialoguePanel = panel.gameObject;
            }
        }

        if (dialoguePanel == null)
        {
            return;
        }

        if (dialogueText == null)
        {
            dialogueText = FindChildComponent<TMP_Text>(dialoguePanel.transform, "Content");
        }

        if (speakerNameText == null)
        {
            speakerNameText = FindChildComponent<TMP_Text>(dialoguePanel.transform, "Name");
        }

        if (continueIndicator == null)
        {
            Transform indicator = FindChild(dialoguePanel.transform, "Triangle");
            if (indicator != null)
            {
                continueIndicator = indicator.gameObject;
            }
        }
    }

    private static Transform FindInActiveScene(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindChild(root.transform, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindChild(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChild(root.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static T FindChildComponent<T>(Transform root, string objectName) where T : Component
    {
        Transform target = FindChild(root, objectName);
        return target != null ? target.GetComponent<T>() : null;
    }

    private void OnDestroy()
    {
        if (clickAdvanceButton != null)
        {
            clickAdvanceButton.onClick.RemoveListener(HandleAdvanceInput);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void EnsureClickAdvancer()
    {
        if (dialoguePanel == null)
        {
            return;
        }

        clickAdvanceButton = dialoguePanel.GetComponent<Button>();
        if (clickAdvanceButton == null)
        {
            clickAdvanceButton = dialoguePanel.AddComponent<Button>();
        }

        clickAdvanceButton.transition = Selectable.Transition.None;
        clickAdvanceButton.onClick.RemoveListener(HandleAdvanceInput);
        clickAdvanceButton.onClick.AddListener(HandleAdvanceInput);
    }

    // 每帧检查输入与状态变化
    private void Update()
    {
// 检测按键输入
        if (!IsDialogueOpen || !Input.GetKeyDown(advanceKey))
        {
// 返回 Update 的处理结果
            return;
        }

// 检查 Update 的前置条件
        HandleAdvanceInput();
    }

    private void HandleAdvanceInput()
    {
        if (!IsDialogueOpen)
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
// 返回 StartDialogue 的处理结果
            return false;
        }

        if (dialoguePanel == null || dialogueText == null)
        {
// 输出调试信息
            Debug.LogError("DialogueUIManager 无法开始对白：dialoguePanel 或 dialogueText 未绑定。", this);
            return false;
        }

// 同步 StartDialogue 的状态
        currentDialogue = dialogue;
        currentLineIndex = 0;
// 同步 StartDialogue 的内部状态
        onDialogueComplete = onComplete;

        // 激活面板后再显示首行，确保 TMP 的可见字符数据可以正确计算
        dialoguePanel.SetActive(true);

// 检查 StartDialogue 的前置条件
        if (openSound != null && SfxManager.Instance != null)
        {
// 使用 StartDialogue 所需功能
            SfxManager.Instance.Play(openSound, openSoundVolume);
        }

        ShowCurrentLine();
// 返回 StartDialogue 的处理结果（StartDialogue）
        return true;
    }

// 显示 ShowDialogue 对应界面
    public void ShowDialogue(string text)
    {
        DialogueData temp = ScriptableObject.CreateInstance<DialogueData>();
// 同步 ShowDialogue 的内部状态
        temp.lines = new[] { new DialogueData.Line { text = text } };
        StartDialogue(temp);
    }

// 显示 ShowCurrentLine 对应界面
    private void ShowCurrentLine()
    {
// 保存 line 引用
        DialogueData.Line line = currentDialogue.lines[currentLineIndex];

        // 名字与正文均在展示时解析令牌，故同一资产可随剧情状态显示不同称谓
        if (speakerNameText != null)
        {
// 解析 ResolveSpeakerName 对应结果
            string name = TextTokens.Resolve(line.ResolveSpeakerName());
            bool hasName = !string.IsNullOrEmpty(name);
// 切换 ShowCurrentLine 的显示状态
            speakerNameText.gameObject.SetActive(hasName);
            speakerNameText.text = hasName ? name : string.Empty;
        }

        // 立绘为空时主动隐藏 Image，避免上一行的图片残留
        if (portraitImage != null)
        {
// 同步 ShowCurrentLine 的相关数据
            CharacterData character = ResolveCharacter(line);
            Sprite portrait = character != null
// 使用 ShowCurrentLine 所需功能
                ? character.GetPortrait(line.expression)
                : null;
// 更新显示图像
            portraitImage.sprite = portrait;
            portraitImage.gameObject.SetActive(portrait != null);
        }

        // 开始新行时隐藏提示，只有全文显示完才提示玩家继续
        if (continueIndicator != null)
        {
// 切换 ShowCurrentLine 的显示状态（ShowCurrentLine 后续步骤）
            continueIndicator.SetActive(false);
        }

// 检查 ShowCurrentLine 的前置条件
        if (typingCoroutine != null)
        {
// 推进 ShowCurrentLine 中的必要步骤
            StopCoroutine(typingCoroutine);
        }

// 启动当前协程
        typingCoroutine = StartCoroutine(TypeLine(TextTokens.Resolve(line.text)));
    }

// 解析 ResolveCharacter 对应结果
    private CharacterData ResolveCharacter(DialogueData.Line line)
    {
        if (line.character != null)
        {
// 返回 ResolveCharacter 的处理结果
            return line.character;
        }

// 检查 ResolveCharacter 的前置条件
        if (line.speakerName == protagonistSpeakerName)
        {
            bool female = GameManager.Instance != null
// 使用 ResolveCharacter 所需功能
                && GameManager.Instance.HasFlag(TextTokens.FemaleFlag);
            return female && protagonistFemale != null ? protagonistFemale : protagonistMale;
        }

// 返回 ResolveCharacter 的处理结果（ResolveCharacter）
        return null;
    }

// 处理 TypeLine 对应逻辑
    private IEnumerator TypeLine(string text)
    {
        isTyping = true;
// 更新 TypeLine 的界面文本
        dialogueText.text = text;
        dialogueText.maxVisibleCharacters = 0;

// 使用 TypeLine 所需功能
        dialogueText.ForceMeshUpdate();
        int totalChars = dialogueText.textInfo.characterCount;

        // 用 maxVisibleCharacters 而非逐字拼接，避免富文本标签被截断
        float visibleCount = 0f;

// 等待条件变化
        while (dialogueText.maxVisibleCharacters < totalChars)
        {
// 推进 TypeLine 的当前步骤
            visibleCount += charsPerSecond * Time.deltaTime;
            dialogueText.maxVisibleCharacters = Mathf.Min(totalChars, Mathf.FloorToInt(visibleCount));
// 等待下一步
            yield return null;
        }

// 推进 TypeLine 中的必要步骤
        FinishTyping();
    }

// 处理 SkipTyping 对应逻辑
    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
// 推进 SkipTyping 中的必要步骤
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

// 同步 SkipTyping 的内部状态
        dialogueText.maxVisibleCharacters = int.MaxValue;
        FinishTyping();
    }

// 处理 FinishTyping 对应逻辑
    private void FinishTyping()
    {
// 同步 FinishTyping 的内部状态
        isTyping = false;
        typingCoroutine = null;

// 检查 FinishTyping 的前置条件
        if (continueIndicator != null)
        {
// 切换 FinishTyping 的显示状态
            continueIndicator.SetActive(true);
        }
    }

// 处理 AdvanceLine 对应逻辑
    private void AdvanceLine()
    {
// 推进 AdvanceLine 的当前步骤
        currentLineIndex++;

        if (currentLineIndex < currentDialogue.lines.Length)
        {
// 推进 AdvanceLine 中的必要步骤
            ShowCurrentLine();
        }
        else
        {
// 推进 AdvanceLine 中的必要步骤（AdvanceLine）
            EndDialogue();
        }
    }

// 处理 EndDialogue 对应逻辑
    private void EndDialogue()
    {
        DialogueData finished = currentDialogue;
// 同步 EndDialogue 的相关数据
        Action callback = onDialogueComplete;

// 同步 EndDialogue 的内部状态
        currentDialogue = null;
        onDialogueComplete = null;
// 推进 EndDialogue 中的必要步骤
        HidePanel();

// 使用 EndDialogue 所需功能
        finished.ApplyCompletionEffects();
        callback?.Invoke();
    }

// 隐藏 HidePanel 对应界面
    private void HidePanel()
    {
// 检查 HidePanel 的前置条件
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

// 同步 HidePanel 的内部状态
        LastClosedTime = Time.time;
    }
}
