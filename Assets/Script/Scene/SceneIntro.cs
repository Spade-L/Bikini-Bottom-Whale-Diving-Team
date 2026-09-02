using System.Collections;
using UnityEngine;

/// <summary>
/// 场景开场白：进入场景时自动播放一次「前提开头」独白（每周目只播一次，
/// 通过 flag "intro_<sceneId>" 记录，存档保存）。每个游戏场景放一个。
/// </summary>
public class SceneIntro : MonoBehaviour
{
    // 此标识必须与对应线索追踪器保持一致。
    [Header("场景标识（与 SceneClueTracker 的 sceneId 一致）")]
    // 用作一次性开场 Flag 的后缀。
    [SerializeField] private string sceneId;

    // 进入场景后要自动播放的独白资源。
    [Header("开场独白")]
    // 未配置时本组件不会播放任何内容。
    [SerializeField] private DialogueData introDialogue;
    // 用于避开场景刚加载完成的时刻。
    [SerializeField] private float startDelay = 0.6f;

    // 用场景标识构造存档 Flag，区分各场景的首次开场。
    private string IntroFlag => $"intro_{sceneId}";

    private IEnumerator Start()
    {
        // 留出场景加载后的初始缓冲时间。
        yield return new WaitForSeconds(startDelay);

        // 等入场黑幕渐亮结束，避免玩家在黑屏里错过对话开头
        while (ScreenFader.IsFading)
        {
            yield return null;
        }

        // 缓存管理器引用，并在缺少配置或已经播放时退出。
        GameManager gm = GameManager.Instance;
        if (gm == null || introDialogue == null || gm.HasFlag(IntroFlag))
        {
            yield break;
        }

        // 等待对话系统就绪且允许打开新的对话。
        while (DialogueUIManager.Instance == null || !DialogueUIManager.Instance.CanOpenDialogue)
        {
            yield return null;
        }

        // 在播放前写入标记，确保本周目只触发一次。
        gm.SetFlag(IntroFlag);
        DialogueUIManager.Instance.StartDialogue(introDialogue);
    }
}
