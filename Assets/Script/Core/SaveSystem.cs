using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

// 纯数据载体，字段名同时是 JsonUtility 写入的 JSON 键名
[Serializable]
public class SaveData
{
// 保存 flags 数据
    public List<string> flags;
    public List<string> collectedClueIds;
// 配置 timePeriod 数值
    public int timePeriod;
    public int investigationCount;

// 保存 sceneName 数据
    public string sceneName;
    public float playerX;
// 配置 playerY 数值
    public float playerY;
    public string savedAtUtc;
}

// 定义 SaveSystem 类型
public static class SaveSystem
{
// 更新当前逻辑
    public const int MinSlot = 1;
    public const int MaxSlot = 4;

// 定义 GetPath 方法
    private static string GetPath(int slot)
    {
// 返回当前结果
        return Path.Combine(Application.persistentDataPath, $"save_{slot}.json");
    }

// 定义 IsValidSlot 方法
    public static bool IsValidSlot(int slot)
    {
// 返回当前结果
        return slot >= MinSlot && slot <= MaxSlot;
    }

// 定义 HasSave 方法
    public static bool HasSave(int slot)
    {
// 返回当前结果
        return TryLoad(slot, out _);
    }

// 定义 Save 方法
    public static bool Save(SaveData data, int slot)
    {
// 空引用时直接退出
        if (!IsValidSlot(slot) || data == null || string.IsNullOrWhiteSpace(data.sceneName))
        {
// 输出调试信息
            Debug.LogWarning($"[SaveSystem] 无法保存：槽位或存档数据无效 ({slot})。");
            return false;
        }

// 更新当前状态
        data.savedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        string path = GetPath(slot);
// 保存 temporaryPath 数据
        string temporaryPath = path + ".tmp";

// 保护可能失败的流程
        try
        {
// 调用 CreateDirectory
            Directory.CreateDirectory(Application.persistentDataPath);
            string json = JsonUtility.ToJson(data, true);
// 调用 WriteAllText
            File.WriteAllText(temporaryPath, json);

// 判断当前条件
            if (File.Exists(path))
            {
// 调用 Replace
                File.Replace(temporaryPath, path, null);
            }
// 处理其他分支
            else
            {
// 调用 Move
                File.Move(temporaryPath, path);
            }

// 输出调试信息
            Debug.Log($"[SaveSystem] 已存档: {path}");
            return true;
        }
// 处理异常情况
        catch (Exception exception)
        {
// 输出调试信息
            Debug.LogError($"[SaveSystem] 保存失败 ({slot}): {exception.Message}");
            TryDelete(temporaryPath);
// 返回当前结果
            return false;
        }
    }

// 定义 Load 方法
    public static SaveData Load(int slot)
    {
// 返回当前结果
        return TryLoad(slot, out SaveData data) ? data : null;
    }

// 定义 TryLoad 方法
    public static bool TryLoad(int slot, out SaveData data)
    {
// 更新当前状态
        data = null;
        if (!IsValidSlot(slot))
        {
// 返回当前结果
            return false;
        }

// 定义 GetPath 方法
        string path = GetPath(slot);
        if (!File.Exists(path))
        {
// 返回当前结果
            return false;
        }

// 保护可能失败的流程
        try
        {
// 更新当前状态
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            if (!IsValidData(data))
            {
// 更新当前状态
                data = null;
                return false;
            }

// 返回当前结果
            return true;
        }
// 处理异常情况
        catch (Exception exception)
        {
// 输出调试信息
            Debug.LogWarning($"[SaveSystem] 存档无法读取，将按空槽位处理 ({slot}): {exception.Message}");
            return false;
        }
    }

// 定义 TryGetSavedTime 方法
    public static bool TryGetSavedTime(int slot, out DateTime savedAtLocal)
    {
// 更新当前状态
        savedAtLocal = default(DateTime);
        if (!TryLoad(slot, out SaveData data))
        {
// 返回当前结果
            return false;
        }

// 判断当前条件
        if (!DateTime.TryParse(
                data.savedAtUtc,
// 更新当前逻辑
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
// 更新当前逻辑
                out DateTime savedAtUtc))
        {
// 返回当前结果
            return false;
        }

// 更新当前状态
        savedAtLocal = savedAtUtc.ToLocalTime();
        return true;
    }

// 定义 IsValidData 方法
    public static bool IsValidData(SaveData data)
    {
// 空引用时直接退出
        if (data == null || string.IsNullOrWhiteSpace(data.sceneName) || string.IsNullOrWhiteSpace(data.savedAtUtc))
        {
// 返回当前结果
            return false;
        }

// 返回当前结果
        return DateTime.TryParse(
            data.savedAtUtc,
// 更新当前逻辑
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
// 更新当前逻辑
            out _);
    }

// 定义 Delete 方法
    public static void Delete(int slot)
    {
// 判断当前条件
        if (!IsValidSlot(slot))
        {
// 返回当前结果
            return;
        }

// 执行 TryDelete
        TryDelete(GetPath(slot));
    }

// 定义 TryDelete 方法
    private static void TryDelete(string path)
    {
// 保护可能失败的流程
        try
        {
// 判断当前条件
            if (File.Exists(path))
            {
// 调用 Delete
                File.Delete(path);
            }
        }
// 处理异常情况
        catch (Exception exception)
        {
// 输出调试信息
            Debug.LogWarning($"[SaveSystem] 文件清理失败: {exception.Message}");
        }
    }
}
