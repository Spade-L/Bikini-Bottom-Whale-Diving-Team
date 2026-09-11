using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

// 纯数据载体，字段名同时是 JsonUtility 写入的 JSON 键名
[Serializable]
public class SaveData
{
// 同步 SaveData 的相关数据
    public List<string> flags;
    public List<string> collectedClueIds;
// 设置 SaveData 的配置数值
    public int timePeriod;
    public int investigationCount;

// 同步 SaveData 的相关数据（SaveData 后续步骤）
    public string sceneName;
    public float playerX;
// 设置 SaveData 的配置数值（SaveData 后续步骤）
    public float playerY;
    public string savedAtUtc;
}

// 负责存档文件读写、校验与槽位管理
public static class SaveSystem
{
// 推进 SaveSystem 的当前步骤
    public const int MinSlot = 1;
    public const int MaxSlot = 4;

// 获取 GetPath 所需引用
    private static string GetPath(int slot)
    {
// 返回 GetPath 的处理结果
        return Path.Combine(Application.persistentDataPath, $"save_{slot}.json");
    }

// 判断 IsValidSlot 对应条件
    public static bool IsValidSlot(int slot)
    {
// 返回 IsValidSlot 的处理结果
        return slot >= MinSlot && slot <= MaxSlot;
    }

// 判断 HasSave 对应条件
    public static bool HasSave(int slot)
    {
// 返回 HasSave 的处理结果
        return TryLoad(slot, out _);
    }

// 保存 Save 对应数据
    public static bool Save(SaveData data, int slot)
    {
// Save 缺少引用时提前结束
        if (!IsValidSlot(slot) || data == null || string.IsNullOrWhiteSpace(data.sceneName))
        {
// 输出调试信息
            Debug.LogWarning($"[SaveSystem] 无法保存：槽位或存档数据无效 ({slot})。");
            return false;
        }

// 同步 Save 的状态
        data.savedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        string path = GetPath(slot);
// 同步 Save 的相关数据
        string temporaryPath = path + ".tmp";

// 保护可能失败的流程
        try
        {
// 使用 Save 所需功能
            Directory.CreateDirectory(Application.persistentDataPath);
            string json = JsonUtility.ToJson(data, true);
// 使用 Save 所需功能（Save）
            File.WriteAllText(temporaryPath, json);

// 检查 Save 的前置条件
            if (File.Exists(path))
            {
// 在 Save 中处理 Replace
                File.Replace(temporaryPath, path, null);
            }
// 处理 Save 的备用分支
            else
            {
// 在 Save 中处理 Move
                File.Move(temporaryPath, path);
            }

// 在 Save 中继续当前处理
            Debug.Log($"[SaveSystem] 已存档: {path}");
            return true;
        }
// 处理异常情况
        catch (Exception exception)
        {
// 在 Save 中继续当前处理（Save 后续步骤）
            Debug.LogError($"[SaveSystem] 保存失败 ({slot}): {exception.Message}");
            TryDelete(temporaryPath);
// 返回 Save 的处理结果
            return false;
        }
    }

// 加载 Load 对应数据
    public static SaveData Load(int slot)
    {
// 返回 Load 的处理结果
        return TryLoad(slot, out SaveData data) ? data : null;
    }

// 处理 TryLoad 对应逻辑
    public static bool TryLoad(int slot, out SaveData data)
    {
// 同步 TryLoad 的内部状态
        data = null;
        if (!IsValidSlot(slot))
        {
// 返回 TryLoad 的处理结果
            return false;
        }

// 获取 GetPath 所需引用（TryLoad）
        string path = GetPath(slot);
        if (!File.Exists(path))
        {
// 返回 TryLoad 的处理结果（TryLoad）
            return false;
        }

// 在 TryLoad 中继续当前处理
        try
        {
// 同步 TryLoad 的内部状态（TryLoad）
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            if (!IsValidData(data))
            {
// 同步 TryLoad 的内部状态（TryLoad）（data）
                data = null;
                return false;
            }

// 返回 TryLoad 的处理结果（TryLoad）（return）
            return true;
        }
// 在 TryLoad 中继续当前处理（TryLoad 后续步骤）
        catch (Exception exception)
        {
// 在 TryLoad 中继续当前处理（TryLoad 后续步骤）（151）
            Debug.LogWarning($"[SaveSystem] 存档无法读取，将按空槽位处理 ({slot}): {exception.Message}");
            return false;
        }
    }

// 处理 TryGetSavedTime 对应逻辑
    public static bool TryGetSavedTime(int slot, out DateTime savedAtLocal)
    {
// 同步 TryGetSavedTime 的内部状态
        savedAtLocal = default(DateTime);
        if (!TryLoad(slot, out SaveData data))
        {
// 返回 TryGetSavedTime 的处理结果
            return false;
        }

// 检查 TryGetSavedTime 的前置条件
        if (!DateTime.TryParse(
                data.savedAtUtc,
// 推进 TryGetSavedTime 的当前步骤
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
// 推进 TryGetSavedTime 的当前步骤（TryGetSavedTime）
                out DateTime savedAtUtc))
        {
// 返回 TryGetSavedTime 的处理结果（TryGetSavedTime）
            return false;
        }

// 同步 TryGetSavedTime 的内部状态（TryGetSavedTime）
        savedAtLocal = savedAtUtc.ToLocalTime();
        return true;
    }

// 判断 IsValidData 对应条件
    public static bool IsValidData(SaveData data)
    {
// 缺少必要引用时退出 IsValidData
        if (data == null || string.IsNullOrWhiteSpace(data.sceneName) || string.IsNullOrWhiteSpace(data.savedAtUtc))
        {
// 返回 IsValidData 的处理结果
            return false;
        }

// 返回 IsValidData 的处理结果（IsValidData）
        return DateTime.TryParse(
            data.savedAtUtc,
// 推进 IsValidData 的当前步骤
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
// 推进 IsValidData 的当前步骤（IsValidData）
            out _);
    }

// 处理 Delete 对应逻辑
    public static void Delete(int slot)
    {
// 检查 Delete 的前置条件
        if (!IsValidSlot(slot))
        {
// 返回 Delete 的处理结果
            return;
        }

// 推进 Delete 中的必要步骤
        TryDelete(GetPath(slot));
    }

// 处理 TryDelete 对应逻辑
    private static void TryDelete(string path)
    {
// 在 TryDelete 中继续当前处理
        try
        {
// 检查 TryDelete 的前置条件
            if (File.Exists(path))
            {
// 使用 TryDelete 所需功能
                File.Delete(path);
            }
        }
// 在 TryDelete 中继续当前处理（TryDelete 后续步骤）
        catch (Exception exception)
        {
// 在 TryDelete 中继续当前处理（TryDelete 后续步骤）（236）
            Debug.LogWarning($"[SaveSystem] 文件清理失败: {exception.Message}");
        }
    }
}
