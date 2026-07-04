using System.Collections;
using UnityEngine;

/// <summary>
/// 场景开场白：进入场景时自动播放一次「前提开头」独白（每周目只播一次，
/// 通过 flag "intro_<sceneId>" 记录，存档保存）。每个游戏场景放一个。
/// </summary>
public class SceneIntro : MonoBehaviour
{
    [Header("场景标识（与 SceneClueTracker 的 sceneId 一致）")]
    [SerializeField] private string sceneId;

    [Header("开场独白")]
    [SerializeField] private DialogueData introDialogue;
    [SerializeField] private float startDelay = 0.6f;

    private string IntroFlag => $"intro_{sceneId}";

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(startDelay);

        // 等入场黑幕渐亮结束，避免玩家在黑屏里错过对话开头
        while (ScreenFader.IsFading)
        {
            yield return null;
        }

        GameManager gm = GameManager.Instance;
        if (gm == null || introDialogue == null || gm.HasFlag(IntroFlag))
        {
            yield break;
        }

        while (DialogueUIManager.Instance == null || !DialogueUIManager.Instance.CanOpenDialogue)
        {
            yield return null;
        }

        gm.SetFlag(IntroFlag);
        DialogueUIManager.Instance.StartDialogue(introDialogue);
    }
}
