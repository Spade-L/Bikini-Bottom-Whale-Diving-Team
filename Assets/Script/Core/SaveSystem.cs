using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public List<string> flags;
    public List<string> collectedClueIds;
    public int timePeriod;
    public int investigationCount;
    public string sceneName;
    public float playerX;
    public float playerY;
}

/// <summary>
/// JSON 存档，写入 Application.persistentDataPath/save_<槽位>.json。
/// </summary>
public static class SaveSystem
{
    private static string GetPath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"save_{slot}.json");
    }

    public static bool HasSave(int slot = 0)
    {
        return File.Exists(GetPath(slot));
    }

    public static void Save(SaveData data, int slot = 0)
    {
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(GetPath(slot), json);
        Debug.Log($"[SaveSystem] 已存档: {GetPath(slot)}");
    }

    public static SaveData Load(int slot = 0)
    {
        string path = GetPath(slot);
        if (!File.Exists(path))
        {
            return null;
        }

        return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
    }

    public static void Delete(int slot = 0)
    {
        string path = GetPath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
