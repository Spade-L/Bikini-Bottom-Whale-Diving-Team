using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 天台结局判定。放在天台场景一个空物体上。
/// 玩家翻开日记最后一页（收集到 finalClueId 线索）时接管演出，二选一：
/// - 真结局：前五关全部关键线索都查过 → 设置 truth_revealed，交给 SceneClueTracker 播通关演出。
/// - 坏结局：有遗漏 → 播「与自己的对话」独白，播完淡出回主菜单。
/// </summary>
public class EndingGate : MonoBehaviour
{
    // 用于识别触发结局判定的唯一线索。
    [Header("触发结局的最后一条线索 Id")]
    // 默认对应天台日记的最终页。
    [SerializeField] private string finalClueId = "roof_diary_final";

    // 探索不足时播放的结局独白资源。
    [Header("坏结局对话（探索不完整时）")]
    // 可留空以跳过独白并直接返回。
    [SerializeField] private DialogueData badEnding;

    // 坏结局结束后的场景加载目标。
    [Header("坏结局播完回到的主菜单场景序号")]
    // 使用 Build Settings 中的场景索引。
    [SerializeField] private int menuSceneIndex = 0;

    // 验证本次事件是否正是最终线索。
    // 防止同一条最终线索的重复事件多次触发结局。
    private bool resolved;

    // Unity 生命周期入口：注册所需事件。
    private void Start()
    {
        // 仅在全局管理器已创建时订阅。
        // 监听线索收集事件，等待日记最终页被调查。
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnClueCollected += HandleClueCollected;
            ResolveIfFinalClueAlreadyCollected(GameManager.Instance);
        }
    }

    // Unity 生命周期出口：解除事件订阅。
    private void OnDestroy()
    {
        // 仅在管理器仍可访问时尝试注销。
        // 场景销毁前解除订阅，避免遗留回调。
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnClueCollected -= HandleClueCollected;
        }
    }

    private void ResolveIfFinalClueAlreadyCollected(GameManager gm)
    {
        if (gm == null || resolved || string.IsNullOrEmpty(finalClueId)
            || !gm.HasClue(finalClueId))
        {
            return;
        }

        ResolveEnding(gm);
    }

    private void ResolveEnding(GameManager gm)
    {
        if (gm == null || resolved)
        {
            return;
        }

        resolved = true;

        // 真结局需要此前全部关键线索均已收集。
        if (gm.HasCollectedAllPreRooftopClues())
        {
            gm.SetFlag("truth_revealed");
        }
        else
        {
            StartCoroutine(PlayBadEnding());
        }
    }

    private void HandleClueCollected(ClueData clue)
    {
        // 非目标线索、空数据或已判定时均不处理。
        if (resolved || clue == null || clue.ClueId != finalClueId)
        {
            return;
        }

        ResolveEnding(GameManager.Instance);
    }

    private System.Collections.IEnumerator PlayBadEnding()
    {
        // 先让调查日记的原始对话完整结束。
        while (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
        {
            yield return null;
        }

        // 有独白 UI 时播放并在回调中返回；否则直接返回。
        if (badEnding != null && DialogueUIManager.Instance != null)
        {
            DialogueUIManager.Instance.StartDialogue(badEnding, ReturnToMenu);
        }
        else
        {
            ReturnToMenu();
        }
    }

    private void ReturnToMenu()
    {
        // 优先使用淡出组件，缺失时仍保证能切回菜单。
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeOutThen(() => SceneManager.LoadScene(menuSceneIndex));
        }
        else
        {
            SceneManager.LoadScene(menuSceneIndex);
        }
    }
}
