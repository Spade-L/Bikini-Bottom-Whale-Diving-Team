using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 播放场景剧情演出并恢复临时对象状态
public class SceneStoryPresentation : MonoBehaviour
{
// 配置 一次性播放 分组
    [Header("一次性播放")]
    [SerializeField] private bool playOnce = true;
// 同步 SceneStoryPresentation 的相关数据
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
// 使用 SceneStoryPresentation 所需功能
    [TextArea(2, 4)]
    [SerializeField] private string[] lines;
// 设置 SceneStoryPresentation 的配置数值
    [SerializeField] private float lineDuration = 1f;

// 记录 SceneStoryPresentation 的当前状态
    private bool playing;
    private readonly Dictionary<GameObject, bool> originalStates = new Dictionary<GameObject, bool>();
// 推进 SceneStoryPresentation 的当前步骤
    private readonly Dictionary<TMP_Text, string> originalTexts = new Dictionary<TMP_Text, string>();

// 记录 SceneStoryPresentation 的当前状态（SceneStoryPresentation 后续步骤）
    public bool IsPlaying => playing;

// 播放 Play 对应演出
    public void Play(Action onComplete)
    {
// 检查 Play 的前置条件
        if (playing)
        {
// 返回 Play 的处理结果
            return;
        }

// 在 Play 中处理 检查 Play 的前置条件
        if (playOnce && !string.IsNullOrEmpty(playedFlag)
            && GameManager.Instance != null
// 使用 Play 所需功能
            && GameManager.Instance.HasFlag(playedFlag))
        {
// 使用 Play 所需功能（Play）
            onComplete?.Invoke();
            return;
        }

// 启动当前协程
        StartCoroutine(PlayRoutine(onComplete));
    }

// 播放 PlayRoutine 对应演出
    private IEnumerator PlayRoutine(Action onComplete)
    {
// 同步 PlayRoutine 的状态
        playing = true;
        SetObjects(objectsToHide, false);
// 推进 PlayRoutine 中的必要步骤
        SetObjects(objectsToShow, true);
        SetText(string.Empty);

// 检查 PlayRoutine 的前置条件
        if (lines != null && lines.Length > 0 && textObjects != null && textObjects.Length > 0)
        {
// 遍历全部元素
            foreach (string line in lines)
            {
// 推进 PlayRoutine 中的必要步骤（PlayRoutine）
                SetText(line);
                yield return new WaitForSeconds(Mathf.Max(0f, lineDuration));
            }
        }
// 处理 PlayRoutine 的备用分支
        else
        {
// 等待下一步
            yield return new WaitForSeconds(Mathf.Max(0f, duration));
        }

// 在 PlayRoutine 中处理 RestoreVisuals
        RestoreVisuals();

// 检查 PlayRoutine 的前置条件（PlayRoutine）
        if (!string.IsNullOrEmpty(playedFlag) && GameManager.Instance != null)
        {
// 更新剧情标记
            GameManager.Instance.SetFlag(playedFlag);
        }

// 同步 PlayRoutine 的内部状态
        playing = false;
        onComplete?.Invoke();
    }

// 禁用时取消订阅并清理临时状态
    private void OnDisable()
    {
// 推进 OnDisable 中的必要步骤
        StopAllCoroutines();
        RestoreVisuals();
// 同步 OnDisable 的内部状态
        playing = false;
    }

// 恢复 RestoreVisuals 对应状态
    private void RestoreVisuals()
    {
// 在 RestoreVisuals 中继续当前处理
        foreach (var pair in originalStates)
        {
// 检查 RestoreVisuals 的前置条件
            if (pair.Key != null) pair.Key.SetActive(pair.Value);
        }
// 使用 RestoreVisuals 所需功能
        originalStates.Clear();
        foreach (var pair in originalTexts)
        {
// 检查 RestoreVisuals 的前置条件（RestoreVisuals）
            if (pair.Key != null) pair.Key.text = pair.Value;
        }
// 使用 RestoreVisuals 所需功能（RestoreVisuals）
        originalTexts.Clear();
    }

// 设置 SetObjects 的目标状态
    private void SetObjects(GameObject[] objects, bool active)
    {
// SetObjects 缺少引用时提前结束
        if (objects == null) return;
        foreach (GameObject target in objects)
        {
            // 不允许演出关闭自身或自己的父节点
            if (target == null || transform.IsChildOf(target.transform)) continue;
            if (!originalStates.ContainsKey(target)) originalStates.Add(target, target.activeSelf);
// 切换 SetObjects 的显示状态
            target.SetActive(active);
        }
    }

// 设置 SetText 的目标状态
    private void SetText(string value)
    {
// 缺少必要引用时退出 SetText
        if (textObjects == null) return;
        foreach (TMP_Text text in textObjects)
        {
// 缺少必要引用时退出 SetText（SetText）
            if (text == null) continue;
            if (!originalTexts.ContainsKey(text)) originalTexts.Add(text, text.text);
// 更新 SetText 的界面文本
            text.text = value;
        }
    }
}
