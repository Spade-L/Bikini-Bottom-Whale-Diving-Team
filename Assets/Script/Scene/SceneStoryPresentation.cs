using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 可选的场景出口演出。挂在门或出口对象上，由 SceneDoor 在切场景前调用。
/// 演出只负责临时视觉，不写入存档；完成后通过回调继续转场。
/// </summary>
public class SceneStoryPresentation : MonoBehaviour
{
    [Header("一次性播放")]
    [SerializeField] private bool playOnce = true;
    [SerializeField] private string playedFlag;

    [Header("临时显示对象")]
    [SerializeField] private GameObject[] objectsToShow;
    [SerializeField] private GameObject[] objectsToHide;
    [SerializeField] private float duration = 1f;

    [Header("可选文字")]
    [SerializeField] private TMP_Text[] textObjects;
    [TextArea(2, 4)]
    [SerializeField] private string[] lines;
    [SerializeField] private float lineDuration = 1f;

    private bool playing;
    private readonly Dictionary<GameObject, bool> originalStates = new Dictionary<GameObject, bool>();
    private readonly Dictionary<TMP_Text, string> originalTexts = new Dictionary<TMP_Text, string>();

    public bool IsPlaying => playing;

    public void Play(Action onComplete)
    {
        if (playing)
        {
            return;
        }

        if (playOnce && !string.IsNullOrEmpty(playedFlag)
            && GameManager.Instance != null
            && GameManager.Instance.HasFlag(playedFlag))
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(PlayRoutine(onComplete));
    }

    private IEnumerator PlayRoutine(Action onComplete)
    {
        playing = true;
        SetObjects(objectsToHide, false);
        SetObjects(objectsToShow, true);
        SetText(string.Empty);

        if (lines != null && lines.Length > 0 && textObjects != null && textObjects.Length > 0)
        {
            foreach (string line in lines)
            {
                SetText(line);
                yield return new WaitForSeconds(Mathf.Max(0f, lineDuration));
            }
        }
        else
        {
            yield return new WaitForSeconds(Mathf.Max(0f, duration));
        }

        RestoreVisuals();

        if (!string.IsNullOrEmpty(playedFlag) && GameManager.Instance != null)
        {
            GameManager.Instance.SetFlag(playedFlag);
        }

        playing = false;
        onComplete?.Invoke();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        RestoreVisuals();
        playing = false;
    }

    private void RestoreVisuals()
    {
        foreach (var pair in originalStates)
        {
            if (pair.Key != null) pair.Key.SetActive(pair.Value);
        }
        originalStates.Clear();
        foreach (var pair in originalTexts)
        {
            if (pair.Key != null) pair.Key.text = pair.Value;
        }
        originalTexts.Clear();
    }

    private void SetObjects(GameObject[] objects, bool active)
    {
        if (objects == null) return;
        foreach (GameObject target in objects)
        {
            // 不允许演出关闭自身或自己的父节点。
            if (target == null || transform.IsChildOf(target.transform)) continue;
            if (!originalStates.ContainsKey(target)) originalStates.Add(target, target.activeSelf);
            target.SetActive(active);
        }
    }

    private void SetText(string value)
    {
        if (textObjects == null) return;
        foreach (TMP_Text text in textObjects)
        {
            if (text == null) continue;
            if (!originalTexts.ContainsKey(text)) originalTexts.Add(text, text.text);
            text.text = value;
        }
    }
}
