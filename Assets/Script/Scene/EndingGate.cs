using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 判定天台结局并播放真假结局流程
public class EndingGate : MonoBehaviour
{
    [Header("触发")]
// 同步 EndingGate 的相关数据
    [SerializeField] private string finalClueId = "roof_diary_final";
    [SerializeField] private int menuSceneIndex = 0;
// 说明当前配置
    [Tooltip("仅调试用：勾选后直接走真结局，不检查前置线索")]
    [SerializeField] private bool forceTrueEndingForTesting;

// 配置 真结局移动 分组
    [Header("真结局移动")]
    [SerializeField] private PlayerMovement2D player;
// 保存 downwardWaypoint 引用
    [SerializeField] private Transform downwardWaypoint;
    [SerializeField] private Transform leftTarget;
// 设置 EndingGate 的配置数值
    [SerializeField] private float movementSpeedMultiplier = 0.25f;
    [SerializeField] private float movementTimeout = 8f;

// 配置 真结局画面（按顺序） 分组
    [Header("真结局画面（按顺序）")]
    [SerializeField] private GameObject closedDiary;
    [SerializeField] private Sprite[] endingPlayerSprites;
// 设置 EndingGate 的配置数值（EndingGate 后续步骤）
    [SerializeField] private float presentationDuration = 1f;

// 配置 结局对白 分组
    [Header("结局对白")]
    [SerializeField] private DialogueData trueEnding;
// 保存 badEnding 引用
    [SerializeField] private DialogueData badEnding;
    [SerializeField] private Canvas endingDialogueCanvas;
// 设置 EndingGate 的配置数值（EndingGate 后续步骤）（39）
    [SerializeField] private int endingDialogueSortingOrder = 100;

// 记录 EndingGate 的当前状态
    private static bool endingTriggeredThisRuntime;

// 记录 EndingGate 的当前状态（EndingGate 后续步骤）
    private bool endingTakenOver;
    private bool transitionStarted;
// 记录 EndingGate 的当前状态（EndingGate 后续步骤）（48）
    private bool endingDialogueFailed;
    private IDisposable movementLock;
// 同步 EndingGate 的相关数据（EndingGate 后续步骤）
    private IDisposable interactionLock;

// 记录 EndingGate 的当前状态（EndingGate 后续步骤）（54）
    public bool HasTakenOverEnding => endingTakenOver || endingTriggeredThisRuntime;

// 处理 ResetRuntimeState 对应逻辑
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
// 同步 ResetRuntimeState 的状态
        ResetSessionState();
    }

    public static void ResetSessionState()
    {
        endingTriggeredThisRuntime = false;
    }

// 读取初始依赖并同步首帧状态
    private void Start()
    {
// 推进 Start 中的必要步骤
        SetPresentationObjects(false);
        ResolvePlayerReference();
// Start 缺少引用时提前结束
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnClueCollected += HandleClueCollected;
// 推进 Start 中的必要步骤（Start）
        ResolveIfFinalClueAlreadyCollected(GameManager.Instance);
    }

// 销毁时释放事件订阅和静态引用
    private void OnDestroy()
    {
// 检查 OnDestroy 的前置条件
        if (GameManager.Instance != null) GameManager.Instance.OnClueCollected -= HandleClueCollected;
        ReleaseLocks();
// 使用 OnDestroy 所需功能
        ClueJournalUI.SetEndingDisabled(false);
    }

// 每帧检查输入与状态变化
    private void Update()
    {
// 缺少必要引用时退出 Update
        if (HasTakenOverEnding || GameManager.Instance == null) return;
        if (GameManager.Instance.HasClue(finalClueId))
        {
// 推进 Update 中的必要步骤
            ResolveEnding(GameManager.Instance);
        }
    }

// 解析 ResolvePlayerReference 对应结果
    private void ResolvePlayerReference()
    {
// 缺少必要引用时退出 ResolvePlayerReference
        if (player == null) player = FindFirstObjectByType<PlayerMovement2D>();
    }

// 解析 ResolveIfFinalClueAlreadyCollected 对应结果
    private void ResolveIfFinalClueAlreadyCollected(GameManager gm)
    {
// 检查 ResolveIfFinalClueAlreadyCollected 的前置条件
        if (gm != null && !HasTakenOverEnding && !string.IsNullOrEmpty(finalClueId) && gm.HasClue(finalClueId))
        {
// 推进 ResolveIfFinalClueAlreadyCollected 中的必要步骤
            ResolveEnding(gm);
        }
    }

// 响应 HandleClueCollected 状态变化
    private void HandleClueCollected(ClueData clue)
    {
// 缺少必要引用时退出 HandleClueCollected
        if (HasTakenOverEnding || clue == null || clue.ClueId != finalClueId) return;
        ResolveEnding(GameManager.Instance);
    }

