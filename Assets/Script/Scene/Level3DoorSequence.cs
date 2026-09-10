using System;
using System.Collections;
using UnityEngine;

// 调用 RequireComponent
[RequireComponent(typeof(BoxCollider2D))]
public class Level3DoorSequence : MonoBehaviour, IInteractionPromptSource
{
// 更新当前逻辑
    [Serializable]
    private class SequenceStep
    {
// 保存 label 数据
        public string label;
        public GameObject root;
// 保存 animator 数据
        public Animator animator;
        public string stateName;
// 记录 showResultObjectsDuringStep 状态
        public bool showResultObjectsDuringStep;
        public bool hideRootAfterStep = true;
// 配置 duration 数值
        [Min(0.01f)] public float duration = 1f;
    }

// 配置 演出步骤（严格为 sps0 → sps） 分组
    [Header("演出步骤（严格为 sps0 → sps）")]
    [SerializeField] private SequenceStep[] steps;

// 配置 六个便利店调查条件 分组
    [Header("六个便利店调查条件")]
    [Tooltip("填写 ClueData.ClueId；组件检查 investigated_<id> Flag，不改变核心线索列表。")]
// 保存 requiredInvestigationIds 数据
    [SerializeField] private string[] requiredInvestigationIds =
    {
// 更新当前逻辑
        "store_note",
        "store_poster",
// 更新当前逻辑
        "store_vegetables",
        "store_handprint",
// 更新当前逻辑
        "store_fruit",
        "store_toy"
// 更新当前逻辑
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
// 保存 resolvedFlag 数据
    [SerializeField] private string resolvedFlag = "level3_store_shadow_resolved";
    [SerializeField] private string toyDeliveredFlag = "level3_store_toy_delivered";
// 保存 redToyPickupFlag 数据
    [SerializeField] private string redToyPickupFlag = "picked_store_red_toy";

// 配置 触发与交互 分组
    [Header("触发与交互")]
    [SerializeField] private GameObject interactionUI;
// 保存 playerTag 数据
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactionKey = KeyCode.F;

// 保存 sequenceCoroutine 数据
    private Coroutine sequenceCoroutine;
    private IDisposable movementLease;
// 保存 interactionLease 数据
    private IDisposable interactionLease;
    private bool playerInRange;
// 记录 inputSuppressed 状态
    private bool inputSuppressed;
    private bool subscribedToGameManager;
// 记录 completed 状态
    private bool completed;
    private bool resolved;
// 记录 exclusiveModeActive 状态
    private bool exclusiveModeActive;
    private bool sequencePriorityActive;
// 记录红色身影 BGM 是否占用中
    private bool redFigureBgmActive;

// 更新当前逻辑
    public bool IsInteractionPromptEligible
    {
// 更新当前逻辑
        get
        {
// 判断当前条件
            if (!isActiveAndEnabled || !playerInRange || sequenceCoroutine != null
                || inputSuppressed || resolved)
            {
// 返回当前结果
                return false;
            }

// 返回当前结果
            return !completed ? HasAllInvestigations() : !resolved;
        }
    }

// 定义 InvestigationFlag 方法
    private string InvestigationFlag(string clueId) => $"investigated_{clueId}";

// 定义 Awake 方法
    private void Awake()
    {
// 获取组件引用
        GetComponent<BoxCollider2D>().isTrigger = true;
        ApplySavedState();
// 执行 HidePrompt
        HidePrompt();
    }

// 定义 Start 方法
    private void Start()
    {
// 执行 ApplySavedState
        ApplySavedState();
    }

// 定义 OnEnable 方法
    private void OnEnable()
    {
// 执行 ApplySavedState
        ApplySavedState();
        SubscribeGameManager(true);
    }

// 定义 OnDisable 方法
    private void OnDisable()
    {
// 调用 UnregisterSource
        PlayerInteractionPromptController.UnregisterSource(this);
        SubscribeGameManager(false);
// 执行 InterruptSequence
        InterruptSequence();
        CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
// 调用 ClearExclusiveInteractionTarget
        CluePickup2D.ClearExclusiveInteractionTarget(redToyGameObject);
        sequencePriorityActive = false;
// 执行 HidePrompt
        HidePrompt();
    }

// 定义 OnDestroy 方法
    private void OnDestroy()
    {
// 调用 UnregisterSource
        PlayerInteractionPromptController.UnregisterSource(this);
        SubscribeGameManager(false);
// 执行 InterruptSequence
        InterruptSequence();
        CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
// 调用 ClearExclusiveInteractionTarget
        CluePickup2D.ClearExclusiveInteractionTarget(redToyGameObject);
        sequencePriorityActive = false;
    }

// 定义 Update 方法
    private void Update()
    {
// 执行 UpdateExclusiveInteractionMode
        UpdateExclusiveInteractionMode();

// 判断当前条件
        if (!subscribedToGameManager)
        {
// 执行 SubscribeGameManager
            SubscribeGameManager(true);
        }

// 判断当前条件
        if (GameplayInputLock.IsInteractionLocked)
        {
// 更新当前状态
            inputSuppressed = true;
            HidePrompt();
// 返回当前结果
            return;
        }

// 判断当前条件
        if (inputSuppressed)
        {
// 更新当前状态
            inputSuppressed = false;
            RefreshPrompt();
        }

// 检测按键输入
        if (!playerInRange || sequenceCoroutine != null || !Input.GetKeyDown(interactionKey)
            || (DialogueUIManager.Instance != null && !DialogueUIManager.Instance.CanOpenDialogue))
        {
// 返回当前结果
            return;
        }

// 判断当前条件
        if (resolved)
        {
// 调用 ClearExclusiveInteractionTarget
            CluePickup2D.ClearExclusiveInteractionTarget(redToyGameObject);
            return;
        }

// 判断当前条件
        if (!completed)
        {
// 判断当前条件
            if (HasAllInvestigations())
            {
// 启动当前协程
                sequenceCoroutine = StartCoroutine(PlaySequence());
            }

// 返回当前结果
            return;
        }

// 判断当前条件
        if (!HasRedToyPickup())
        {
// 调用 SetExclusiveInteractionTarget
            CluePickup2D.SetExclusiveInteractionTarget(redToyGameObject);
            sequenceCoroutine = StartCoroutine(RemindMissingToy());
// 返回当前结果
            return;
        }

// 调用 ClearExclusiveInteractionTarget
        CluePickup2D.ClearExclusiveInteractionTarget(redToyGameObject);
        sequenceCoroutine = StartCoroutine(DeliverToy());
    }

// 定义 OnTriggerEnter2D 方法
    private void OnTriggerEnter2D(Collider2D other)
    {
// 判断当前条件
        if (other.CompareTag(playerTag))
        {
// 更新当前状态
            playerInRange = true;
            PlayerInteractionPromptController.RegisterSource(this);
// 调用 RefreshSource
            PlayerInteractionPromptController.RefreshSource(this);
        }
    }

// 定义 OnTriggerExit2D 方法
    private void OnTriggerExit2D(Collider2D other)
    {
// 判断当前条件
        if (other.CompareTag(playerTag))
        {
// 更新当前状态
            playerInRange = false;
            PlayerInteractionPromptController.UnregisterSource(this);
        }
    }

// 定义 PlaySequence 方法
    private IEnumerator PlaySequence()
    {
// 执行 HidePrompt
        HidePrompt();

// 判断当前条件
        if (!HasAllInvestigations() || !HasValidSteps())
        {
// 判断当前条件
            if (!HasValidSteps())
            {
// 输出调试信息
                Debug.LogError("[Level3DoorSequence] 必须按顺序配置且仅配置 sps0、sps 两个有效步骤，演出未启动。", this);
            }

// 更新当前状态
            sequenceCoroutine = null;
            RefreshPrompt();
// 保存 break 数据
            yield break;
        }

// 更新当前状态
        movementLease = GameplayInputLock.AcquireMovementLock();
        interactionLease = GameplayInputLock.AcquireInteractionLock();
// 执行 SetAllStepsVisible
        SetAllStepsVisible(false);
        SetSequenceOnlyObjectsVisible(false);
// 执行 SetResultObjectsVisible
        SetResultObjectsVisible(false);

// 遍历全部元素
        foreach (SequenceStep step in steps)
        {
// 空引用时直接退出
            if (step == null)
            {
// 更新当前逻辑
                continue;
            }

// 判断当前条件
// 红色身影出现时暂停普通 BGM
            if (string.Equals(step.label, "sps", StringComparison.Ordinal)) StartRedFigureBgm();

            if (step.root != null)
            {
// 切换显示状态
                step.root.SetActive(true);
            }

// 判断当前条件
            if (step.showResultObjectsDuringStep)
            {
// 执行 SetResultObjectsVisible
                SetResultObjectsVisible(true);
            }

// 判断当前条件
            if (step.animator != null && !string.IsNullOrEmpty(step.stateName))
            {
// 调用 Play
                step.animator.Play(step.stateName, 0, 0f);
                step.animator.Update(0f);
            }

// 等待下一步
            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, step.duration));

// 判断当前条件
            if (step.hideRootAfterStep && step.root != null)
            {
// 切换显示状态
                step.root.SetActive(false);
            }

// 判断当前条件
            if (step.showResultObjectsDuringStep)
            {
// 执行 SetResultObjectsVisible
                SetResultObjectsVisible(false);
            }
        }

// 执行 SetResultObjectsVisible
        SetResultObjectsVisible(true);
        completed = true;
// 执行 SetSequenceOnlyObjectsVisible
        SetSequenceOnlyObjectsVisible(false);
        SetStepRootVisible(0, false);
// 执行 SetStepRootVisible
        SetStepRootVisible(1, true);
        GameManager.Instance?.SetFlag(completedFlag);

// 等待下一步
        yield return StartDialogueAndWait(revealDialogue);
        StopRedFigureBgm();

// 执行 ReleaseLocks
        ReleaseLocks();
        sequenceCoroutine = null;
// 执行 RefreshPrompt
        RefreshPrompt();
    }

