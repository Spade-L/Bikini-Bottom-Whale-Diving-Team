using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingGate : MonoBehaviour
{
    [Header("触发")]
    [SerializeField] private string finalClueId = "roof_diary_final";
    [SerializeField] private int menuSceneIndex = 0;
    [Tooltip("仅调试用：勾选后直接走真结局，不检查前置线索")]
    [SerializeField] private bool forceTrueEndingForTesting;

    [Header("真结局移动")]
    [SerializeField] private PlayerMovement2D player;
    [SerializeField] private Transform downwardWaypoint;
    [SerializeField] private Transform leftTarget;
    [SerializeField] private float movementSpeedMultiplier = 0.25f;
    [SerializeField] private float movementTimeout = 8f;

    [Header("真结局画面（按顺序）")]
    [SerializeField] private GameObject closedDiary;
    [SerializeField] private float closedDiaryDuration = 1f;
    [SerializeField] private Sprite[] endingPlayerSprites;
    [SerializeField] private float presentationDuration = 1f;

    [Header("结局对白")]
    [SerializeField] private DialogueData trueEnding;
    [SerializeField] private DialogueData badEnding;
    [SerializeField] private Canvas endingDialogueCanvas;
    [SerializeField] private int endingDialogueSortingOrder = 10000;

    private static bool endingTriggeredThisRuntime;

    private bool endingTakenOver;
    private bool transitionStarted;
    private bool endingDialogueFailed;
    private IDisposable movementLock;
    private IDisposable interactionLock;

    public bool HasTakenOverEnding => endingTakenOver || endingTriggeredThisRuntime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        endingTriggeredThisRuntime = false;
    }

    private void Start()
    {
        SetPresentationObjects(false);
        ResolvePlayerReference();
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnClueCollected += HandleClueCollected;
        ResolveIfFinalClueAlreadyCollected(GameManager.Instance);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnClueCollected -= HandleClueCollected;
        ReleaseLocks();
        ClueJournalUI.SetEndingDisabled(false);
    }

    private void Update()
    {
        if (HasTakenOverEnding || GameManager.Instance == null) return;
        if (GameManager.Instance.HasClue(finalClueId))
        {
            ResolveEnding(GameManager.Instance);
        }
    }

    private void ResolvePlayerReference()
    {
        if (player == null) player = FindFirstObjectByType<PlayerMovement2D>();
    }

    private void ResolveIfFinalClueAlreadyCollected(GameManager gm)
    {
        if (gm != null && !HasTakenOverEnding && !string.IsNullOrEmpty(finalClueId) && gm.HasClue(finalClueId))
        {
            ResolveEnding(gm);
        }
    }

    private void HandleClueCollected(ClueData clue)
    {
        if (HasTakenOverEnding || clue == null || clue.ClueId != finalClueId) return;
        ResolveEnding(GameManager.Instance);
    }

    private void ResolveEnding(GameManager gm)
    {
        if (gm == null || HasTakenOverEnding) return;
        endingTriggeredThisRuntime = true;
        endingTakenOver = true;
        movementLock = GameplayInputLock.AcquireMovementLock();
        interactionLock = GameplayInputLock.AcquireInteractionLock();
        ClueJournalUI.SetEndingDisabled(true);
        bool trueEndingBranch = forceTrueEndingForTesting || gm.HasCollectedAllPreRooftopClues();
        Debug.Log($"[EndingGate] 结局触发，真结局分支：{trueEndingBranch}", this);
        StartCoroutine(PlayEnding(trueEndingBranch, gm));
    }

    private IEnumerator PlayEnding(bool trueEndingBranch, GameManager gm)
    {
        try
        {
            while (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen) yield return null;
            while (InvestigationDirector.Instance != null && InvestigationDirector.Instance.IsPlayingFlashback) yield return null;

            if (trueEndingBranch)
            {
                gm.SetFlag("truth_revealed");
                yield return PlayTrueEndingSequence();
            }

            yield return FadeToBlack();
            ConfigureEndingDialogueCanvas();
            endingDialogueFailed = false;
            yield return PlayDialogue(trueEndingBranch ? trueEnding : badEnding);
            if (!endingDialogueFailed) ReturnToMenu(trueEndingBranch);
        }
        finally
        {
            ReleaseLocks();
            ClueJournalUI.SetEndingDisabled(false);
        }
    }

    private IEnumerator PlayTrueEndingSequence()
    {
        SetPresentationObjects(false);

        if (closedDiary != null)
        {
            closedDiary.SetActive(true);
            yield return new WaitForSeconds(Mathf.Max(0f, closedDiaryDuration));
            closedDiary.SetActive(false);
        }

        yield return PlayTrueEndingMovement();
        yield return new WaitForSeconds(1f);
        yield return PlayEndingSprites();
    }

    private IEnumerator PlayTrueEndingMovement()
    {
        if (player == null) yield break;

        Vector3 verticalTarget = downwardWaypoint != null
            ? downwardWaypoint.position
            : new Vector3(
                player.transform.position.x,
                leftTarget != null ? leftTarget.position.y : player.transform.position.y,
                player.transform.position.z);

        yield return player.MoveToEndingTarget(
            verticalTarget,
            Vector2.down,
            movementSpeedMultiplier,
            movementTimeout);

        if (leftTarget != null)
        {
            yield return player.MoveToEndingTarget(
                leftTarget.position,
                Vector2.left,
                movementSpeedMultiplier,
                movementTimeout);
        }

        player.SetEndingIdleLeft();
    }

    private IEnumerator PlayEndingSprites()
    {
        if (player == null || endingPlayerSprites == null) yield break;

        foreach (Sprite sprite in endingPlayerSprites)
        {
            if (sprite == null) continue;
            player.SetEndingSprite(sprite);
            yield return new WaitForSeconds(Mathf.Max(0f, presentationDuration));
        }
    }

    private IEnumerator FadeToBlack()
    {
        if (ScreenFader.Instance == null) yield break;
        bool complete = false;
        ScreenFader.Instance.FadeOutThen(() => complete = true);
        while (!complete) yield return null;
    }

    private IEnumerator PlayDialogue(DialogueData dialogue)
    {
        if (dialogue == null)
        {
            endingDialogueFailed = true;
            Debug.LogError("EndingGate 无法播放结局对白：对白资源未绑定。", this);
            yield break;
        }

        DialogueUIManager dialogueManager = DialogueUIManager.Instance;
        if (dialogueManager == null)
        {
            endingDialogueFailed = true;
            Debug.LogError("EndingGate 无法播放结局对白：场景中没有 DialogueUIManager。", this);
            yield break;
        }

        bool complete = false;
        if (!dialogueManager.StartDialogue(dialogue, () => complete = true))
        {
            endingDialogueFailed = true;
            Debug.LogError("EndingGate 无法播放结局对白：DialogueUIManager 启动对白失败。", this);
            yield break;
        }

        while (!complete) yield return null;
    }

    private void ConfigureEndingDialogueCanvas()
    {
        if (endingDialogueCanvas == null) return;

        RectTransform canvasRect = endingDialogueCanvas.GetComponent<RectTransform>();
        if (canvasRect != null && canvasRect.localScale == Vector3.zero)
        {
            canvasRect.localScale = Vector3.one;
        }

        endingDialogueCanvas.gameObject.SetActive(true);
        endingDialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        endingDialogueCanvas.worldCamera = null;
        endingDialogueCanvas.overrideSorting = true;
        endingDialogueCanvas.sortingOrder = endingDialogueSortingOrder;
    }

    private void SetPresentationObjects(bool active)
    {
        if (closedDiary != null) closedDiary.SetActive(active);
    }

    private void ReturnToMenu(bool useWhiteTransition)
    {
        if (transitionStarted) return;
        transitionStarted = true;
        ReleaseLocks();
        ClueJournalUI.SetEndingDisabled(false);

        if (ScreenFader.Instance != null)
        {
            Action loadMenu = () => SceneManager.LoadScene(menuSceneIndex);
            if (useWhiteTransition)
            {
                ScreenFader.Instance.FadeToWhiteThen(loadMenu);
            }
            else
            {
                ScreenFader.Instance.FadeOutThen(loadMenu);
            }
        }
        else
        {
            SceneManager.LoadScene(menuSceneIndex);
        }
    }

    private void ReleaseLocks()
    {
        movementLock?.Dispose();
        interactionLock?.Dispose();
        movementLock = null;
        interactionLock = null;
    }
}