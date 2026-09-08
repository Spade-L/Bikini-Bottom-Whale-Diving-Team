using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 纯数据载体，字段名同时是 JsonUtility 写入的 JSON 键名
[System.Serializable]
public class SaveData
{
    // GameManager 的剧情进度快照
    public List<string> flags;
    public List<string> collectedClueIds;
    public int timePeriod;
    public int investigationCount;

    // 场景和二维坐标用于在读取后恢复玩家位置
    public string sceneName;
    public float playerX;
    public float playerY;
}

public static class SaveSystem
{
    // 槽位编号直接参与文件名；不同槽位互不覆盖
    private static string GetPath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"save_{slot}.json");
    }

    // 仅检查文件是否存在，不验证 JSON 完整性
    public static bool HasSave(int slot = 0)
    {
        return File.Exists(GetPath(slot));
    }

    // 立即以格式化 JSON 覆盖对应槽位，调用方负责先汇集当前运行状态
    public static void Save(SaveData data, int slot = 0)
    {
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(GetPath(slot), json);
        Debug.Log($"[SaveSystem] 已存档: {GetPath(slot)}");
    }

    // 缺档返回 null，由调用方决定显示新游戏流程还是报错提示
    public static SaveData Load(int slot = 0)
    {
        string path = GetPath(slot);
        if (!File.Exists(path))
        {
            return null;
        }

        return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
    }

    // 删除操作保持幂等：槽位本来为空时不抛出异常
    public static void Delete(int slot = 0)
    {
        string path = GetPath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
