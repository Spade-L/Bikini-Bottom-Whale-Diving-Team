using System;
using System.Collections;
using UnityEngine;

// 使用 当前脚本 所需功能
[RequireComponent(typeof(BoxCollider2D))]
public class Level3DoorSequence : MonoBehaviour, IInteractionPromptSource
{
// 推进 Level3DoorSequence 的当前步骤
    [Serializable]
    private class SequenceStep
    {
// 同步 SequenceStep 的相关数据
        public string label;
        public GameObject root;
// 同步 SequenceStep 的相关数据（SequenceStep 后续步骤）
        public Animator animator;
        public string stateName;
// 记录 SequenceStep 的当前状态
        public bool showResultObjectsDuringStep;
        public bool hideRootAfterStep = true;
// 设置 SequenceStep 的配置数值
        [Min(0.01f)] public float duration = 1f;
    }

// 配置 演出步骤（严格为 sps0 → sps） 分组
    [Header("演出步骤（严格为 sps0 → sps）")]
    [SerializeField] private SequenceStep[] steps;

// 配置 六个便利店调查条件 分组
    [Header("六个便利店调查条件")]
    [Tooltip("填写 ClueData.ClueId；组件检查 investigated_<id> Flag，不改变核心线索列表。")]
// 同步 SequenceStep 的相关数据（SequenceStep 后续步骤）（32）
    [SerializeField] private string[] requiredInvestigationIds =
    {
// 推进 SequenceStep 的当前步骤
        "store_note",
        "store_poster",
// 推进 SequenceStep 的当前步骤（SequenceStep）
        "store_vegetables",
        "store_handprint",
// 推进 SequenceStep 的当前步骤（SequenceStep）（store_fruit）
        "store_fruit",
        "store_toy"
// 推进 SequenceStep 的当前步骤（SequenceStep）（Header）
    };

// 配置 演出对象 分组
    [Header("演出对象")]
    [SerializeField] private GameObject hand;
// 保存 eye 引用
    [SerializeField] private GameObject eye;
    [SerializeField] private GameObject redToyGameObject;
// 保存 sequenceOnlyObjects 引用
    [SerializeField] private GameObject[] sequenceOnlyObjects;

// 配置 演出对白 分组
    [Header("演出对白")]
    [SerializeField] private DialogueData revealDialogue;
// 保存 missingToyDialogue 引用
    [SerializeField] private DialogueData missingToyDialogue;
    [SerializeField] private DialogueData deliveryDialogue;
// 保存 disappearanceDialogue 引用
    [SerializeField] private DialogueData disappearanceDialogue;
    [SerializeField] private DialogueData exitReadyDialogue;

// 配置 音效（没有匹配资产时保持为空） 分组
    [Header("音效（没有匹配资产时保持为空）")]
    [SerializeField] private AudioClip fallingBreakingSound;
// 限制当前数值范围
    [Range(0f, 1f)] [SerializeField] private float fallingBreakingVolume = 1f;

// 配置 红色身影 BGM 分组
    [Header("红色身影 BGM")]
    [Tooltip("危险、尖锐、急促脉冲、超自然")]
    [SerializeField] private AudioClip redFigureBgm;
    [Range(0f, 1f)] [SerializeField] private float redFigureBgmVolume = 0.9f;
    [Min(0f)] [SerializeField] private float redFigureBgmFade = 0.5f;

// 配置 存档 Flag 分组
    [Header("存档 Flag")]
    [SerializeField] private string completedFlag = "level3_door_sequence_done";
// 同步 SequenceStep 的相关数据（SequenceStep 后续步骤）（82）
    [SerializeField] private string resolvedFlag = "level3_store_shadow_resolved";
    [SerializeField] private string toyDeliveredFlag = "level3_store_toy_delivered";
// 同步 SequenceStep 的相关数据（SequenceStep 后续步骤）（85）
    [SerializeField] private string redToyPickupFlag = "picked_store_red_toy";

// 配置 触发与交互 分组
    [Header("触发与交互")]
    [SerializeField] private GameObject interactionUI;
// 同步 SequenceStep 的相关数据（SequenceStep 后续步骤）（91）
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactionKey = KeyCode.F;

// 同步 SequenceStep 的相关数据（SequenceStep 后续步骤）（95）
    private Coroutine sequenceCoroutine;
    private IDisposable movementLease;
// 同步 SequenceStep 的相关数据（SequenceStep 后续步骤）（98）
    private IDisposable interactionLease;
    private bool playerInRange;
// 记录 SequenceStep 的当前状态（SequenceStep 后续步骤）
    private bool inputSuppressed;
    private bool subscribedToGameManager;
// 记录 SequenceStep 的当前状态（SequenceStep 后续步骤）（104）
    private bool completed;
    private bool resolved;
// 记录 SequenceStep 的当前状态（SequenceStep 后续步骤）（107）
    private bool exclusiveModeActive;
    private bool sequencePriorityActive;
// 记录红色身影 BGM 是否占用中
    private bool redFigureBgmActive;

// 推进 SequenceStep 的当前步骤（SequenceStep）（public）
    public bool IsInteractionPromptEligible
    {
// 推进 SequenceStep 的当前步骤（SequenceStep）（get）
        get
        {
// 检查 SequenceStep 的前置条件
            if (!isActiveAndEnabled || !playerInRange || sequenceCoroutine != null
                || inputSuppressed || resolved)
            {
// 返回 SequenceStep 的处理结果
                return false;
            }

// 在 SequenceStep 中处理 返回 SequenceStep 的处理结果
            return !completed ? HasAllInvestigations() : !resolved;
        }
    }

// 处理 InvestigationFlag 对应逻辑
    private string InvestigationFlag(string clueId) => $"investigated_{clueId}";

// 初始化组件引用和运行状态
    private void Awake()
    {
// 获取 Awake 的组件引用
        GetComponent<BoxCollider2D>().isTrigger = true;
        ApplySavedState();
// 推进 Awake 中的必要步骤
        HidePrompt();
    }

// 读取初始依赖并同步首帧状态
    private void Start()
    {
// 推进 Start 中的必要步骤
        ApplySavedState();
    }

// 启用时订阅事件并恢复状态
    private void OnEnable()
    {
// 推进 OnEnable 中的必要步骤
        ApplySavedState();
        SubscribeGameManager(true);
    }

// 禁用时取消订阅并清理临时状态
    private void OnDisable()
    {
// 使用 OnDisable 所需功能
        PlayerInteractionPromptController.UnregisterSource(this);
        SubscribeGameManager(false);
// 推进 OnDisable 中的必要步骤
        InterruptSequence();
        CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
// 使用 OnDisable 所需功能（OnDisable）
        CluePickup2D.ClearExclusiveInteractionTarget(redToyGameObject);
        sequencePriorityActive = false;
// 推进 OnDisable 中的必要步骤（OnDisable）
        HidePrompt();
    }

// 销毁时释放事件订阅和静态引用
    private void OnDestroy()
    {
// 使用 OnDestroy 所需功能
        PlayerInteractionPromptController.UnregisterSource(this);
        SubscribeGameManager(false);
// 推进 OnDestroy 中的必要步骤
        InterruptSequence();
        CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
// 使用 OnDestroy 所需功能（OnDestroy）
        CluePickup2D.ClearExclusiveInteractionTarget(redToyGameObject);
        sequencePriorityActive = false;
    }

// 每帧检查输入与状态变化
    private void Update()
    {
// 推进 Update 中的必要步骤
        UpdateExclusiveInteractionMode();

// 检查 Update 的前置条件
        if (!subscribedToGameManager)
        {
// 推进 Update 中的必要步骤（Update）
            SubscribeGameManager(true);
        }

// 检查 Update 的前置条件（Update）
        if (GameplayInputLock.IsInteractionLocked)
        {
// 同步 Update 的状态
            inputSuppressed = true;
            HidePrompt();
// 返回 Update 的处理结果
            return;
        }

// 检查 Update 的前置条件（Update）（if）
        if (inputSuppressed)
        {
// 同步 Update 的内部状态
            inputSuppressed = false;
            RefreshPrompt();
        }

// 检测按键输入
        if (!playerInRange || sequenceCoroutine != null || !Input.GetKeyDown(interactionKey)
            || (DialogueUIManager.Instance != null && !DialogueUIManager.Instance.CanOpenDialogue))
        {
// 返回 Update 的处理结果（Update）
            return;
        }

// 在 Update 中继续当前处理
        if (resolved)
        {
// 使用 Update 所需功能
            CluePickup2D.ClearExclusiveInteractionTarget(redToyGameObject);
            return;
        }

// 在 Update 中继续当前处理（Update 后续步骤）
        if (!completed)
        {
// 在 Update 中继续当前处理（Update 后续步骤）（240）
            if (HasAllInvestigations())
            {
// 启动当前协程
                sequenceCoroutine = StartCoroutine(PlaySequence());
            }

// 返回 Update 的处理结果（Update）（return）
            return;
        }

// 在 Update 中继续当前处理（Update 后续步骤）（251）
        if (!HasRedToyPickup())
        {
// 使用 Update 所需功能（Update）
            CluePickup2D.SetExclusiveInteractionTarget(redToyGameObject);
            sequenceCoroutine = StartCoroutine(RemindMissingToy());
// 在 Update 中继续当前处理（Update 后续步骤）（257）
            return;
        }

// 在 Update 中处理 ClearExclusiveInteractionTarget
        CluePickup2D.ClearExclusiveInteractionTarget(redToyGameObject);
        sequenceCoroutine = StartCoroutine(DeliverToy());
    }

// 玩家进入范围后登记可交互状态
    private void OnTriggerEnter2D(Collider2D other)
    {
// 检查 OnTriggerEnter2D 的前置条件
        if (other.CompareTag(playerTag))
        {
// 同步 OnTriggerEnter2D 的内部状态
            playerInRange = true;
            PlayerInteractionPromptController.RegisterSource(this);
// 使用 OnTriggerEnter2D 所需功能
            PlayerInteractionPromptController.RefreshSource(this);
        }
    }

// 玩家离开范围后移除可交互状态
    private void OnTriggerExit2D(Collider2D other)
    {
// 检查 OnTriggerExit2D 的前置条件
        if (other.CompareTag(playerTag))
        {
// 同步 OnTriggerExit2D 的内部状态
            playerInRange = false;
            PlayerInteractionPromptController.UnregisterSource(this);
        }
    }

// 按步骤播放门前黑影完整演出
    private IEnumerator PlaySequence()
    {
// 推进 PlaySequence 中的必要步骤
        HidePrompt();

// 检查 PlaySequence 的前置条件
        if (!HasAllInvestigations() || !HasValidSteps())
        {
// 检查 PlaySequence 的前置条件（PlaySequence）
            if (!HasValidSteps())
            {
// 输出调试信息
                Debug.LogError("[Level3DoorSequence] 必须按顺序配置且仅配置 sps0、sps 两个有效步骤，演出未启动。", this);
            }

// 同步 PlaySequence 的内部状态
            sequenceCoroutine = null;
            RefreshPrompt();
// 无法继续时结束 PlaySequence
            yield break;
        }

// 同步 PlaySequence 的内部状态（PlaySequence）
        movementLease = GameplayInputLock.AcquireMovementLock();
        interactionLease = GameplayInputLock.AcquireInteractionLock();
// 推进 PlaySequence 中的必要步骤（PlaySequence）
        SetAllStepsVisible(false);
        SetSequenceOnlyObjectsVisible(false);
// 在 PlaySequence 中处理 SetResultObjectsVisible
        SetResultObjectsVisible(false);

// 遍历全部元素
        foreach (SequenceStep step in steps)
        {
// PlaySequence 缺少引用时提前结束
            if (step == null)
            {
// 推进 PlaySequence 的当前步骤
                continue;
            }

// 检查 PlaySequence 的前置条件（PlaySequence）（if）
// 红色身影出现时暂停普通 BGM
            if (string.Equals(step.label, "sps", StringComparison.Ordinal)) StartRedFigureBgm();

            if (step.root != null)
            {
// 切换 PlaySequence 的显示状态
                step.root.SetActive(true);
            }

// 在 PlaySequence 中继续当前处理
            if (step.showResultObjectsDuringStep)
            {
// 在 PlaySequence 中处理 SetResultObjectsVisible（PlaySequence 后续步骤）
                SetResultObjectsVisible(true);
            }

// 在 PlaySequence 中继续当前处理（PlaySequence 后续步骤）
            if (step.animator != null && !string.IsNullOrEmpty(step.stateName))
            {
// 使用 PlaySequence 所需功能
                step.animator.Play(step.stateName, 0, 0f);
                step.animator.Update(0f);
            }

// 等待下一步
            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, step.duration));

// 在 PlaySequence 中继续当前处理（PlaySequence 后续步骤）（362）
            if (step.hideRootAfterStep && step.root != null)
            {
// 切换 PlaySequence 的显示状态（PlaySequence 后续步骤）
                step.root.SetActive(false);
            }

// 在 PlaySequence 中继续当前处理（PlaySequence 后续步骤）（369）
            if (step.showResultObjectsDuringStep)
            {
// 在 PlaySequence 中处理 SetResultObjectsVisible（PlaySequence 后续步骤）（后续处理 2）
                SetResultObjectsVisible(false);
            }
        }