// 解析 ResolveEnding 对应结果
    private void ResolveEnding(GameManager gm)
    {
// 缺少必要引用时退出 ResolveEnding
        if (gm == null || HasTakenOverEnding) return;
        endingTriggeredThisRuntime = true;
// 同步 ResolveEnding 的内部状态
        endingTakenOver = true;
        movementLock = GameplayInputLock.AcquireMovementLock();
// 同步 ResolveEnding 的内部状态（ResolveEnding）
        interactionLock = GameplayInputLock.AcquireInteractionLock();
        ClueJournalUI.SetEndingDisabled(true);
// 判断 HasCollectedAllPreRooftopClues 对应条件
        bool trueEndingBranch = forceTrueEndingForTesting || gm.HasCollectedAllPreRooftopClues();
        Debug.Log($"[EndingGate] 结局触发，真结局分支：{trueEndingBranch}", this);
// 启动当前协程
        StartCoroutine(PlayEnding(trueEndingBranch, gm));
    }

// 播放 PlayEnding 对应演出
    private IEnumerator PlayEnding(bool trueEndingBranch, GameManager gm)
    {
// 保护可能失败的流程
        try
        {
// 等待条件变化
            while (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen) yield return null;
            while (InvestigationDirector.Instance != null && InvestigationDirector.Instance.IsPlayingFlashback) yield return null;

// 检查 PlayEnding 的前置条件
            if (trueEndingBranch)
            {
// 更新剧情标记
                gm.SetFlag("truth_revealed");
                yield return PlayTrueEndingSequence();
            }

// 等待下一步
            yield return FadeToBlack();
            ConfigureEndingDialogueCanvas();
// 同步 PlayEnding 的内部状态
            endingDialogueFailed = false;
            yield return PlayDialogue(trueEndingBranch ? trueEnding : badEnding);
// 检查 PlayEnding 的前置条件（PlayEnding）
            if (!endingDialogueFailed) ReturnToMenu(trueEndingBranch);
        }
// 推进 PlayEnding 的当前步骤
        finally
        {
// 推进 PlayEnding 中的必要步骤
            ReleaseLocks();
            ClueJournalUI.SetEndingDisabled(false);
        }
    }

// 播放真结局从静止到黑屏的完整流程
    private IEnumerator PlayTrueEndingSequence()
    {
        // 重置结局演出对象
        SetPresentationObjects(false);

        // 闭合日记整段保持显示
        if (closedDiary != null)
        {
            closedDiary.SetActive(true);
        }

        // 闭合日记出现后原地停留
        yield return new WaitForSeconds(1f);

        // 玩家移动后停留
        yield return PlayTrueEndingMovement();
        yield return new WaitForSeconds(1f);

        // 到达目的地后切换玩家图像
        yield return PlayEndingSprites();
    }
    private IEnumerator PlayTrueEndingMovement()
    {
// 缺少必要引用时退出 PlayTrueEndingMovement
        if (player == null) yield break;

// 同步 PlayTrueEndingMovement 的相关数据
        Vector3 verticalTarget = downwardWaypoint != null
            ? downwardWaypoint.position
// 使用 PlayTrueEndingMovement 所需功能
            : new Vector3(
                player.transform.position.x,
// 推进 PlayTrueEndingMovement 的当前步骤
                leftTarget != null ? leftTarget.position.y : player.transform.position.y,
                player.transform.position.z);

// 在 PlayTrueEndingMovement 中继续当前处理
        yield return player.MoveToEndingTarget(
            verticalTarget,
// 推进 PlayTrueEndingMovement 的当前步骤（PlayTrueEndingMovement）
            Vector2.down,
            movementSpeedMultiplier,
// 推进 PlayTrueEndingMovement 的当前步骤（PlayTrueEndingMovement）（movementTimeout）
            movementTimeout);

// 检查 PlayTrueEndingMovement 的前置条件
        if (leftTarget != null)
        {
// 在 PlayTrueEndingMovement 中继续当前处理（PlayTrueEndingMovement 后续步骤）
            yield return player.MoveToEndingTarget(
                leftTarget.position,
// 推进 PlayTrueEndingMovement 的当前步骤（PlayTrueEndingMovement）（Vector2）
                Vector2.left,
                movementSpeedMultiplier,
// 在 PlayTrueEndingMovement 中继续当前处理（PlayTrueEndingMovement 后续步骤）（236）
                movementTimeout);
        }

// 使用 PlayTrueEndingMovement 所需功能（PlayTrueEndingMovement）
        player.SetEndingIdleLeft();
    }

// 到达目标后依次切换玩家结局图像
    private IEnumerator PlayEndingSprites()
    {
// 缺少必要引用时退出 PlayEndingSprites
        if (player == null || endingPlayerSprites == null) yield break;

// 遍历全部元素
        foreach (Sprite sprite in endingPlayerSprites)
        {
// 缺少必要引用时退出 PlayEndingSprites（PlayEndingSprites）
            if (sprite == null) continue;
            player.SetEndingSprite(sprite);
// 在 PlayEndingSprites 中继续当前处理
            yield return new WaitForSeconds(Mathf.Max(0f, presentationDuration));
        }
    }

