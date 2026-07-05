using UnityEngine;

/// <summary>
/// 全局音效播放器（自动创建，跨场景常驻，不需要在场景里摆放）。
/// 供 UI 点击等一次性音效使用：SfxManager.Instance.Play(clip, volume)。
/// 与 MusicManager（背景音乐）分开，互不影响。
/// </summary>
public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    private AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            new GameObject("SfxManager").AddComponent<SfxManager>();
        }
    }

    private void Awake()
    {
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

    /// <summary>播放一次性音效（叠加播放，不打断其他音效）。</summary>
    public void Play(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
        {
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }
}