// 在 PlaySequence 中处理 SetResultObjectsVisible（PlaySequence 后续步骤）（后续处理 3）
        SetResultObjectsVisible(true);
        completed = true;
// 在 PlaySequence 中处理 SetSequenceOnlyObjectsVisible
        SetSequenceOnlyObjectsVisible(false);
        SetStepRootVisible(0, false);
// 在 PlaySequence 中处理 SetStepRootVisible
        SetStepRootVisible(1, true);
        GameManager.Instance?.SetFlag(completedFlag);

// 在 PlaySequence 中继续当前处理（PlaySequence 后续步骤）（387）
        yield return StartDialogueAndWait(revealDialogue);
        StopRedFigureBgm();

// 在 PlaySequence 中处理 ReleaseLocks
        ReleaseLocks();
        sequenceCoroutine = null;
// 在 PlaySequence 中处理 RefreshPrompt
        RefreshPrompt();
    }

// 提示玩家尚未取得红色玩具
    private IEnumerator RemindMissingToy()
    {
// 推进 RemindMissingToy 中的必要步骤
        HidePrompt();
        movementLease = GameplayInputLock.AcquireMovementLock();
// 同步 RemindMissingToy 的内部状态
        interactionLease = GameplayInputLock.AcquireInteractionLock();

// 在 RemindMissingToy 中继续当前处理
        yield return StartDialogueAndWait(missingToyDialogue);

// 推进 RemindMissingToy 中的必要步骤（RemindMissingToy）
        ReleaseLocks();
        sequenceCoroutine = null;
// 在 RemindMissingToy 中处理 RefreshPrompt
        RefreshPrompt();
    }

