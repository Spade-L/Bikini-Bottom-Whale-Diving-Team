using UnityEngine;

// 提供跨场景的一次性音效播放
public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    // 专用 2D 声源；PlayOneShot 会在同一声源上叠加短音效
    private AudioSource source;

    // 在场景加载前创建常驻管理器
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
// AutoCreate 缺少引用时提前结束
        if (Instance == null)
        {
// 添加所需组件
            new GameObject("SfxManager").AddComponent<SfxManager>();
        }
    }

    // 初始化组件引用和运行状态
    private void Awake()
    {
        // 自动创建或场景预置重叠时，仅保留最早建立的播放器
        if (Instance != null && Instance != this)
        {
// 清理当前对象
            Destroy(gameObject);
            return;
        }

// 同步 Awake 的状态
        Instance = this;
        DontDestroyOnLoad(gameObject);

// 同步 Awake 的内部状态
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
// 同步 Awake 的内部状态（Awake）
        source.loop = false;
        source.spatialBlend = 0f;
    }

// 播放 Play 对应演出
    public void Play(AudioClip clip, float volume = 1f)
    {
// 检查 Play 的前置条件
        if (clip != null)
        {
// 设置 Play 的配置数值
            float globalVolume = SettingsManager.Instance == null ? 1f : SettingsManager.Instance.SfxVolume;
            source.PlayOneShot(clip, Mathf.Clamp01(volume * globalVolume));
        }
    }
}
