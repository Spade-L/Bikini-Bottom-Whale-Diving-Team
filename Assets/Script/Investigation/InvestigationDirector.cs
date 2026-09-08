using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

            string reachedFlag = $"inv_reached_{evt.threshold}";
            if (gm.HasFlag(reachedFlag))
            {
                continue;
            }

            // 批量写入相关标记，避免每个 Flag 都触发一次全场景刷新。
            gm.SetFlags(BuildFlags(evt));
            pendingEvents.Enqueue(evt);
        }

        if (!isPlayingEvent && pendingEvents.Count > 0)
        {
            StartCoroutine(PlayPendingEvents());
        }
    }

    private IEnumerable<string> BuildFlags(InvestigationEventTable.ThresholdEvent evt)
    {
        yield return $"inv_reached_{evt.threshold}";
        if (evt.setFlags == null)
        {
            yield break;
        }

        foreach (string flag in evt.setFlags)
        {
            yield return flag;
        }
    }

    private IEnumerator PlayPendingEvents()
    {
        isPlayingEvent = true;

        while (pendingEvents.Count > 0)
        {
            var evt = pendingEvents.Dequeue();

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
