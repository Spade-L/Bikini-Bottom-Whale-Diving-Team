using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 调查系统导演：监听调查次数变化，触发阈值事件（回溯闪回、独白、封锁 Flag）。
/// 与 GameManager 同物体或单独挂都可，场景常驻（DontDestroyOnLoad 由 GameManager 承担，
/// 本组件建议放在每个场景的 UI Canvas 上，闪回 UI 引用场景内的即可）。
/// </summary>
public class InvestigationDirector : MonoBehaviour
{
    public static InvestigationDirector Instance { get; private set; }

    [Header("事件表")]
    [SerializeField] private InvestigationEventTable eventTable;

    [Header("闪回 UI（全屏覆盖）")]
    [SerializeField] private CanvasGroup flashbackOverlay;
    [SerializeField] private Image flashbackImage;
    [SerializeField] private TMP_Text flashbackCaption;
    [SerializeField] private float fadeDuration = 0.35f;

    private readonly Queue<InvestigationEventTable.ThresholdEvent> pendingEvents
        = new Queue<InvestigationEventTable.ThresholdEvent>();

    private bool isPlayingEvent;

    public bool IsPlayingFlashback => isPlayingEvent;

    private void Awake()
    {
        Instance = this;

        if (flashbackOverlay != null)
        {
            flashbackOverlay.alpha = 0f;
            flashbackOverlay.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnInvestigationCountChanged += HandleCountChanged;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnInvestigationCountChanged -= HandleCountChanged;
        }
    }

    private void HandleCountChanged(int count)
    {
        if (eventTable == null || eventTable.events == null)
        {
            return;
        }

        GameManager gm = GameManager.Instance;

        foreach (var evt in eventTable.events)
        {
            if (count < evt.threshold)
            {
                continue;
            }

            // "inv_reached_N" flag 同时充当"已触发"标记与剧情条件
            string reachedFlag = $"inv_reached_{evt.threshold}";
            if (gm.HasFlag(reachedFlag))
            {
                continue;
            }

            gm.SetFlag(reachedFlag);

            if (evt.setFlags != null)
            {
                foreach (string flag in evt.setFlags)
                {
                    gm.SetFlag(flag);
                }
            }

            pendingEvents.Enqueue(evt);
        }

        if (!isPlayingEvent && pendingEvents.Count > 0)
        {
            StartCoroutine(PlayPendingEvents());
        }
    }

    private IEnumerator PlayPendingEvents()
    {
        isPlayingEvent = true;

        while (pendingEvents.Count > 0)
        {
            var evt = pendingEvents.Dequeue();

            // 等当前对话关闭再演出，避免 UI 叠在一起
            while (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
            {
                yield return null;
            }

            if (evt.flashback != null && evt.flashback.HasContent)
            {
                yield return PlayFlashback(evt.flashback);
            }

            if (evt.monologue != null && DialogueUIManager.Instance != null)
            {
                bool done = false;
                DialogueUIManager.Instance.StartDialogue(evt.monologue, () => done = true);
                while (!done)
                {
                    yield return null;
                }
            }
        }

        isPlayingEvent = false;
    }

    private IEnumerator PlayFlashback(FlashbackSequence flashback)
    {
        if (flashbackOverlay == null || flashbackImage == null)
        {
            Debug.LogWarning("[InvestigationDirector] 闪回 UI 未配置，跳过演出。");
            yield break;
        }

        flashbackOverlay.gameObject.SetActive(true);

        if (flashbackCaption != null)
        {
            flashbackCaption.text = TextTokens.Resolve(flashback.caption ?? string.Empty);
        }

        foreach (Sprite sprite in flashback.images)
        {
            flashbackImage.sprite = sprite;

            yield return Fade(0f, 1f);
            yield return new WaitForSeconds(flashback.secondsPerImage);
            yield return Fade(1f, 0f);
        }

        flashbackOverlay.gameObject.SetActive(false);
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            flashbackOverlay.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        flashbackOverlay.alpha = to;
    }
}
