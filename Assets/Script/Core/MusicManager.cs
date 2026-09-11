using System.Collections;
using UnityEngine;

// 管理场景音乐、专属音乐和播放进度恢复
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private AudioSource source;
// 特殊 BGM 使用独立播放源
    private AudioSource interruptionSource;

// 保存 playlist 引用
    private AudioClip[] playlist;
    private int lastIndex = -1;
// 设置 MusicManager 的配置数值
    private float targetVolume = 1f;
    private float baseVolume = 1f;
// 设置 MusicManager 的配置数值（MusicManager 后续步骤）
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

// 在场景加载前创建常驻管理器
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
// AutoCreate 缺少引用时提前结束
        if (Instance == null) new GameObject("MusicManager").AddComponent<MusicManager>();
    }

// 初始化组件引用和运行状态
    private void Awake()
    {
// 检查 Awake 的前置条件
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
// 推进 Awake 中的必要步骤
        DontDestroyOnLoad(gameObject);

        source = gameObject.AddComponent<AudioSource>();
// 普通列表播放结束后由 Update 继续轮播
        source.loop = false;
        source.playOnAwake = false;
// 同步 Awake 的状态
        source.spatialBlend = 0f;

        interruptionSource = gameObject.AddComponent<AudioSource>();
        interruptionSource.loop = true;
        interruptionSource.playOnAwake = false;
        interruptionSource.spatialBlend = 0f;
    }

// 每帧检查输入与状态变化
    private void Update()
    {
// 特殊音乐期间不启动普通播放列表
        if (!interruptionActive && transitionRoutine == null && playlist != null && playlist.Length > 0
            && !source.isPlaying && Application.isFocused)
        {
            PlayNextRandom();
        }
    }

// 播放 PlayPlaylist 对应演出
    public void PlayPlaylist(AudioClip[] clips, float volume = 1f, float fade = 1f)
    {
// 同步 PlayPlaylist 的内部状态
        baseVolume = Mathf.Clamp01(volume);
        targetVolume = baseVolume * GetGlobalMusicVolume();
// 同步 PlayPlaylist 的内部状态（PlayPlaylist）
        fadeDuration = Mathf.Max(0f, fade);

        if (interruptionActive) CancelInterruption();

// 相同列表跨场景时保留原播放进度
        if (IsSamePlaylist(clips))
        {
// 缺少必要引用时退出 PlayPlaylist
            if (transitionRoutine == null && source.isPlaying) source.volume = targetVolume;
            return;
        }

// 同步 PlayPlaylist 的内部状态（PlayPlaylist）（playlist）
        playlist = clips;
        lastIndex = -1;

// 检查 PlayPlaylist 的前置条件
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(CrossFadeToNewPlaylist());
    }

// 停止 StopMusic 对应流程
    public void StopMusic(float fade = 1f)
    {
// 同步 StopMusic 的内部状态
        fadeDuration = Mathf.Max(0f, fade);
        if (interruptionActive) CancelInterruption();
        playlist = null;

// 检查 StopMusic 的前置条件
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(CrossFadeToNewPlaylist());
    }

// 暂停普通音乐并播放专属循环音乐
    public void PlayInterruptingBgm(object owner, AudioClip clip, float volume = 1f, float fade = 1f)
    {
// 缺少必要引用时退出 PlayInterruptingBgm
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

// 同步 PlayInterruptingBgm 的内部状态
        interruptionActive = true;
        interruptionOwner = owner;
        interruptionBaseVolume = Mathf.Clamp01(volume);
        interruptionTargetVolume = interruptionBaseVolume * GetGlobalMusicVolume();
        float actualFade = Mathf.Max(0f, fade);

// 检查 PlayInterruptingBgm 的前置条件
        if (interruptionRoutine != null) StopCoroutine(interruptionRoutine);
        interruptionRoutine = StartCoroutine(PlayInterruptionRoutine(clip, actualFade));
    }

// 淡出专属音乐并恢复普通音乐进度
    public void StopInterruptingBgm(object owner, float fade = 1f)
    {
// 缺少必要引用时退出 StopInterruptingBgm
        if (!interruptionActive || interruptionRestoring) return;
        if (owner != null && interruptionOwner != null && !ReferenceEquals(owner, interruptionOwner)) return;

        interruptionRestoring = true;
// 检查 StopInterruptingBgm 的前置条件
        if (interruptionRoutine != null) StopCoroutine(interruptionRoutine);
        interruptionRoutine = StartCoroutine(RestorePreviousBgmRoutine(Mathf.Max(0f, fade)));
    }

