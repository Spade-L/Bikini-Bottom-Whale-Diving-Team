using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    // 读取初始依赖并同步首帧状态
    [Header("本场景背景音乐列表（随机循环播放）")]
    // 在 SceneMusic 中处理 Start
    [SerializeField] private AudioClip[] musicClips;

    // 在 SceneMusic 中处理 Start（SceneMusic 后续步骤）
    [Header("播放设置")]
    // 在 SceneMusic 中处理 Start（SceneMusic 后续步骤）（后续处理 2）
    [Range(0f, 1f)]
// 在 SceneMusic 中处理 Start（SceneMusic 后续步骤）（后续处理 3）
    [SerializeField] private float volume = 0.8f;
    [Tooltip("切换音乐时的淡入淡出秒数")]
    // 在 SceneMusic 中处理 Start（SceneMusic 后续步骤）（后续处理 4）
    [SerializeField] private float fadeDuration = 1f;
// 在 SceneMusic 中处理 Start（SceneMusic 后续步骤）（后续处理 5）
    [Tooltip("勾选 = 本场景静音（淡出上个场景的音乐）")]
    // 在 SceneMusic 中处理 Start（SceneMusic 后续步骤）（后续处理 6）
    [SerializeField] private bool silence = false;

    private void Start()
    {
        // 音乐管理器不存在时不执行场景音频配置
        if (MusicManager.Instance == null)
        {
            return;
        }

        // 静音或未配置曲目时，淡出当前正在播放的音乐
        if (silence || musicClips == null || musicClips.Length == 0)
        {
            MusicManager.Instance.StopMusic(fadeDuration);
        }
// 处理 Start 的备用分支
        else
        {
            // 将本场景曲目列表及播放参数交给全局管理器
            MusicManager.Instance.PlayPlaylist(musicClips, volume, fadeDuration);
        }
    }
}
