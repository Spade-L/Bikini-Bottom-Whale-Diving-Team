using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    // 单一声源负责串行播放，避免跨场景残留多个 BGM 声源
    private AudioSource source;
    private AudioClip[] playlist;
    // 用于随机选曲时避免紧接着重播同一列表项
    private int lastIndex = -1;
    private float targetVolume = 1f;
    private float fadeDuration = 1f;
    // 非空表示正在淡变，Update 不会在中间状态自动换曲
    private Coroutine transitionRoutine;

    // 首场景加载前创建，供场景组件在自身生命周期中直接调用
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
        // 重复实例立即销毁，防止两条 BGM 同时播放
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

    // 仅在应用获得焦点且过渡结束后补播，避免失焦期间产生突兀换曲
    private void Update()
    {
        // 当前曲目播完且没有在转场中 → 随机下一首
        if (transitionRoutine == null && playlist != null && playlist.Length > 0
            && !source.isPlaying && Application.isFocused)
        {
            PlayNextRandom();
        }
    }

    public void PlayPlaylist(AudioClip[] clips, float volume = 1f, float fade = 1f)
    {
        // 调用参数先归一化，再决定是否需要真正切换列表
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

        // 新列表从未选择状态开始，首曲也参与随机
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

    // 按数组长度和引用顺序比较，避免内容相同的场景切换重启 BGM
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

    // 单一协程串行完成淡出、换曲和淡入；新请求会先中止这次过渡
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

        // 无淡变时直接停止并以目标音量启动，避免除以零
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

        // 协程最后清标记，下一帧 Update 才可接管自然播完后的续播
        transitionRoutine = null;
    }

    // 在候选中随机挑选，并把本次索引留给下一轮排除
    private void PlayNextRandom()
    {
        int candidateCount = 0;
        for (int i = 0; i < playlist.Length; i++)
        {
            if (playlist[i] != null && (playlist.Length == 1 || i != lastIndex))
            {
                candidateCount++;
            }
        }

        if (candidateCount == 0)
        {
            if (lastIndex < 0 || lastIndex >= playlist.Length || playlist[lastIndex] == null)
            {
                return;
            }

            // 候选为空时保留上一首有效曲目
        }
        else
        {
            int target = Random.Range(0, candidateCount);
            for (int i = 0; i < playlist.Length; i++)
            {
                if (playlist[i] == null || (playlist.Length > 1 && i == lastIndex))
                {
                    continue;
                }

                if (target-- == 0)
                {
                    lastIndex = i;
                    break;
                }
            }
        }

        source.clip = playlist[lastIndex];
        source.Play();
    }
}