// 播放交付玩具与身影消失流程
    private IEnumerator DeliverToy()
    {
// 推进 DeliverToy 中的必要步骤
        HidePrompt();
        movementLease = GameplayInputLock.AcquireMovementLock();
// 同步 DeliverToy 的内部状态
        interactionLease = GameplayInputLock.AcquireInteractionLock();

// 在 DeliverToy 中继续当前处理
        yield return StartDialogueAndWait(deliveryDialogue);
        GameManager.Instance?.SetFlag(toyDeliveredFlag);
// 在 DeliverToy 中继续当前处理（DeliverToy 后续步骤）
        yield return StartDialogueAndWait(disappearanceDialogue);

// 检查 DeliverToy 的前置条件
        if (SfxManager.Instance != null && fallingBreakingSound != null)
        {
// 使用 DeliverToy 所需功能
            SfxManager.Instance.Play(fallingBreakingSound, fallingBreakingVolume);
        }

// 推进 DeliverToy 中的必要步骤（DeliverToy）
        SetResultObjectsVisible(false);
        SetStepRootVisible(1, false);
// 在 DeliverToy 中继续当前处理（DeliverToy 后续步骤）（442）
        yield return StartDialogueAndWait(exitReadyDialogue);

// 更新剧情标记
        GameManager.Instance?.SetFlag(resolvedFlag);
        resolved = true;
// 在 DeliverToy 中处理 ReleaseLocks
        ReleaseLocks();
        sequenceCoroutine = null;
// 在 DeliverToy 中处理 RefreshPrompt
        RefreshPrompt();
    }

