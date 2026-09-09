using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }
    private AudioSource source;
    private AudioClip[] playlist;
    private int lastIndex = -1;
    private float targetVolume = 1f;
    private float baseVolume = 1f;
    private float fadeDuration = 1f;
    private Coroutine transitionRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null) new GameObject("MusicManager").AddComponent<MusicManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        source = gameObject.AddComponent<AudioSource>();
        source.loop = false;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    private void Update()
    {
        if (transitionRoutine == null && playlist != null && playlist.Length > 0 && !source.isPlaying && Application.isFocused)
            PlayNextRandom();
    }

    public void PlayPlaylist(AudioClip[] clips, float volume = 1f, float fade = 1f)
    {
        baseVolume = Mathf.Clamp01(volume);
        targetVolume = baseVolume * GetGlobalMusicVolume();
        fadeDuration = Mathf.Max(0f, fade);
        if (IsSamePlaylist(clips))
        {
            if (transitionRoutine == null && source.isPlaying) source.volume = targetVolume;
            return;
        }
        playlist = clips;
        lastIndex = -1;
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(CrossFadeToNewPlaylist());
    }

    public void StopMusic(float fade = 1f)
    {
        fadeDuration = Mathf.Max(0f, fade);
        playlist = null;
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(CrossFadeToNewPlaylist());
    }

    public void RefreshVolume()
    {
        targetVolume = baseVolume * GetGlobalMusicVolume();
        if (transitionRoutine == null) source.volume = targetVolume;
    }

    private float GetGlobalMusicVolume() => SettingsManager.Instance == null ? 1f : SettingsManager.Instance.MusicVolume;

    private bool IsSamePlaylist(AudioClip[] clips)
    {
        if (playlist == null || clips == null || playlist.Length != clips.Length)
            return playlist == null && (clips == null || clips.Length == 0);
        for (int i = 0; i < clips.Length; i++) if (playlist[i] != clips[i]) return false;
        return true;
    }

    private IEnumerator CrossFadeToNewPlaylist()
    {
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
        int candidateCount = 0;
        for (int i = 0; i < playlist.Length; i++)
            if (playlist[i] != null && (playlist.Length == 1 || i != lastIndex)) candidateCount++;
        if (candidateCount == 0)
        {
            if (lastIndex < 0 || lastIndex >= playlist.Length || playlist[lastIndex] == null) return;
        }
        else
        {
            int target = Random.Range(0, candidateCount);
            for (int i = 0; i < playlist.Length; i++)
            {
                if (playlist[i] == null || (playlist.Length > 1 && i == lastIndex)) continue;
                if (target-- == 0) { lastIndex = i; break; }
            }
        }
        source.clip = playlist[lastIndex];
        source.Play();
    }
}
