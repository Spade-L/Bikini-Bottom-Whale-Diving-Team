using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 定义 SceneStoryPresentation 类型
public class SceneStoryPresentation : MonoBehaviour
{
// 配置 一次性播放 分组
    [Header("一次性播放")]
    [SerializeField] private bool playOnce = true;
// 保存 playedFlag 数据
    [SerializeField] private string playedFlag;

// 配置 临时显示对象 分组
    [Header("临时显示对象")]
    [SerializeField] private GameObject[] objectsToShow;
// 保存 objectsToHide 引用
    [SerializeField] private GameObject[] objectsToHide;
    [SerializeField] private float duration = 1f;

// 配置 可选文字 分组
    [Header("可选文字")]
    [SerializeField] private TMP_Text[] textObjects;
// 调用 TextArea
    [TextArea(2, 4)]
    [SerializeField] private string[] lines;
// 配置 lineDuration 数值
    [SerializeField] private float lineDuration = 1f;

// 记录 playing 状态
    private bool playing;
    private readonly Dictionary<GameObject, bool> originalStates = new Dictionary<GameObject, bool>();
// 更新当前逻辑
    private readonly Dictionary<TMP_Text, string> originalTexts = new Dictionary<TMP_Text, string>();

// 记录 IsPlaying 状态
    public bool IsPlaying => playing;

// 定义 Play 方法
    public void Play(Action onComplete)
    {
// 判断当前条件
        if (playing)
        {
// 返回当前结果
            return;
        }

// 判断当前条件
        if (playOnce && !string.IsNullOrEmpty(playedFlag)
            && GameManager.Instance != null
// 调用 HasFlag
            && GameManager.Instance.HasFlag(playedFlag))
        {
// 调用 Invoke
            onComplete?.Invoke();
            return;
        }

// 启动当前协程
        StartCoroutine(PlayRoutine(onComplete));
    }

// 定义 PlayRoutine 方法
    private IEnumerator PlayRoutine(Action onComplete)
    {
// 更新当前状态
        playing = true;
        SetObjects(objectsToHide, false);
// 执行 SetObjects
        SetObjects(objectsToShow, true);
        SetText(string.Empty);

// 判断当前条件
        if (lines != null && lines.Length > 0 && textObjects != null && textObjects.Length > 0)
        {
// 遍历全部元素
            foreach (string line in lines)
            {
// 执行 SetText
                SetText(line);
                yield return new WaitForSeconds(Mathf.Max(0f, lineDuration));
            }
        }
// 处理其他分支
        else
        {
// 等待下一步
            yield return new WaitForSeconds(Mathf.Max(0f, duration));
        }

// 执行 RestoreVisuals
        RestoreVisuals();

// 判断当前条件
        if (!string.IsNullOrEmpty(playedFlag) && GameManager.Instance != null)
        {
// 更新剧情标记
            GameManager.Instance.SetFlag(playedFlag);
        }

// 更新当前状态
        playing = false;
        onComplete?.Invoke();
    }

// 定义 OnDisable 方法
    private void OnDisable()
    {
// 执行 StopAllCoroutines
        StopAllCoroutines();
        RestoreVisuals();
// 更新当前状态
        playing = false;
    }

// 定义 RestoreVisuals 方法
    private void RestoreVisuals()
    {
// 遍历全部元素
        foreach (var pair in originalStates)
        {
// 判断当前条件
            if (pair.Key != null) pair.Key.SetActive(pair.Value);
        }
// 调用 Clear
        originalStates.Clear();
        foreach (var pair in originalTexts)
        {
// 判断当前条件
            if (pair.Key != null) pair.Key.text = pair.Value;
        }
// 调用 Clear
        originalTexts.Clear();
    }

// 定义 SetObjects 方法
    private void SetObjects(GameObject[] objects, bool active)
    {
// 空引用时直接退出
        if (objects == null) return;
        foreach (GameObject target in objects)
        {
            // 不允许演出关闭自身或自己的父节点
            if (target == null || transform.IsChildOf(target.transform)) continue;
            if (!originalStates.ContainsKey(target)) originalStates.Add(target, target.activeSelf);
// 切换显示状态
            target.SetActive(active);
        }
    }

// 定义 SetText 方法
    private void SetText(string value)
    {
// 空引用时直接退出
        if (textObjects == null) return;
        foreach (TMP_Text text in textObjects)
        {
// 空引用时直接退出
            if (text == null) continue;
            if (!originalTexts.ContainsKey(text)) originalTexts.Add(text, text.text);
// 更新界面文本
            text.text = value;
        }
    }
}