// 启动对白并等待整段播放完成
    private IEnumerator StartDialogueAndWait(DialogueData dialogue)
    {
// 缺少必要引用时退出 StartDialogueAndWait
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0
            || DialogueUIManager.Instance == null)
        {
// 无法继续时结束 StartDialogueAndWait
            yield break;
        }

// 记录 StartDialogueAndWait 的当前状态
        bool finished = false;
        DialogueUIManager.Instance.StartDialogue(dialogue, () => finished = true);
// 等待条件变化
        while (!finished)
        {
// 在 StartDialogueAndWait 中继续当前处理
            yield return null;
        }
    }

// 判断 HasAllInvestigations 对应条件
    private bool HasAllInvestigations()
    {
// 缺少必要引用时退出 HasAllInvestigations
        if (GameManager.Instance == null || requiredInvestigationIds == null || requiredInvestigationIds.Length == 0)
        {
// 返回 HasAllInvestigations 的处理结果
            return false;
        }

// 在 HasAllInvestigations 中继续当前处理
        foreach (string clueId in requiredInvestigationIds)
        {
// 检查 HasAllInvestigations 的前置条件
            if (string.IsNullOrEmpty(clueId) || !GameManager.Instance.HasFlag(InvestigationFlag(clueId)))
            {
// 返回 HasAllInvestigations 的处理结果（HasAllInvestigations）
                return false;
            }
        }

// 返回 HasAllInvestigations 的处理结果（HasAllInvestigations）（return）
        return true;
    }

