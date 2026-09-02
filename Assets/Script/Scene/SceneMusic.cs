using UnityEngine;

/// <summary>
/// 场景背景音乐配置。每个场景放一个空物体挂上，把本场景的 BGM 拖进列表，
/// 进入场景时自动交给 MusicManager 随机播放。
/// 相邻场景配置相同列表时音乐不会中断（无缝延续）。
/// </summary>
public class SceneMusic : MonoBehaviour
{
    // 本场景可供全局音乐管理器随机播放的曲目。
    [Header("本场景背景音乐列表（随机循环播放）")]
    // 相邻场景使用同一列表时可维持播放连续性。
    [SerializeField] private AudioClip[] musicClips;

    // 以下参数统一传给全局音乐管理器。
    [Header("播放设置")]
    // 控制播放列表的目标音量。
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.8f;
    [Tooltip("切换音乐时的淡入淡出秒数")]
    // 停止或切换音乐所用的淡化时长。
    [SerializeField] private float fadeDuration = 1f;
    [Tooltip("勾选 = 本场景静音（淡出上个场景的音乐）")]
    // 勾选时忽略曲目列表并停止音乐。
    [SerializeField] private bool silence = false;

    private void Start()
    {
        // 音乐管理器不存在时不执行场景音频配置。
        if (MusicManager.Instance == null)
        {
            return;
        }

        // 静音或未配置曲目时，淡出当前正在播放的音乐。
        if (silence || musicClips == null || musicClips.Length == 0)
        {
            MusicManager.Instance.StopMusic(fadeDuration);
        }
        else
        {
            // 将本场景曲目列表及播放参数交给全局管理器。
            MusicManager.Instance.PlayPlaylist(musicClips, volume, fadeDuration);
        }
    }
}