// 刷新 RefreshVolume 对应状态
    public void RefreshVolume()
    {
// 同步 RefreshVolume 的内部状态
        targetVolume = baseVolume * GetGlobalMusicVolume();
        interruptionTargetVolume = interruptionBaseVolume * GetGlobalMusicVolume();

// 检查 RefreshVolume 的前置条件
        if (transitionRoutine == null && !interruptionActive) source.volume = targetVolume;
        if (interruptionActive && !interruptionRestoring && interruptionRoutine == null)
        {
            interruptionSource.volume = interruptionTargetVolume;
        }
    }

// 获取 GetGlobalMusicVolume 所需引用
    private float GetGlobalMusicVolume() => SettingsManager.Instance == null ? 1f : SettingsManager.Instance.MusicVolume;

// 判断 IsSamePlaylist 对应条件
    private bool IsSamePlaylist(AudioClip[] clips)
    {
// 缺少必要引用时退出 IsSamePlaylist
        if (playlist == null || clips == null || playlist.Length != clips.Length)
            return playlist == null && (clips == null || clips.Length == 0);
// 循环处理当前集合
        for (int i = 0; i < clips.Length; i++) if (playlist[i] != clips[i]) return false;
        return true;
    }

// 执行专属音乐淡入与循环切换
    private IEnumerator PlayInterruptionRoutine(AudioClip clip, float fade)
    {
// 同步 PlayInterruptionRoutine 的相关数据
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

// 从暂停位置恢复普通音乐并淡入
    private IEnumerator RestorePreviousBgmRoutine(float fade)
    {
// 同步 RestorePreviousBgmRoutine 的相关数据
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

// 判断 CancelInterruption 对应条件
    private void CancelInterruption()
    {
// 检查 CancelInterruption 的前置条件
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

// 淡出旧曲并启动新的场景播放列表
    private IEnumerator CrossFadeToNewPlaylist()
    {
// 检查 CrossFadeToNewPlaylist 的前置条件
        if (source.isPlaying && fadeDuration > 0f)
        {
// 设置 CrossFadeToNewPlaylist 的配置数值
            float startVolume = source.volume;
            for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
// 同步 CrossFadeToNewPlaylist 的内部状态
                source.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
                yield return null;
            }
        }

// 使用 CrossFadeToNewPlaylist 所需功能
        source.Stop();
        source.volume = targetVolume;

// 检查 CrossFadeToNewPlaylist 的前置条件（CrossFadeToNewPlaylist）
        if (playlist != null && playlist.Length > 0)
        {
// 推进 CrossFadeToNewPlaylist 中的必要步骤
            PlayNextRandom();
            if (fadeDuration > 0f)
            {
// 同步 CrossFadeToNewPlaylist 的内部状态（CrossFadeToNewPlaylist）
                source.volume = 0f;
                for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
                {
// 同步 CrossFadeToNewPlaylist 的内部状态（CrossFadeToNewPlaylist）（source）
                    source.volume = Mathf.Lerp(0f, targetVolume, t / fadeDuration);
                    yield return null;
                }
// 在 CrossFadeToNewPlaylist 中继续当前处理
                source.volume = targetVolume;
            }
        }

// 同步 CrossFadeToNewPlaylist 的内部状态（CrossFadeToNewPlaylist）（transitionRoutine）
        transitionRoutine = null;
    }

// 从播放列表随机选择下一首并避免重复
    private void PlayNextRandom()
    {
// 设置 PlayNextRandom 的配置数值
        int candidateCount = 0;
        for (int i = 0; i < playlist.Length; i++)
// 检查 PlayNextRandom 的前置条件
            if (playlist[i] != null && (playlist.Length == 1 || i != lastIndex)) candidateCount++;

        if (candidateCount == 0)
        {
// 缺少必要引用时退出 PlayNextRandom
            if (lastIndex < 0 || lastIndex >= playlist.Length || playlist[lastIndex] == null) return;
        }
        else
        {
// 完成 PlayNextRandom 的主要职责
            int target = Random.Range(0, candidateCount);
            for (int i = 0; i < playlist.Length; i++)
            {
// 缺少必要引用时退出 PlayNextRandom（PlayNextRandom）
                if (playlist[i] == null || (playlist.Length > 1 && i == lastIndex)) continue;
                if (target-- == 0) { lastIndex = i; break; }
            }
        }

// 同步 PlayNextRandom 的内部状态
        source.clip = playlist[lastIndex];
        source.loop = false;
        source.Play();
    }
}