// 判断 HasRedToyPickup 对应条件
    private bool HasRedToyPickup()
    {
// 返回 HasRedToyPickup 的处理结果
        return GameManager.Instance != null
            && !string.IsNullOrEmpty(redToyPickupFlag)
// 使用 HasRedToyPickup 所需功能
            && GameManager.Instance.HasFlag(redToyPickupFlag);
    }

// 判断 HasValidSteps 对应条件
    private bool HasValidSteps()
    {
// 缺少必要引用时退出 HasValidSteps
        if (steps == null || steps.Length != 2)
        {
// 返回 HasValidSteps 的处理结果
            return false;
        }

// 循环处理当前集合
        for (int i = 0; i < steps.Length; i++)
        {
// 同步 HasValidSteps 的相关数据
            SequenceStep step = steps[i];
            string expectedLabel = i == 0 ? "sps0" : "sps";
// 缺少必要引用时退出 HasValidSteps（HasValidSteps）
            if (step == null || !string.Equals(step.label, expectedLabel, StringComparison.Ordinal)
                || step.root == null || step.duration < 0.01f)
            {
// 返回 HasValidSteps 的处理结果（HasValidSteps）
                return false;
            }
        }

// 返回 HasValidSteps 的处理结果（HasValidSteps）（return）
        return true;
    }

