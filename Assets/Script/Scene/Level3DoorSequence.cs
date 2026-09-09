using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Level3DoorSequence : MonoBehaviour, IInteractionPromptSource
{
    [Serializable]
    private class SequenceStep
    {
        public string label;
        public GameObject root;
        public Animator animator;
        public string stateName;
        public bool showResultObjectsDuringStep;
        public bool hideRootAfterStep = true;
        [Min(0.01f)] public float duration = 1f;
    }

    [Header("演出步骤（严格为 sps0 → sps）")]
    [SerializeField] private SequenceStep[] steps;

    [Header("六个便利店调查条件")]
    [Tooltip("填写 ClueData.ClueId；组件检查 investigated_<id> Flag，不改变核心线索列表。")]
    [SerializeField] private string[] requiredInvestigationIds =
    {
        "store_note",
        "store_poster",
        "store_vegetables",
        "store_handprint",
        "store_fruit",
        "store_toy"
    };

    [Header("演出对象")]
    [SerializeField] private GameObject hand;
    [SerializeField] private GameObject eye;
    [SerializeField] private GameObject redToyGameObject;
    [SerializeField] private GameObject[] sequenceOnlyObjects;

    [Header("演出对白")]
    [SerializeField] private DialogueData revealDialogue;
    [SerializeField] private DialogueData missingToyDialogue;
    [SerializeField] private DialogueData deliveryDialogue;
    [SerializeField] private DialogueData disappearanceDialogue;
    [SerializeField] private DialogueData exitReadyDialogue;

    [Header("音效（没有匹配资产时保持为空）")]
    [SerializeField] private AudioClip fallingBreakingSound;
    [Range(0f, 1f)] [SerializeField] private float fallingBreakingVolume = 1f;

    [Header("存档 Flag")]
    [SerializeField] private string completedFlag = "level3_door_sequence_done";
    [SerializeField] private string resolvedFlag = "level3_store_shadow_resolved";
    [SerializeField] private string toyDeliveredFlag = "level3_store_toy_delivered";
    [SerializeField] private string redToyPickupFlag = "picked_store_red_toy";

    [Header("触发与交互")]
    [SerializeField] private GameObject interactionUI;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactionKey = KeyCode.F;

    private Coroutine sequenceCoroutine;
    private IDisposable movementLease;
    private IDisposable interactionLease;
    private bool playerInRange;
    private bool inputSuppressed;
    private bool subscribedToGameManager;
    private bool completed;
    private bool resolved;
    private bool exclusiveModeActive;

    public bool IsInteractionPromptEligible
    {
        get
        {
            if (!isActiveAndEnabled || !playerInRange || sequenceCoroutine != null
                || inputSuppressed || resolved)
            {
                return false;
            }

            return !completed ? HasAllInvestigations() : !resolved;
        }
    }

    private string InvestigationFlag(string clueId) => $"investigated_{clueId}";

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        ApplySavedState();
        HidePrompt();
    }

    private void Start()
    {
        ApplySavedState();
    }

    private void OnEnable()
    {
        ApplySavedState();
        SubscribeGameManager(true);
    }

    private void OnDisable()
    {
        PlayerInteractionPromptController.UnregisterSource(this);
        SubscribeGameManager(false);
        InterruptSequence();
        CluePickup2D.ClearExclusiveInteractionTarget(redToyGameObject);
        HidePrompt();
    }

    private void OnDestroy()
    {
        PlayerInteractionPromptController.UnregisterSource(this);
        SubscribeGameManager(false);
        InterruptSequence();
        CluePickup2D.ClearExclusiveInteractionTarget(redToyGameObject);
    }

    private void Update()
    {
        UpdateExclusiveInteractionMode();

        if (!subscribedToGameManager)
        {
            SubscribeGameManager(true);
        }

        if (GameplayInputLock.IsInteractionLocked)
        {
            inputSuppressed = true;
            HidePrompt();
            return;
        }

        if (inputSuppressed)
        {
            inputSuppressed = false;
            RefreshPrompt();
        }

        if (!playerInRange || sequenceCoroutine != null || !Input.GetKeyDown(interactionKey)
            || (DialogueUIManager.Instance != null && !DialogueUIManager.Instance.CanOpenDialogue))
        {
            return;
        }

        if (resolved)
        {
            CluePickup2D.ClearExclusiveInteractionTarget(redToyGameObject);
            return;
        }

        if (!completed)
        {
            if (HasAllInvestigations())
            {
                sequenceCoroutine = StartCoroutine(PlaySequence());
            }

            return;
        }

        if (!HasRedToyPickup())
        {
            CluePickup2D.SetExclusiveInteractionTarget(redToyGameObject);
            sequenceCoroutine = StartCoroutine(RemindMissingToy());
            return;
        }

        CluePickup2D.ClearExclusiveInteractionTarget(redToyGameObject);
        sequenceCoroutine = StartCoroutine(DeliverToy());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            PlayerInteractionPromptController.RegisterSource(this);
            PlayerInteractionPromptController.RefreshSource(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            PlayerInteractionPromptController.UnregisterSource(this);
        }
    }

    private IEnumerator PlaySequence()
    {
        HidePrompt();

        if (!HasAllInvestigations() || !HasValidSteps())
        {
            if (!HasValidSteps())
            {
                Debug.LogError("[Level3DoorSequence] 必须按顺序配置且仅配置 sps0、sps 两个有效步骤，演出未启动。", this);
            }

            sequenceCoroutine = null;
            RefreshPrompt();
            yield break;
        }

        movementLease = GameplayInputLock.AcquireMovementLock();
        interactionLease = GameplayInputLock.AcquireInteractionLock();
        SetAllStepsVisible(false);
        SetSequenceOnlyObjectsVisible(false);
        SetResultObjectsVisible(false);

        foreach (SequenceStep step in steps)
        {
            if (step == null)
            {
                continue;
            }

            if (step.root != null)
            {
                step.root.SetActive(true);
            }

            if (step.showResultObjectsDuringStep)
            {
                SetResultObjectsVisible(true);
            }

            if (step.animator != null && !string.IsNullOrEmpty(step.stateName))
            {
                step.animator.Play(step.stateName, 0, 0f);
                step.animator.Update(0f);
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, step.duration));

            if (step.hideRootAfterStep && step.root != null)
            {
                step.root.SetActive(false);
            }

            if (step.showResultObjectsDuringStep)
            {
                SetResultObjectsVisible(false);
            }
        }

        SetResultObjectsVisible(true);
        completed = true;
        SetSequenceOnlyObjectsVisible(false);
        SetStepRootVisible(0, false);
        SetStepRootVisible(1, true);
        GameManager.Instance?.SetFlag(completedFlag);

        yield return StartDialogueAndWait(revealDialogue);

        ReleaseLocks();
        sequenceCoroutine = null;
        RefreshPrompt();
    }

    private IEnumerator RemindMissingToy()
    {
        HidePrompt();
        movementLease = GameplayInputLock.AcquireMovementLock();
        interactionLease = GameplayInputLock.AcquireInteractionLock();

        yield return StartDialogueAndWait(missingToyDialogue);

        ReleaseLocks();
        sequenceCoroutine = null;
        RefreshPrompt();
    }

    private IEnumerator DeliverToy()
    {
        HidePrompt();
        movementLease = GameplayInputLock.AcquireMovementLock();
        interactionLease = GameplayInputLock.AcquireInteractionLock();

        yield return StartDialogueAndWait(deliveryDialogue);
        GameManager.Instance?.SetFlag(toyDeliveredFlag);
        yield return StartDialogueAndWait(disappearanceDialogue);

        if (SfxManager.Instance != null && fallingBreakingSound != null)
        {
            SfxManager.Instance.Play(fallingBreakingSound, fallingBreakingVolume);
        }

        SetResultObjectsVisible(false);
        SetStepRootVisible(1, false);
        yield return StartDialogueAndWait(exitReadyDialogue);

        GameManager.Instance?.SetFlag(resolvedFlag);
        resolved = true;
        ReleaseLocks();
        sequenceCoroutine = null;
        RefreshPrompt();
    }

    private IEnumerator StartDialogueAndWait(DialogueData dialogue)
    {
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0
            || DialogueUIManager.Instance == null)
        {
            yield break;
        }

        bool finished = false;
        DialogueUIManager.Instance.StartDialogue(dialogue, () => finished = true);
        while (!finished)
        {
            yield return null;
        }
    }

    private bool HasAllInvestigations()
    {
        if (GameManager.Instance == null || requiredInvestigationIds == null || requiredInvestigationIds.Length == 0)
        {
            return false;
        }

        foreach (string clueId in requiredInvestigationIds)
        {
            if (string.IsNullOrEmpty(clueId) || !GameManager.Instance.HasFlag(InvestigationFlag(clueId)))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasRedToyPickup()
    {
        return GameManager.Instance != null
            && !string.IsNullOrEmpty(redToyPickupFlag)
            && GameManager.Instance.HasFlag(redToyPickupFlag);
    }

    private bool HasValidSteps()
    {
        if (steps == null || steps.Length != 2)
        {
            return false;
        }

        for (int i = 0; i < steps.Length; i++)
        {
            SequenceStep step = steps[i];
            string expectedLabel = i == 0 ? "sps0" : "sps";
            if (step == null || !string.Equals(step.label, expectedLabel, StringComparison.Ordinal)
                || step.root == null || step.duration < 0.01f)
            {
                return false;
            }
        }

        return true;
    }

    private void ApplySavedState()
    {
        GameManager gm = GameManager.Instance;
        completed = gm != null && gm.HasFlag(completedFlag);
        resolved = gm != null && gm.HasFlag(resolvedFlag);

        SetAllStepsVisible(false);
        SetSequenceOnlyObjectsVisible(false);
        SetResultObjectsVisible(completed && !resolved);
        SetStepRootVisible(0, false);
        SetStepRootVisible(1, completed && !resolved);
        RefreshPrompt();
    }

    private void SubscribeGameManager(bool subscribe)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || subscribedToGameManager == subscribe)
        {
            return;
        }

        if (subscribe)
        {
            gm.OnFlagsChanged += HandleFlagsChanged;
        }
        else
        {
            gm.OnFlagsChanged -= HandleFlagsChanged;
        }

        subscribedToGameManager = subscribe;
    }

    private void HandleFlagsChanged()
    {
        ApplySavedState();
        UpdateExclusiveInteractionMode();
        PlayerInteractionPromptController.RefreshSource(this);
    }

    private void UpdateExclusiveInteractionMode()
    {
        bool shouldBeActive = redToyGameObject != null && completed && !resolved && !HasRedToyPickup();
        if (exclusiveModeActive == shouldBeActive)
        {
            return;
        }

        exclusiveModeActive = shouldBeActive;
        if (shouldBeActive)
        {
            CluePickup2D.SetExclusiveInteractionTarget(redToyGameObject);
        }
        else
        {
            CluePickup2D.ClearExclusiveInteractionTarget(redToyGameObject);
        }
    }

    private void RefreshPrompt()
    {
        PlayerInteractionPromptController.RefreshSource(this);
    }

    private void HidePrompt()
    {
        PlayerInteractionPromptController.RefreshSource(this);
    }

    private void InterruptSequence()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        ReleaseLocks();
        if (!completed || resolved)
        {
            SetAllStepsVisible(false);
            SetSequenceOnlyObjectsVisible(false);
        }

        if (resolved)
        {
            SetResultObjectsVisible(false);
        }
    }

    private void SetStepRootVisible(int index, bool visible)
    {
        if (steps == null || index < 0 || index >= steps.Length || steps[index] == null
            || steps[index].root == null)
        {
            return;
        }

        steps[index].root.SetActive(visible);
    }

    private void SetAllStepsVisible(bool visible)
    {
        if (steps == null) return;
        foreach (SequenceStep step in steps)
        {
            if (step != null && step.root != null) step.root.SetActive(visible);
        }
    }

    private void SetSequenceOnlyObjectsVisible(bool visible)
    {
        if (sequenceOnlyObjects == null) return;
        foreach (GameObject sequenceObject in sequenceOnlyObjects)
        {
            if (sequenceObject != null) sequenceObject.SetActive(visible);
        }
    }

    private void SetResultObjectsVisible(bool visible)
    {
        if (hand != null) hand.SetActive(visible);
        if (eye != null) eye.SetActive(visible);
    }

    private void ReleaseLocks()
    {
        interactionLease?.Dispose();
        interactionLease = null;
        movementLease?.Dispose();
        movementLease = null;
    }
}
