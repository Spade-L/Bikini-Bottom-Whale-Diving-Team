using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局背景音乐管理器（自动创建，跨场景常驻，不需要在场景里手动摆放）。
/// 每个场景放一个 SceneMusic 组件配置本场景的音乐列表，进入场景时调用
/// MusicManager.Instance.PlayPlaylist(...)：
/// - 列表内随机播放（不会连续重复同一首，除非列表只有一首）
/// - 切换列表时旧音乐淡出、新音乐淡入
/// - 相同列表重复设置不会打断当前播放（过门回到同类场景时音乐无缝延续）
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private AudioSource source;
    private AudioClip[] playlist;
    private int lastIndex = -1;
    private float targetVolume = 1f;
    private float fadeDuration = 1f;
    private Coroutine transitionRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            new GameObject("MusicManager").AddComponent<MusicManager>();
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
        source.loop = false;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    private void Update()
    {
        // 当前曲目播完且没有在转场中 → 随机下一首
        if (transitionRoutine == null && playlist != null && playlist.Length > 0
            && !source.isPlaying && Application.isFocused)
        {
            PlayNextRandom();
        }
    }

    /// <summary>
    /// 设置并播放一个音乐列表。clips 为空 = 淡出停止音乐。
    /// 与当前列表内容相同时不打断播放，只更新音量/淡入淡出参数。
    /// </summary>
    public void PlayPlaylist(AudioClip[] clips, float volume = 1f, float fade = 1f)
    {
        targetVolume = Mathf.Clamp01(volume);
        fadeDuration = Mathf.Max(0f, fade);

        if (IsSamePlaylist(clips))
        {
            // 同一列表：只调整音量（用于两个场景共用一套 BGM）
            if (transitionRoutine == null && source.isPlaying)
            {
                source.volume = targetVolume;
            }
            return;
        }

        playlist = clips;
        lastIndex = -1;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(CrossFadeToNewPlaylist());
    }

    /// <summary>淡出并停止当前音乐。</summary>
    public void StopMusic(float fade = 1f)
    {
        fadeDuration = Mathf.Max(0f, fade);
        playlist = null;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(CrossFadeToNewPlaylist());
    }

    private bool IsSamePlaylist(AudioClip[] clips)
    {
        if (playlist == null || clips == null || playlist.Length != clips.Length)
        {
            return playlist == null && (clips == null || clips.Length == 0);
        }

        for (int i = 0; i < clips.Length; i++)
        {
            if (playlist[i] != clips[i])
            {
                return false;
            }
        }

        return true;
    }

    private IEnumerator CrossFadeToNewPlaylist()
    {
        // 旧音乐淡出
        if (source.isPlaying && fadeDuration > 0f)
        {
            float startVolume = source.volume;
            for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
                yield return null;
            }
        }

        source.Stop();
        source.volume = targetVolume;

        // 新列表随机起播 + 淡入
        if (playlist != null && playlist.Length > 0)
        {
            PlayNextRandom();

            if (fadeDuration > 0f)
            {
                source.volume = 0f;
                for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
                {
                    source.volume = Mathf.Lerp(0f, targetVolume, t / fadeDuration);
                    yield return null;
                }
                source.volume = targetVolume;
            }
        }

        transitionRoutine = null;
    }

    private void PlayNextRandom()
    {
        // 收集有效曲目下标（跳过列表里的空槽）
        List<int> candidates = new List<int>();
        for (int i = 0; i < playlist.Length; i++)
        {
            if (playlist[i] != null && (playlist.Length == 1 || i != lastIndex))
            {
                candidates.Add(i);
            }
        }

        if (candidates.Count == 0)
        {
            // 只剩上一首可用（如列表里其他槽全空）
            if (lastIndex >= 0 && lastIndex < playlist.Length && playlist[lastIndex] != null)
            {
                candidates.Add(lastIndex);
            }
            else
            {
                return;
            }
        }

        lastIndex = candidates[Random.Range(0, candidates.Count)];
        source.clip = playlist[lastIndex];
        source.Play();
    }
}