// 处理 FadeToBlack 对应逻辑
    private IEnumerator FadeToBlack()
    {
// 缺少必要引用时退出 FadeToBlack
        if (ScreenFader.Instance == null) yield break;
        bool complete = false;
// 使用 FadeToBlack 所需功能
        ScreenFader.Instance.FadeOutThen(() => complete = true);
        while (!complete) yield return null;
    }

// 播放 PlayDialogue 对应演出
    private IEnumerator PlayDialogue(DialogueData dialogue)
    {
// 缺少必要引用时退出 PlayDialogue
        if (dialogue == null)
        {
// 同步 PlayDialogue 的内部状态
            endingDialogueFailed = true;
            Debug.LogError("EndingGate 无法播放结局对白：对白资源未绑定。", this);
// 无法继续时结束 PlayDialogue
            yield break;
        }

// 同步 PlayDialogue 的相关数据
        DialogueUIManager dialogueManager = DialogueUIManager.Instance;
        if (dialogueManager == null)
        {
// 同步 PlayDialogue 的内部状态（PlayDialogue）
            endingDialogueFailed = true;
            Debug.LogError("EndingGate 无法播放结局对白：场景中没有 DialogueUIManager。", this);
// 在 PlayDialogue 中处理 无法继续时结束 PlayDialogue
            yield break;
        }

// 记录 PlayDialogue 的当前状态
        bool complete = false;
        if (!dialogueManager.StartDialogue(dialogue, () => complete = true))
        {
// 同步 PlayDialogue 的内部状态（PlayDialogue）（endingDialogueFailed）
            endingDialogueFailed = true;
            Debug.LogError("EndingGate 无法播放结局对白：DialogueUIManager 启动对白失败。", this);
// 无法继续时结束 PlayDialogue（PlayDialogue）
            yield break;
        }

// 在 PlayDialogue 中继续当前处理
        while (!complete) yield return null;
    }

// 配置 ConfigureEndingDialogueCanvas 对应字段
    private void ConfigureEndingDialogueCanvas()
    {
// 缺少必要引用时退出 ConfigureEndingDialogueCanvas
        if (endingDialogueCanvas == null) return;

// 同步 ConfigureEndingDialogueCanvas 的相关数据
        RectTransform canvasRect = endingDialogueCanvas.GetComponent<RectTransform>();
        if (canvasRect != null && canvasRect.localScale == Vector3.zero)
        {
// 同步 ConfigureEndingDialogueCanvas 的内部状态
            canvasRect.localScale = Vector3.one;
        }

// 切换 ConfigureEndingDialogueCanvas 的显示状态
        endingDialogueCanvas.gameObject.SetActive(true);
        endingDialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
// 同步 ConfigureEndingDialogueCanvas 的内部状态（ConfigureEndingDialogueCanvas）
        endingDialogueCanvas.worldCamera = null;
        endingDialogueCanvas.overrideSorting = true;
// 同步 ConfigureEndingDialogueCanvas 的内部状态（ConfigureEndingDialogueCanvas）（endingDialogueCanvas）
        endingDialogueCanvas.sortingOrder = endingDialogueSortingOrder;
    }

// 设置 SetPresentationObjects 的目标状态
    private void SetPresentationObjects(bool active)
    {
// 检查 SetPresentationObjects 的前置条件
        if (closedDiary != null) closedDiary.SetActive(active);
    }

// 完成结局演出后返回主菜单
    private void ReturnToMenu(bool useWhiteTransition)
    {
// 检查 ReturnToMenu 的前置条件
        if (transitionStarted) return;
        transitionStarted = true;
        endingTriggeredThisRuntime = false;
// 推进 ReturnToMenu 中的必要步骤
        ReleaseLocks();
        ClueJournalUI.SetEndingDisabled(false);

// 检查转场状态
        if (ScreenFader.Instance != null)
        {
// 执行场景切换
            Action loadMenu = () => SceneManager.LoadScene(menuSceneIndex);
            if (useWhiteTransition)
            {
// 使用 ReturnToMenu 所需功能
                ScreenFader.Instance.FadeBlackToWhiteThen(loadMenu, 2f);
            }
// 处理 ReturnToMenu 的备用分支
            else
            {
// 使用 ReturnToMenu 所需功能（ReturnToMenu）
                ScreenFader.Instance.FadeOutThen(loadMenu);
            }
        }
// 在 ReturnToMenu 中处理 处理 ReturnToMenu 的备用分支
        else
        {
// 在 ReturnToMenu 中继续当前处理
            SceneManager.LoadScene(menuSceneIndex);
        }
    }

// 处理 ReleaseLocks 对应逻辑
    private void ReleaseLocks()
    {
// 使用 ReleaseLocks 所需功能
        movementLock?.Dispose();
        interactionLock?.Dispose();
// 同步 ReleaseLocks 的内部状态
        movementLock = null;
        interactionLock = null;
    }
}
