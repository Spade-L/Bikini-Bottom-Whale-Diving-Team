using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

// 纯数据载体，字段名同时是 JsonUtility 写入的 JSON 键名
[Serializable]
public class SaveData
{
    public List<string> flags;
    public List<string> collectedClueIds;
    public int timePeriod;
    public int investigationCount;

    public string sceneName;
    public float playerX;
    public float playerY;
    public string savedAtUtc;
}

public static class SaveSystem
{
    public const int MinSlot = 1;
    public const int MaxSlot = 4;

    private static string GetPath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"save_{slot}.json");
    }

    public static bool IsValidSlot(int slot)
    {
        return slot >= MinSlot && slot <= MaxSlot;
    }

    public static bool HasSave(int slot)
    {
        return TryLoad(slot, out _);
    }

    public static bool Save(SaveData data, int slot)
    {
        if (!IsValidSlot(slot) || data == null || string.IsNullOrWhiteSpace(data.sceneName))
        {
            Debug.LogWarning($"[SaveSystem] 无法保存：槽位或存档数据无效 ({slot})。");
            return false;
        }

        data.savedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        string path = GetPath(slot);
        string temporaryPath = path + ".tmp";

        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(temporaryPath, json);

            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, null);
            }
            else
            {
                File.Move(temporaryPath, path);
            }

            Debug.Log($"[SaveSystem] 已存档: {path}");
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SaveSystem] 保存失败 ({slot}): {exception.Message}");
            TryDelete(temporaryPath);
            return false;
        }
    }

    public static SaveData Load(int slot)
    {
        return TryLoad(slot, out SaveData data) ? data : null;
    }

    public static bool TryLoad(int slot, out SaveData data)
    {
        data = null;
        if (!IsValidSlot(slot))
        {
            return false;
        }

        string path = GetPath(slot);
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            if (!IsValidData(data))
            {
                data = null;
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[SaveSystem] 存档无法读取，将按空槽位处理 ({slot}): {exception.Message}");
            return false;
        }
    }

    public static bool TryGetSavedTime(int slot, out DateTime savedAtLocal)
    {
        savedAtLocal = default(DateTime);
        if (!TryLoad(slot, out SaveData data))
        {
            return false;
        }

        if (!DateTime.TryParse(
                data.savedAtUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTime savedAtUtc))
        {
            return false;
        }

        savedAtLocal = savedAtUtc.ToLocalTime();
        return true;
    }

    public static bool IsValidData(SaveData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.sceneName) || string.IsNullOrWhiteSpace(data.savedAtUtc))
        {
            return false;
        }

        return DateTime.TryParse(
            data.savedAtUtc,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out _);
    }

    public static void Delete(int slot)
    {
        if (!IsValidSlot(slot))
        {
            return;
        }

        TryDelete(GetPath(slot));
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[SaveSystem] 文件清理失败: {exception.Message}");
        }
    }
}
