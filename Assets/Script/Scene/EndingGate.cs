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
    [Header("触发结局的最后一条线索 Id")]
    [SerializeField] private string finalClueId = "roof_diary_final";

    [Header("坏结局对话（探索不完整时）")]
    [SerializeField] private DialogueData badEnding;

    [Header("坏结局播完回到的主菜单场景序号")]
    [SerializeField] private int menuSceneIndex = 0;

    private bool resolved;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnClueCollected += HandleClueCollected;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnClueCollected -= HandleClueCollected;
        }
    }

    private void HandleClueCollected(ClueData clue)
    {
        if (resolved || clue == null || clue.ClueId != finalClueId)
        {
            return;
        }

        resolved = true;

        if (GameManager.Instance.HasCollectedAllPreRooftopClues())
        {
            // 真结局：设置真相 flag，线索日志切换真相文本；
            // 天台的 SceneClueTracker（勾了 Require Truth Revealed）随后播通关演出。
            GameManager.Instance.SetFlag("truth_revealed");
        }
        else
        {
            // 坏结局：等最后一页的调查对话关闭后播独白，播完回主菜单。
            StartCoroutine(PlayBadEnding());
        }
    }

    private System.Collections.IEnumerator PlayBadEnding()
    {
        while (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
        {
            yield return null;
        }

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