// 按存档标记重建当前场景状态
    private void ApplySavedState()
    {
// 同步 ApplySavedState 的相关数据
        GameManager gm = GameManager.Instance;
        completed = gm != null && gm.HasFlag(completedFlag);
// 同步 ApplySavedState 的内部状态
        resolved = gm != null && gm.HasFlag(resolvedFlag);

// 推进 ApplySavedState 中的必要步骤
        SetAllStepsVisible(false);
        SetSequenceOnlyObjectsVisible(false);
// 推进 ApplySavedState 中的必要步骤（ApplySavedState）
        SetResultObjectsVisible(completed && !resolved);
        SetStepRootVisible(0, false);
// 在 ApplySavedState 中处理 SetStepRootVisible
        SetStepRootVisible(1, completed && !resolved);
        RefreshPrompt();
    }

// 处理 SubscribeGameManager 对应逻辑
    private void SubscribeGameManager(bool subscribe)
    {
// 同步 SubscribeGameManager 的相关数据
        GameManager gm = GameManager.Instance;
        if (gm == null || subscribedToGameManager == subscribe)
        {
// 返回 SubscribeGameManager 的处理结果
            return;
        }

// 检查 SubscribeGameManager 的前置条件
        if (subscribe)
        {
// 推进 SubscribeGameManager 的当前步骤
            gm.OnFlagsChanged += HandleFlagsChanged;
        }
// 处理 SubscribeGameManager 的备用分支
        else
        {
// 推进 SubscribeGameManager 的当前步骤（SubscribeGameManager）
            gm.OnFlagsChanged -= HandleFlagsChanged;
        }

// 同步 SubscribeGameManager 的内部状态
        subscribedToGameManager = subscribe;
    }

// 剧情标记变化后同步当前界面与交互
    private void HandleFlagsChanged()
    {
// 推进 HandleFlagsChanged 中的必要步骤
        ApplySavedState();
        UpdateExclusiveInteractionMode();
// 使用 HandleFlagsChanged 所需功能
        PlayerInteractionPromptController.RefreshSource(this);
    }

// 刷新 UpdateExclusiveInteractionMode 对应状态
    private void UpdateExclusiveInteractionMode()
    {
// 判断 HasAllInvestigations 对应条件（UpdateExclusiveInteractionMode）
        bool shouldPrioritizeSequence = !completed && playerInRange && HasAllInvestigations();
        if (sequencePriorityActive != shouldPrioritizeSequence)
        {
// 同步 UpdateExclusiveInteractionMode 的内部状态
            sequencePriorityActive = shouldPrioritizeSequence;
            if (shouldPrioritizeSequence)
            {
// 使用 UpdateExclusiveInteractionMode 所需功能
                CluePickup2D.SetExclusiveInteractionTarget(gameObject);
            }
// 处理 UpdateExclusiveInteractionMode 的备用分支
            else
            {
// 使用 UpdateExclusiveInteractionMode 所需功能（UpdateExclusiveInteractionMode）
                CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
            }
        }

// 判断 HasRedToyPickup 对应条件（UpdateExclusiveInteractionMode）
        bool shouldBeActive = redToyGameObject != null && completed && !resolved && !HasRedToyPickup();
        if (exclusiveModeActive == shouldBeActive)
        {
// 返回 UpdateExclusiveInteractionMode 的处理结果
            return;
        }

// 同步 UpdateExclusiveInteractionMode 的内部状态（UpdateExclusiveInteractionMode）
        exclusiveModeActive = shouldBeActive;
        if (shouldBeActive)
        {
// 在 UpdateExclusiveInteractionMode 中处理 SetExclusiveInteractionTarget
            CluePickup2D.SetExclusiveInteractionTarget(redToyGameObject);
        }
// 处理 UpdateExclusiveInteractionMode 的备用分支（UpdateExclusiveInteractionMode）
        else
        {
// 在 UpdateExclusiveInteractionMode 中处理 ClearExclusiveInteractionTarget
            CluePickup2D.ClearExclusiveInteractionTarget(redToyGameObject);
        }
    }

// 刷新 RefreshPrompt 对应状态
    private void RefreshPrompt()
    {
// 使用 RefreshPrompt 所需功能
        PlayerInteractionPromptController.RefreshSource(this);
    }

