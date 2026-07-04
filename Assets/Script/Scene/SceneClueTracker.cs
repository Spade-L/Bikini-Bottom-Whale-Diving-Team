using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 场景寻踪进度：登记本场景的关键线索，UI 显示“寻踪进度：2/4”。
/// 集齐后播放通关演出：影子出现 → 黑幕 → 场景变化（设置 Flag，门由 Flag 打开）。
/// 每个游戏场景放一个。
/// </summary>
public class SceneClueTracker : MonoBehaviour
{
    [Header("场景标识（用于生成通关 Flag: scene_cleared_<sceneId>）")]
    [SerializeField] private string sceneId;

    [Header("本场景的关键线索（3-4 个）")]
    [SerializeField] private ClueData[] keyClues;

    [Header("进度 UI（屏幕角落）")]
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private string progressFormat = "寻踪进度：{0}/{1}";

    [Header("通关演出")]
    [Tooltip("“哥哥”的影子（场景里预放好，默认隐藏）")]
    [SerializeField] private GameObject brotherShadow;
    [Tooltip("影子展示秒数")]
    [SerializeField] private float shadowDuration = 2f;
    [Tooltip("全屏黑幕 CanvasGroup")]
    [SerializeField] private CanvasGroup blackout;
    [SerializeField] private float blackoutFadeDuration = 0.6f;
    [SerializeField] private float blackoutHoldDuration = 1f;
    [Tooltip("黑幕后播放的独白（可空）")]
    [SerializeField] private DialogueData clearMonologue;

    private string ClearedFlag => $"scene_cleared_{sceneId}";

    private void Start()
    {
        if (brotherShadow != null)
        {
            brotherShadow.SetActive(false);
        }

        if (blackout != null)
        {
            blackout.alpha = 0f;
            blackout.gameObject.SetActive(false);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnClueCollected += HandleClueCollected;
        }

        RefreshProgressUI();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnClueCollected -= HandleClueCollected;
        }
    }

    private int CountCollected()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || keyClues == null)
        {
            return 0;
        }

        int count = 0;
        foreach (ClueData clue in keyClues)
        {
            if (clue != null && gm.HasClue(clue.ClueId))
            {
                count++;
            }
        }

        return count;
    }

    private void RefreshProgressUI()
    {
        if (progressText != null && keyClues != null)
        {
            progressText.text = string.Format(progressFormat, CountCollected(), keyClues.Length);
        }
    }

    private void HandleClueCollected(ClueData _)
    {
        RefreshProgressUI();

        GameManager gm = GameManager.Instance;
        if (gm == null || gm.HasFlag(ClearedFlag))
        {
            return;
        }

        if (keyClues != null && keyClues.Length > 0 && CountCollected() >= keyClues.Length)
        {
            StartCoroutine(PlayClearSequence());
        }
    }

    private IEnumerator PlayClearSequence()
    {
        // 等收尾对话（最后一条线索的调查对话）关闭
        while (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
        {
            yield return null;
        }

        // 等回溯闪回演出结束，避免叠加
        while (InvestigationDirector.Instance != null && InvestigationDirector.Instance.IsPlayingFlashback)
        {
            yield return null;
        }

        // 影子出现
        if (brotherShadow != null)
        {
            brotherShadow.SetActive(true);
            yield return new WaitForSeconds(shadowDuration);
        }

        // 黑幕
        if (blackout != null)
        {
            blackout.gameObject.SetActive(true);
            yield return FadeBlackout(0f, 1f);

            if (brotherShadow != null)
            {
                brotherShadow.SetActive(false);
            }

            yield return new WaitForSeconds(blackoutHoldDuration);

            // 通关 Flag 在黑幕中设置——场景门/物件在黑幕里完成变化
            GameManager.Instance.SetFlag(ClearedFlag);

            yield return FadeBlackout(1f, 0f);
            blackout.gameObject.SetActive(false);
        }
        else
        {
            GameManager.Instance.SetFlag(ClearedFlag);
        }

        if (clearMonologue != null && DialogueUIManager.Instance != null)
        {
            DialogueUIManager.Instance.StartDialogue(clearMonologue);
        }
    }

    private IEnumerator FadeBlackout(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < blackoutFadeDuration)
        {
            elapsed += Time.deltaTime;
            blackout.alpha = Mathf.Lerp(from, to, elapsed / blackoutFadeDuration);
            yield return null;
        }

        blackout.alpha = to;
    }
}
