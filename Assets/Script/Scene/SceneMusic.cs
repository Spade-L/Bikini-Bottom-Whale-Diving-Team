using UnityEngine;

/// <summary>
/// 场景背景音乐配置。每个场景放一个空物体挂上，把本场景的 BGM 拖进列表，
/// 进入场景时自动交给 MusicManager 随机播放。
/// 相邻场景配置相同列表时音乐不会中断（无缝延续）。
/// </summary>
public class SceneMusic : MonoBehaviour
{
    [Header("本场景背景音乐列表（随机循环播放）")]
    [SerializeField] private AudioClip[] musicClips;

    [Header("播放设置")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.8f;
    [Tooltip("切换音乐时的淡入淡出秒数")]
    [SerializeField] private float fadeDuration = 1f;
    [Tooltip("勾选 = 本场景静音（淡出上个场景的音乐）")]
    [SerializeField] private bool silence = false;

    private void Start()
    {
        if (MusicManager.Instance == null)
        {
            return;
        }

        if (silence || musicClips == null || musicClips.Length == 0)
        {
            MusicManager.Instance.StopMusic(fadeDuration);
        }
        else
        {
            MusicManager.Instance.PlayPlaylist(musicClips, volume, fadeDuration);
        }
    }
}