// 定义 RemindMissingToy 方法
    private IEnumerator RemindMissingToy()
    {
// 执行 HidePrompt
        HidePrompt();
        movementLease = GameplayInputLock.AcquireMovementLock();
// 更新当前状态
        interactionLease = GameplayInputLock.AcquireInteractionLock();

// 等待下一步
        yield return StartDialogueAndWait(missingToyDialogue);

// 执行 ReleaseLocks
        ReleaseLocks();
        sequenceCoroutine = null;
// 执行 RefreshPrompt
        RefreshPrompt();
    }

// 定义 DeliverToy 方法
    private IEnumerator DeliverToy()
    {
// 执行 HidePrompt
        HidePrompt();
        movementLease = GameplayInputLock.AcquireMovementLock();
// 更新当前状态
        interactionLease = GameplayInputLock.AcquireInteractionLock();

// 等待下一步
        yield return StartDialogueAndWait(deliveryDialogue);
        GameManager.Instance?.SetFlag(toyDeliveredFlag);
// 等待下一步
        yield return StartDialogueAndWait(disappearanceDialogue);

// 判断当前条件
        if (SfxManager.Instance != null && fallingBreakingSound != null)
        {
// 调用 Play
            SfxManager.Instance.Play(fallingBreakingSound, fallingBreakingVolume);
        }

// 执行 SetResultObjectsVisible
        SetResultObjectsVisible(false);
        SetStepRootVisible(1, false);
// 等待下一步
        yield return StartDialogueAndWait(exitReadyDialogue);

// 更新剧情标记
        GameManager.Instance?.SetFlag(resolvedFlag);
        resolved = true;
// 执行 ReleaseLocks
        ReleaseLocks();
        sequenceCoroutine = null;
// 执行 RefreshPrompt
        RefreshPrompt();
    }

