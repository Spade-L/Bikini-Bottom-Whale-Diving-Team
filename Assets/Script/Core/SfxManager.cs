using UnityEngine;

public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    // 专用 2D 声源；PlayOneShot 会在同一声源上叠加短音效
    private AudioSource source;

    // 在首个场景前完成自动注册，场景无需预置管理器对象
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            new GameObject("SfxManager").AddComponent<SfxManager>();
        }
    }

    // 初始化为 2D、非循环声源；单例跨场景保留，重复自动创建时不接管播放
    private void Awake()
    {
        // 自动创建或场景预置重叠时，仅保留最早建立的播放器
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
    }

    public void Play(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
        {
            float globalVolume = SettingsManager.Instance == null ? 1f : SettingsManager.Instance.SfxVolume;
            source.PlayOneShot(clip, Mathf.Clamp01(volume * globalVolume));
        }
    }
}
