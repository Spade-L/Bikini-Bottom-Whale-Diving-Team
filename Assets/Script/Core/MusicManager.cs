using System.Collections;
using UnityEngine;

// 定义 MusicManager 类型
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private AudioSource source;
// 特殊 BGM 使用独立播放源
    private AudioSource interruptionSource;

// 保存 playlist 引用
    private AudioClip[] playlist;
    private int lastIndex = -1;
// 配置 targetVolume 数值
    private float targetVolume = 1f;
    private float baseVolume = 1f;
// 配置 fadeDuration 数值
    private float fadeDuration = 1f;
    private float interruptionBaseVolume = 1f;
    private float interruptionTargetVolume = 1f;

    private Coroutine transitionRoutine;
    private Coroutine interruptionRoutine;
// 记录特殊 BGM 的占用者
    private object interruptionOwner;
    private bool interruptionActive;
    private bool interruptionRestoring;
    private bool normalWasPlayingBeforeInterruption;
    private float normalVolumeBeforeInterruption = 1f;

// 运行前初始化状态
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
// 空引用时直接退出
        if (Instance == null) new GameObject("MusicManager").AddComponent<MusicManager>();
    }

// 定义 Awake 方法
    private void Awake()
    {
// 判断当前条件
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
// 执行 DontDestroyOnLoad
        DontDestroyOnLoad(gameObject);

        source = gameObject.AddComponent<AudioSource>();
// 普通列表播放结束后由 Update 继续轮播
        source.loop = false;
        source.playOnAwake = false;
// 更新当前状态
        source.spatialBlend = 0f;

        interruptionSource = gameObject.AddComponent<AudioSource>();
        interruptionSource.loop = true;
        interruptionSource.playOnAwake = false;
        interruptionSource.spatialBlend = 0f;
    }

// 定义 Update 方法
    private void Update()
    {
// 特殊音乐期间不启动普通播放列表
        if (!interruptionActive && transitionRoutine == null && playlist != null && playlist.Length > 0
            && !source.isPlaying && Application.isFocused)
        {
            PlayNextRandom();
        }
    }

// 定义 PlayPlaylist 方法
    public void PlayPlaylist(AudioClip[] clips, float volume = 1f, float fade = 1f)
    {
// 更新当前状态
        baseVolume = Mathf.Clamp01(volume);
        targetVolume = baseVolume * GetGlobalMusicVolume();
// 更新当前状态
        fadeDuration = Mathf.Max(0f, fade);

        if (interruptionActive) CancelInterruption();

// 相同列表跨场景时保留原播放进度
        if (IsSamePlaylist(clips))
        {
// 空引用时直接退出
            if (transitionRoutine == null && source.isPlaying) source.volume = targetVolume;
            return;
        }

// 更新当前状态
        playlist = clips;
        lastIndex = -1;

// 判断当前条件
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(CrossFadeToNewPlaylist());
    }

// 定义 StopMusic 方法
    public void StopMusic(float fade = 1f)
    {
// 更新当前状态
        fadeDuration = Mathf.Max(0f, fade);
        if (interruptionActive) CancelInterruption();
        playlist = null;

// 判断当前条件
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(CrossFadeToNewPlaylist());
    }