// 定义 StartDialogueAndWait 方法
    private IEnumerator StartDialogueAndWait(DialogueData dialogue)
    {
// 空引用时直接退出
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0
            || DialogueUIManager.Instance == null)
        {
// 保存 break 数据
            yield break;
        }

// 记录 finished 状态
        bool finished = false;
        DialogueUIManager.Instance.StartDialogue(dialogue, () => finished = true);
// 等待条件变化
        while (!finished)
        {
// 等待下一步
            yield return null;
        }
    }

// 定义 HasAllInvestigations 方法
    private bool HasAllInvestigations()
    {
// 空引用时直接退出
        if (GameManager.Instance == null || requiredInvestigationIds == null || requiredInvestigationIds.Length == 0)
        {
// 返回当前结果
            return false;
        }

// 遍历全部元素
        foreach (string clueId in requiredInvestigationIds)
        {
// 判断当前条件
            if (string.IsNullOrEmpty(clueId) || !GameManager.Instance.HasFlag(InvestigationFlag(clueId)))
            {
// 返回当前结果
                return false;
            }
        }

// 返回当前结果
        return true;
    }

// 定义 HasRedToyPickup 方法
    private bool HasRedToyPickup()
    {
// 返回当前结果
        return GameManager.Instance != null
            && !string.IsNullOrEmpty(redToyPickupFlag)
// 调用 HasFlag
            && GameManager.Instance.HasFlag(redToyPickupFlag);
    }

