using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 定义 EndingGate 类型
public class EndingGate : MonoBehaviour
{
    [Header("触发")]
// 保存 finalClueId 数据
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
// 配置 movementSpeedMultiplier 数值
    [SerializeField] private float movementSpeedMultiplier = 0.25f;
    [SerializeField] private float movementTimeout = 8f;

// 配置 真结局画面（按顺序） 分组
    [Header("真结局画面（按顺序）")]
    [SerializeField] private GameObject closedDiary;
    [SerializeField] private Sprite[] endingPlayerSprites;
// 配置 presentationDuration 数值
    [SerializeField] private float presentationDuration = 1f;

// 配置 结局对白 分组
    [Header("结局对白")]
    [SerializeField] private DialogueData trueEnding;
// 保存 badEnding 引用
    [SerializeField] private DialogueData badEnding;
    [SerializeField] private Canvas endingDialogueCanvas;
// 配置 endingDialogueSortingOrder 数值
    [SerializeField] private int endingDialogueSortingOrder = 10000;

// 记录 endingTriggeredThisRuntime 状态
    private static bool endingTriggeredThisRuntime;

// 记录 endingTakenOver 状态
    private bool endingTakenOver;
    private bool transitionStarted;
// 记录 endingDialogueFailed 状态
    private bool endingDialogueFailed;
    private IDisposable movementLock;
// 保存 interactionLock 数据
    private IDisposable interactionLock;

// 记录 HasTakenOverEnding 状态
    public bool HasTakenOverEnding => endingTakenOver || endingTriggeredThisRuntime;

// 运行前初始化状态
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
// 更新当前状态
        endingTriggeredThisRuntime = false;
    }

// 定义 Start 方法
    private void Start()
    {
// 执行 SetPresentationObjects
        SetPresentationObjects(false);
        ResolvePlayerReference();
// 空引用时直接退出
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnClueCollected += HandleClueCollected;
// 执行 ResolveIfFinalClueAlreadyCollected
        ResolveIfFinalClueAlreadyCollected(GameManager.Instance);
    }

// 定义 OnDestroy 方法
    private void OnDestroy()
    {
// 判断当前条件
        if (GameManager.Instance != null) GameManager.Instance.OnClueCollected -= HandleClueCollected;
        ReleaseLocks();
// 调用 SetEndingDisabled
        ClueJournalUI.SetEndingDisabled(false);
    }

// 定义 Update 方法
    private void Update()
    {
// 空引用时直接退出
        if (HasTakenOverEnding || GameManager.Instance == null) return;
        if (GameManager.Instance.HasClue(finalClueId))
        {
// 执行 ResolveEnding
            ResolveEnding(GameManager.Instance);
        }
    }

// 定义 ResolvePlayerReference 方法
    private void ResolvePlayerReference()
    {
// 空引用时直接退出
        if (player == null) player = FindFirstObjectByType<PlayerMovement2D>();
    }

// 定义 ResolveIfFinalClueAlreadyCollected 方法
    private void ResolveIfFinalClueAlreadyCollected(GameManager gm)
    {
// 判断当前条件
        if (gm != null && !HasTakenOverEnding && !string.IsNullOrEmpty(finalClueId) && gm.HasClue(finalClueId))
        {
// 执行 ResolveEnding
            ResolveEnding(gm);
        }
    }

// 定义 HandleClueCollected 方法
    private void HandleClueCollected(ClueData clue)
    {
// 空引用时直接退出
        if (HasTakenOverEnding || clue == null || clue.ClueId != finalClueId) return;
        ResolveEnding(GameManager.Instance);
    }

// 定义 ResolveEnding 方法
    private void ResolveEnding(GameManager gm)
    {
// 空引用时直接退出
        if (gm == null || HasTakenOverEnding) return;
        endingTriggeredThisRuntime = true;
// 更新当前状态
        endingTakenOver = true;
        movementLock = GameplayInputLock.AcquireMovementLock();
// 更新当前状态
        interactionLock = GameplayInputLock.AcquireInteractionLock();
        ClueJournalUI.SetEndingDisabled(true);
// 定义 HasCollectedAllPreRooftopClues 方法
        bool trueEndingBranch = forceTrueEndingForTesting || gm.HasCollectedAllPreRooftopClues();
        Debug.Log($"[EndingGate] 结局触发，真结局分支：{trueEndingBranch}", this);
// 启动当前协程
        StartCoroutine(PlayEnding(trueEndingBranch, gm));
    }

// 定义 PlayEnding 方法
    private IEnumerator PlayEnding(bool trueEndingBranch, GameManager gm)
    {
// 保护可能失败的流程
        try
        {
// 等待条件变化
            while (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen) yield return null;
            while (InvestigationDirector.Instance != null && InvestigationDirector.Instance.IsPlayingFlashback) yield return null;

// 判断当前条件
            if (trueEndingBranch)
            {
// 更新剧情标记
                gm.SetFlag("truth_revealed");
                yield return PlayTrueEndingSequence();
            }

// 等待下一步
            yield return FadeToBlack();
            ConfigureEndingDialogueCanvas();
// 更新当前状态
            endingDialogueFailed = false;
            yield return PlayDialogue(trueEndingBranch ? trueEnding : badEnding);
// 判断当前条件
            if (!endingDialogueFailed) ReturnToMenu(trueEndingBranch);
        }
// 更新当前逻辑
        finally
        {
// 执行 ReleaseLocks
            ReleaseLocks();
            ClueJournalUI.SetEndingDisabled(false);
        }
    }