// 播放会暂停普通 BGM 的特殊音乐
    public void PlayInterruptingBgm(object owner, AudioClip clip, float volume = 1f, float fade = 1f)
    {
// 空引用时直接退出
        if (clip == null) return;

        if (!interruptionActive)
        {
// 记录普通音乐状态供恢复
            normalWasPlayingBeforeInterruption = source.isPlaying;
            normalVolumeBeforeInterruption = targetVolume;
        }
        else
        {
            interruptionRestoring = false;
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

// 更新当前状态
        interruptionActive = true;
        interruptionOwner = owner;
        interruptionBaseVolume = Mathf.Clamp01(volume);
        interruptionTargetVolume = interruptionBaseVolume * GetGlobalMusicVolume();
        float actualFade = Mathf.Max(0f, fade);

// 判断当前条件
        if (interruptionRoutine != null) StopCoroutine(interruptionRoutine);
        interruptionRoutine = StartCoroutine(PlayInterruptionRoutine(clip, actualFade));
    }

// 停止特殊音乐并从原进度恢复普通 BGM
    public void StopInterruptingBgm(object owner, float fade = 1f)
    {
// 空引用时直接退出
        if (!interruptionActive || interruptionRestoring) return;
        if (owner != null && interruptionOwner != null && !ReferenceEquals(owner, interruptionOwner)) return;

        interruptionRestoring = true;
// 判断当前条件
        if (interruptionRoutine != null) StopCoroutine(interruptionRoutine);
        interruptionRoutine = StartCoroutine(RestorePreviousBgmRoutine(Mathf.Max(0f, fade)));
    }

// 定义 RefreshVolume 方法
    public void RefreshVolume()
    {
// 更新当前状态
        targetVolume = baseVolume * GetGlobalMusicVolume();
        interruptionTargetVolume = interruptionBaseVolume * GetGlobalMusicVolume();

// 判断当前条件
        if (transitionRoutine == null && !interruptionActive) source.volume = targetVolume;
        if (interruptionActive && !interruptionRestoring && interruptionRoutine == null)
        {
            interruptionSource.volume = interruptionTargetVolume;
        }
    }

// 定义 GetGlobalMusicVolume 方法
    private float GetGlobalMusicVolume() => SettingsManager.Instance == null ? 1f : SettingsManager.Instance.MusicVolume;

// 定义 IsSamePlaylist 方法
    private bool IsSamePlaylist(AudioClip[] clips)
    {
// 空引用时直接退出
        if (playlist == null || clips == null || playlist.Length != clips.Length)
            return playlist == null && (clips == null || clips.Length == 0);
// 循环处理当前集合
        for (int i = 0; i < clips.Length; i++) if (playlist[i] != clips[i]) return false;
        return true;
    }

// 定义 PlayInterruptionRoutine 方法
    private IEnumerator PlayInterruptionRoutine(AudioClip clip, float fade)
    {
// 保存 startVolume 数据
        float startVolume = source.volume;
        if (source.isPlaying && fade > 0f)
        {
// 淡出普通音乐但保留时间位置
            for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(startVolume, 0f, t / fade);
                yield return null;
            }
        }

// 暂停普通音乐以便返回时继续原进度
        if (source.isPlaying) source.Pause();
        source.volume = normalVolumeBeforeInterruption;

// 切换特殊音乐前淡出上一首特殊音乐
        startVolume = interruptionSource.volume;
        if (interruptionSource.isPlaying && fade > 0f)
        {
            for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
            {
                interruptionSource.volume = Mathf.Lerp(startVolume, 0f, t / fade);
                yield return null;
            }
        }

        interruptionSource.Stop();
        interruptionSource.clip = clip;
        interruptionSource.loop = true;
        interruptionSource.volume = fade > 0f ? 0f : interruptionTargetVolume;
        interruptionSource.Play();

        if (fade > 0f)
        {
// 特殊音乐淡入并循环
            for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
            {
                interruptionSource.volume = Mathf.Lerp(0f, interruptionTargetVolume, t / fade);
                yield return null;
            }
            interruptionSource.volume = interruptionTargetVolume;
        }

        interruptionRoutine = null;
    }