// 定义 HasValidSteps 方法
    private bool HasValidSteps()
    {
// 空引用时直接退出
        if (steps == null || steps.Length != 2)
        {
// 返回当前结果
            return false;
        }

// 循环处理当前集合
        for (int i = 0; i < steps.Length; i++)
        {
// 保存 step 数据
            SequenceStep step = steps[i];
            string expectedLabel = i == 0 ? "sps0" : "sps";
// 空引用时直接退出
            if (step == null || !string.Equals(step.label, expectedLabel, StringComparison.Ordinal)
                || step.root == null || step.duration < 0.01f)
            {
// 返回当前结果
                return false;
            }
        }

// 返回当前结果
        return true;
    }

// 定义 ApplySavedState 方法
    private void ApplySavedState()
    {
// 保存 gm 数据
        GameManager gm = GameManager.Instance;
        completed = gm != null && gm.HasFlag(completedFlag);
// 更新当前状态
        resolved = gm != null && gm.HasFlag(resolvedFlag);

// 执行 SetAllStepsVisible
        SetAllStepsVisible(false);
        SetSequenceOnlyObjectsVisible(false);
// 执行 SetResultObjectsVisible
        SetResultObjectsVisible(completed && !resolved);
        SetStepRootVisible(0, false);
// 执行 SetStepRootVisible
        SetStepRootVisible(1, completed && !resolved);
        RefreshPrompt();
    }

// 定义 SubscribeGameManager 方法
    private void SubscribeGameManager(bool subscribe)
    {
// 保存 gm 数据
        GameManager gm = GameManager.Instance;
        if (gm == null || subscribedToGameManager == subscribe)
        {
// 返回当前结果
            return;
        }

// 判断当前条件
        if (subscribe)
        {
// 更新当前逻辑
            gm.OnFlagsChanged += HandleFlagsChanged;
        }
// 处理其他分支
        else
        {
// 更新当前逻辑
            gm.OnFlagsChanged -= HandleFlagsChanged;
        }

// 更新当前状态
        subscribedToGameManager = subscribe;
    }

// 定义 HandleFlagsChanged 方法
    private void HandleFlagsChanged()
    {
// 执行 ApplySavedState
        ApplySavedState();
        UpdateExclusiveInteractionMode();
// 调用 RefreshSource
        PlayerInteractionPromptController.RefreshSource(this);
    }

