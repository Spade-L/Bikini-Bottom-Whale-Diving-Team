using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// level3 门前一次性演出。使用独立触发器，依次播放 sps0 与 sps，完成后显示手印和眼睛。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Level3DoorSequence : MonoBehaviour
{
    [Serializable]
    private class SequenceStep
    {
        public string label;
        public GameObject root;
        public Animator animator;
        public string stateName;
        [Tooltip("此步骤是否需要先启用 hand 与 eye 供动画绑定；完成 Flag 仍只在全部步骤结束后写入。")]
        public bool showResultObjectsDuringStep;
        [Tooltip("步骤结束后是否隐藏 root。结果对象位于 root 子级时应关闭此项。")]
        public bool hideRootAfterStep = true;
        [Min(0.01f)] public float duration = 1f;
    }

    [Header("演出步骤（严格按数组顺序）")]
    [SerializeField] private SequenceStep[] steps;

    [Header("完成后显示")]
    [SerializeField] private GameObject hand;
    [SerializeField] private GameObject eye;
    [Tooltip("仅供动画绑定使用；演出完成或中断后隐藏。")]
    [SerializeField] private GameObject[] sequenceOnlyObjects;
    [SerializeField] private string completedFlag = "level3_door_sequence_done";

    [Header("触发")]
    [SerializeField] private string playerTag = "Player";

    private Coroutine sequenceCoroutine;
    private IDisposable movementLease;
    private IDisposable interactionLease;
    private bool completed;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        ApplySavedState();
    }

    private void Start()
    {
        ApplySavedState();
    }

    private void OnEnable()
    {
        ApplySavedState();
    }

    private void OnDisable()
    {
        InterruptSequence();
    }

    private void OnDestroy()
    {
        InterruptSequence();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!completed && sequenceCoroutine == null && other.CompareTag(playerTag))
        {
            sequenceCoroutine = StartCoroutine(PlaySequence());
        }
    }

    private IEnumerator PlaySequence()
    {
        if (!HasValidSteps())
        {
            Debug.LogError("[Level3DoorSequence] 必须按顺序配置且仅配置 sps0、sps 两个有效步骤，演出未启动。", this);
            sequenceCoroutine = null;
            SetAllStepsVisible(false);
            SetResultObjectsVisible(false);
            yield break;
        }

        movementLease = GameplayInputLock.AcquireMovementLock();
        interactionLease = GameplayInputLock.AcquireInteractionLock();

        SetResultObjectsVisible(false);
        SetAllStepsVisible(false);

        if (steps != null)
        {
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
                else
                {
                    Debug.LogWarning($"[Level3DoorSequence] 步骤 {step.label} 未配置可播放的 Animator 状态，将仅等待配置时长。", this);
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
        }

        completed = true;
        SetSequenceOnlyObjectsVisible(false);
        SetResultObjectsVisible(true);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetFlag(completedFlag);
        }
        else
        {
            completed = false;
            SetResultObjectsVisible(false);
            Debug.LogWarning("[Level3DoorSequence] GameManager 不存在，演出完成状态未保存。", this);
        }

        sequenceCoroutine = null;
        ReleaseLocks();
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
            if (step == null
                || !string.Equals(step.label, expectedLabel, StringComparison.Ordinal)
                || step.root == null
                || step.duration < 0.01f)
            {
                return false;
            }
        }

        return true;
    }

    private void ApplySavedState()
    {
        completed = GameManager.Instance != null
            && GameManager.Instance.HasFlag(completedFlag);

        if (completed)
        {
            SetAllStepsVisible(false);
            SetSequenceOnlyObjectsVisible(false);
            SetResultObjectsVisible(true);
        }
        else if (sequenceCoroutine == null)
        {
            SetAllStepsVisible(false);
            SetSequenceOnlyObjectsVisible(false);
            SetResultObjectsVisible(false);
        }
    }

    private void InterruptSequence()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        if (!completed)
        {
            SetAllStepsVisible(false);
            SetSequenceOnlyObjectsVisible(false);
            SetResultObjectsVisible(false);
        }

        ReleaseLocks();
    }

    private void SetAllStepsVisible(bool visible)
    {
        if (steps == null)
        {
            return;
        }

        foreach (SequenceStep step in steps)
        {
            if (step != null && step.root != null)
            {
                step.root.SetActive(visible);
            }
        }
    }

    private void SetSequenceOnlyObjectsVisible(bool visible)
    {
        if (sequenceOnlyObjects == null)
        {
            return;
        }

        foreach (GameObject sequenceObject in sequenceOnlyObjects)
        {
            if (sequenceObject != null)
            {
                sequenceObject.SetActive(visible);
            }
        }
    }

    private void SetResultObjectsVisible(bool visible)
    {
        if (hand != null)
        {
            hand.SetActive(visible);
        }

        if (eye != null)
        {
            eye.SetActive(visible);
        }
    }

    private void ReleaseLocks()
    {
        interactionLease?.Dispose();
        interactionLease = null;
        movementLease?.Dispose();
        movementLease = null;
    }
}