// 隐藏 HidePrompt 对应界面
    private void HidePrompt()
    {
// 使用 HidePrompt 所需功能
        PlayerInteractionPromptController.RefreshSource(this);
    }

// 处理 StartRedFigureBgm 对应逻辑
    private void StartRedFigureBgm()
    {
// 缺少必要引用时退出 StartRedFigureBgm
        if (redFigureBgmActive || redFigureBgm == null || MusicManager.Instance == null) return;
// 暂停普通 BGM 并循环播放红色身影音乐
        MusicManager.Instance.PlayInterruptingBgm(this, redFigureBgm, redFigureBgmVolume, redFigureBgmFade);
        redFigureBgmActive = true;
    }

// 停止 StopRedFigureBgm 对应流程
    private void StopRedFigureBgm()
    {
// 缺少必要引用时退出 StopRedFigureBgm
        if (!redFigureBgmActive) return;
// 从原进度恢复普通 BGM
        MusicManager.Instance?.StopInterruptingBgm(this, redFigureBgmFade);
        redFigureBgmActive = false;
    }

// 中断演出并释放全部输入锁
    private void InterruptSequence()
    {
        StopRedFigureBgm();
// 检查 InterruptSequence 的前置条件
        if (sequenceCoroutine != null)
        {
// 推进 InterruptSequence 中的必要步骤
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

// 推进 InterruptSequence 中的必要步骤（InterruptSequence）
        ReleaseLocks();
        if (!completed || resolved)
        {
// 在 InterruptSequence 中处理 SetAllStepsVisible
            SetAllStepsVisible(false);
            SetSequenceOnlyObjectsVisible(false);
        }

// 检查 InterruptSequence 的前置条件（InterruptSequence）
        if (resolved)
        {
// 在 InterruptSequence 中处理 SetResultObjectsVisible
            SetResultObjectsVisible(false);
        }
    }

// 设置 SetStepRootVisible 的目标状态
    private void SetStepRootVisible(int index, bool visible)
    {
// 缺少必要引用时退出 SetStepRootVisible
        if (steps == null || index < 0 || index >= steps.Length || steps[index] == null
            || steps[index].root == null)
        {
// 返回 SetStepRootVisible 的处理结果
            return;
        }

// 切换 SetStepRootVisible 的显示状态
        steps[index].root.SetActive(visible);
    }

// 设置 SetAllStepsVisible 的目标状态
    private void SetAllStepsVisible(bool visible)
    {
// 缺少必要引用时退出 SetAllStepsVisible
        if (steps == null) return;
        foreach (SequenceStep step in steps)
        {
// 检查 SetAllStepsVisible 的前置条件
            if (step != null && step.root != null) step.root.SetActive(visible);
        }
    }

// 设置 SetSequenceOnlyObjectsVisible 的目标状态
    private void SetSequenceOnlyObjectsVisible(bool visible)
    {
// 缺少必要引用时退出 SetSequenceOnlyObjectsVisible
        if (sequenceOnlyObjects == null) return;
        foreach (GameObject sequenceObject in sequenceOnlyObjects)
        {
// 检查 SetSequenceOnlyObjectsVisible 的前置条件
            if (sequenceObject != null) sequenceObject.SetActive(visible);
        }
    }

// 设置 SetResultObjectsVisible 的目标状态
    private void SetResultObjectsVisible(bool visible)
    {
// 检查 SetResultObjectsVisible 的前置条件
        if (hand != null) hand.SetActive(visible);
        if (eye != null) eye.SetActive(visible);
    }

// 处理 ReleaseLocks 对应逻辑
    private void ReleaseLocks()
    {
// 使用 ReleaseLocks 所需功能
        interactionLease?.Dispose();
        interactionLease = null;
// 使用 ReleaseLocks 所需功能（ReleaseLocks）
        movementLease?.Dispose();
        movementLease = null;
    }
}