// 定义 UpdateExclusiveInteractionMode 方法
    private void UpdateExclusiveInteractionMode()
    {
// 定义 HasAllInvestigations 方法
        bool shouldPrioritizeSequence = !completed && playerInRange && HasAllInvestigations();
        if (sequencePriorityActive != shouldPrioritizeSequence)
        {
// 更新当前状态
            sequencePriorityActive = shouldPrioritizeSequence;
            if (shouldPrioritizeSequence)
            {
// 调用 SetExclusiveInteractionTarget
                CluePickup2D.SetExclusiveInteractionTarget(gameObject);
            }
// 处理其他分支
            else
            {
// 调用 ClearExclusiveInteractionTarget
                CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
            }
        }

// 定义 HasRedToyPickup 方法
        bool shouldBeActive = redToyGameObject != null && completed && !resolved && !HasRedToyPickup();
        if (exclusiveModeActive == shouldBeActive)
        {
// 返回当前结果
            return;
        }

// 更新当前状态
        exclusiveModeActive = shouldBeActive;
        if (shouldBeActive)
        {
// 调用 SetExclusiveInteractionTarget
            CluePickup2D.SetExclusiveInteractionTarget(redToyGameObject);
        }
// 处理其他分支
        else
        {
// 调用 ClearExclusiveInteractionTarget
            CluePickup2D.ClearExclusiveInteractionTarget(redToyGameObject);
        }
    }

// 定义 RefreshPrompt 方法
    private void RefreshPrompt()
    {
// 调用 RefreshSource
        PlayerInteractionPromptController.RefreshSource(this);
    }

// 定义 HidePrompt 方法
    private void HidePrompt()
    {
// 调用 RefreshSource
        PlayerInteractionPromptController.RefreshSource(this);
    }

// 定义 StartRedFigureBgm 方法
    private void StartRedFigureBgm()
    {
// 空引用时直接退出
        if (redFigureBgmActive || redFigureBgm == null || MusicManager.Instance == null) return;
// 暂停普通 BGM 并循环播放红色身影音乐
        MusicManager.Instance.PlayInterruptingBgm(this, redFigureBgm, redFigureBgmVolume, redFigureBgmFade);
        redFigureBgmActive = true;
    }

// 定义 StopRedFigureBgm 方法
    private void StopRedFigureBgm()
    {
// 空引用时直接退出
        if (!redFigureBgmActive) return;
// 从原进度恢复普通 BGM
        MusicManager.Instance?.StopInterruptingBgm(this, redFigureBgmFade);
        redFigureBgmActive = false;
    }

// 定义 InterruptSequence 方法
    private void InterruptSequence()
    {
        StopRedFigureBgm();
// 判断当前条件
        if (sequenceCoroutine != null)
        {
// 执行 StopCoroutine
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

// 执行 ReleaseLocks
        ReleaseLocks();
        if (!completed || resolved)
        {
// 执行 SetAllStepsVisible
            SetAllStepsVisible(false);
            SetSequenceOnlyObjectsVisible(false);
        }

// 判断当前条件
        if (resolved)
        {
// 执行 SetResultObjectsVisible
            SetResultObjectsVisible(false);
        }
    }

// 定义 SetStepRootVisible 方法
    private void SetStepRootVisible(int index, bool visible)
    {
// 空引用时直接退出
        if (steps == null || index < 0 || index >= steps.Length || steps[index] == null
            || steps[index].root == null)
        {
// 返回当前结果
            return;
        }

// 切换显示状态
        steps[index].root.SetActive(visible);
    }

// 定义 SetAllStepsVisible 方法
    private void SetAllStepsVisible(bool visible)
    {
// 空引用时直接退出
        if (steps == null) return;
        foreach (SequenceStep step in steps)
        {
// 判断当前条件
            if (step != null && step.root != null) step.root.SetActive(visible);
        }
    }

// 定义 SetSequenceOnlyObjectsVisible 方法
    private void SetSequenceOnlyObjectsVisible(bool visible)
    {
// 空引用时直接退出
        if (sequenceOnlyObjects == null) return;
        foreach (GameObject sequenceObject in sequenceOnlyObjects)
        {
// 判断当前条件
            if (sequenceObject != null) sequenceObject.SetActive(visible);
        }
    }

// 定义 SetResultObjectsVisible 方法
    private void SetResultObjectsVisible(bool visible)
    {
// 判断当前条件
        if (hand != null) hand.SetActive(visible);
        if (eye != null) eye.SetActive(visible);
    }

// 定义 ReleaseLocks 方法
    private void ReleaseLocks()
    {
// 调用 Dispose
        interactionLease?.Dispose();
        interactionLease = null;
// 调用 Dispose
        movementLease?.Dispose();
        movementLease = null;
    }
}