// 定义 PlayTrueEndingSequence 方法
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
// 空引用时直接退出
        if (player == null) yield break;

// 保存 verticalTarget 数据
        Vector3 verticalTarget = downwardWaypoint != null
            ? downwardWaypoint.position
// 调用 Vector3
            : new Vector3(
                player.transform.position.x,
// 更新当前逻辑
                leftTarget != null ? leftTarget.position.y : player.transform.position.y,
                player.transform.position.z);

// 等待下一步
        yield return player.MoveToEndingTarget(
            verticalTarget,
// 更新当前逻辑
            Vector2.down,
            movementSpeedMultiplier,
// 更新当前逻辑
            movementTimeout);

// 判断当前条件
        if (leftTarget != null)
        {
// 等待下一步
            yield return player.MoveToEndingTarget(
                leftTarget.position,
// 更新当前逻辑
                Vector2.left,
                movementSpeedMultiplier,
// 更新当前逻辑
                movementTimeout);
        }

// 调用 SetEndingIdleLeft
        player.SetEndingIdleLeft();
    }

// 定义 PlayEndingSprites 方法
    private IEnumerator PlayEndingSprites()
    {
// 空引用时直接退出
        if (player == null || endingPlayerSprites == null) yield break;

// 遍历全部元素
        foreach (Sprite sprite in endingPlayerSprites)
        {
// 空引用时直接退出
            if (sprite == null) continue;
            player.SetEndingSprite(sprite);
// 等待下一步
            yield return new WaitForSeconds(Mathf.Max(0f, presentationDuration));
        }
    }

// 定义 FadeToBlack 方法
    private IEnumerator FadeToBlack()
    {
// 空引用时直接退出
        if (ScreenFader.Instance == null) yield break;
        bool complete = false;
// 调用 FadeOutThen
        ScreenFader.Instance.FadeOutThen(() => complete = true);
        while (!complete) yield return null;
    }

// 定义 PlayDialogue 方法
    private IEnumerator PlayDialogue(DialogueData dialogue)
    {
// 空引用时直接退出
        if (dialogue == null)
        {
// 更新当前状态
            endingDialogueFailed = true;
            Debug.LogError("EndingGate 无法播放结局对白：对白资源未绑定。", this);
// 保存 break 数据
            yield break;
        }

// 保存 dialogueManager 数据
        DialogueUIManager dialogueManager = DialogueUIManager.Instance;
        if (dialogueManager == null)
        {
// 更新当前状态
            endingDialogueFailed = true;
            Debug.LogError("EndingGate 无法播放结局对白：场景中没有 DialogueUIManager。", this);
// 保存 break 数据
            yield break;
        }

// 记录 complete 状态
        bool complete = false;
        if (!dialogueManager.StartDialogue(dialogue, () => complete = true))
        {
// 更新当前状态
            endingDialogueFailed = true;
            Debug.LogError("EndingGate 无法播放结局对白：DialogueUIManager 启动对白失败。", this);
// 保存 break 数据
            yield break;
        }

// 等待条件变化
        while (!complete) yield return null;
    }

// 定义 ConfigureEndingDialogueCanvas 方法
    private void ConfigureEndingDialogueCanvas()
    {
// 空引用时直接退出
        if (endingDialogueCanvas == null) return;

// 保存 canvasRect 数据
        RectTransform canvasRect = endingDialogueCanvas.GetComponent<RectTransform>();
        if (canvasRect != null && canvasRect.localScale == Vector3.zero)
        {
// 更新当前状态
            canvasRect.localScale = Vector3.one;
        }

// 切换显示状态
        endingDialogueCanvas.gameObject.SetActive(true);
        endingDialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
// 更新当前状态
        endingDialogueCanvas.worldCamera = null;
        endingDialogueCanvas.overrideSorting = true;
// 更新当前状态
        endingDialogueCanvas.sortingOrder = endingDialogueSortingOrder;
    }

// 定义 SetPresentationObjects 方法
    private void SetPresentationObjects(bool active)
    {
// 判断当前条件
        if (closedDiary != null) closedDiary.SetActive(active);
    }

// 定义 ReturnToMenu 方法
    private void ReturnToMenu(bool useWhiteTransition)
    {
// 判断当前条件
        if (transitionStarted) return;
        transitionStarted = true;
// 执行 ReleaseLocks
        ReleaseLocks();
        ClueJournalUI.SetEndingDisabled(false);

// 检查转场状态
        if (ScreenFader.Instance != null)
        {
// 执行场景切换
            Action loadMenu = () => SceneManager.LoadScene(menuSceneIndex);
            if (useWhiteTransition)
            {
// 调用 FadeToWhiteThen
                ScreenFader.Instance.FadeBlackToWhiteThen(loadMenu, 2f);
            }
// 处理其他分支
            else
            {
// 调用 FadeOutThen
                ScreenFader.Instance.FadeOutThen(loadMenu);
            }
        }
// 处理其他分支
        else
        {
// 执行场景切换
            SceneManager.LoadScene(menuSceneIndex);
        }
    }

// 定义 ReleaseLocks 方法
    private void ReleaseLocks()
    {
// 调用 Dispose
        movementLock?.Dispose();
        interactionLock?.Dispose();
// 更新当前状态
        movementLock = null;
        interactionLock = null;
    }
}