// 定义 RestorePreviousBgmRoutine 方法
    private IEnumerator RestorePreviousBgmRoutine(float fade)
    {
// 保存 startVolume 数据
        float startVolume = interruptionSource.volume;
        if (interruptionSource.isPlaying && fade > 0f)
        {
// 淡出特殊音乐
            for (float t = 0f; t < fade; t += Mathf.Max(Time.unscaledDeltaTime, 0.0001f))
            {
                interruptionSource.volume = Mathf.Lerp(startVolume, 0f, t / fade);
                yield return null;
            }
        }

        interruptionSource.Stop();
        interruptionSource.clip = null;

        if (normalWasPlayingBeforeInterruption && source.clip != null)
        {
// 从暂停位置继续普通音乐
            source.volume = fade > 0f ? 0f : targetVolume;
            source.UnPause();
            if (fade > 0f)
            {
                for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
                {
                    source.volume = Mathf.Lerp(0f, targetVolume, t / fade);
                    yield return null;
                }
            }
            source.volume = targetVolume;
        }
        else if (playlist != null && playlist.Length > 0)
        {
// 没有可恢复片段时重新选择普通音乐
            PlayNextRandom();
            source.volume = fade > 0f ? 0f : targetVolume;
            if (fade > 0f)
            {
                for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
                {
                    source.volume = Mathf.Lerp(0f, targetVolume, t / fade);
                    yield return null;
                }
            }
            source.volume = targetVolume;
        }
        else
        {
            source.Stop();
        }

// 清理特殊播放状态
        interruptionActive = false;
        interruptionRestoring = false;
        interruptionOwner = null;
        normalWasPlayingBeforeInterruption = false;
        interruptionRoutine = null;
    }

// 在普通播放列表接管前清理特殊状态
    private void CancelInterruption()
    {
// 判断当前条件
        if (interruptionRoutine != null) StopCoroutine(interruptionRoutine);
        interruptionSource.Stop();
        interruptionSource.clip = null;

        if (normalWasPlayingBeforeInterruption && source.clip != null)
        {
// 恢复普通播放源并保留暂停位置
            source.volume = targetVolume;
            source.UnPause();
        }

// 重置特殊播放状态
        interruptionActive = false;
        interruptionRestoring = false;
        interruptionOwner = null;
        interruptionRoutine = null;
        normalWasPlayingBeforeInterruption = false;
    }

// 定义 CrossFadeToNewPlaylist 方法
    private IEnumerator CrossFadeToNewPlaylist()
    {
// 判断当前条件
        if (source.isPlaying && fadeDuration > 0f)
        {
// 配置 startVolume 数值
            float startVolume = source.volume;
            for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
// 更新当前状态
                source.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
                yield return null;
            }
        }

// 调用 Stop
        source.Stop();
        source.volume = targetVolume;

// 判断当前条件
        if (playlist != null && playlist.Length > 0)
        {
// 执行 PlayNextRandom
            PlayNextRandom();
            if (fadeDuration > 0f)
            {
// 更新当前状态
                source.volume = 0f;
                for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
                {
// 更新当前状态
                    source.volume = Mathf.Lerp(0f, targetVolume, t / fadeDuration);
                    yield return null;
                }
// 更新当前状态
                source.volume = targetVolume;
            }
        }

// 更新当前状态
        transitionRoutine = null;
    }

// 定义 PlayNextRandom 方法
    private void PlayNextRandom()
    {
// 配置 candidateCount 数值
        int candidateCount = 0;
        for (int i = 0; i < playlist.Length; i++)
// 判断当前条件
            if (playlist[i] != null && (playlist.Length == 1 || i != lastIndex)) candidateCount++;

        if (candidateCount == 0)
        {
// 空引用时直接退出
            if (lastIndex < 0 || lastIndex >= playlist.Length || playlist[lastIndex] == null) return;
        }
        else
        {
// 定义 Range 方法
            int target = Random.Range(0, candidateCount);
            for (int i = 0; i < playlist.Length; i++)
            {
// 空引用时直接退出
                if (playlist[i] == null || (playlist.Length > 1 && i == lastIndex)) continue;
                if (target-- == 0) { lastIndex = i; break; }
            }
        }

// 更新当前状态
        source.clip = playlist[lastIndex];
        source.loop = false;
        source.Play();
    }
}