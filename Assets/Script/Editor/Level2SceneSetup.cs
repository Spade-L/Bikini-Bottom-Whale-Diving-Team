using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 一键生成 level2 的饮水机与隐藏空间配置
public static class Level2SceneSetup
{
// 推进 Level2SceneSetup 的当前步骤
    private const string ScenePath = "Assets/Scenes/level2.unity";
    private const string ClueDir = "Assets/GameData/Clues/";
// 在 Level2SceneSetup 中处理 推进 Level2SceneSetup 的当前步骤
    private const string DialogueDir = "Assets/GameData/Dialogues/";

// 设置 Setup 的目标状态
    [MenuItem("Trace Me/设置 level2 教室补充线索")]
    public static void Setup()
    {
// 执行场景切换
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject classroom = FindSceneObject("jiaoshi");
// 完成 Setup 的主要职责
        GameObject hiddenRoom = FindSceneObject("jiaoshi (1)");
        if (classroom == null || hiddenRoom == null)
        {
// 输出调试信息
            Debug.LogError("[Level2SceneSetup] 找不到 jiaoshi 或 jiaoshi (1)。");
            return;
        }

// 切换 Setup 的显示状态
        classroom.SetActive(true);
        hiddenRoom.SetActive(false);

// 缓存 Setup 所需引用
        GameObject dispenser = FindSceneObject("插画5 5_0");
        if (dispenser == null)
        {
// 在 Setup 中继续当前处理
            Debug.LogError("[Level2SceneSetup] 找不到饮水机对象：插画5 5_0。");
            return;
        }

// 同步 Setup 的相关数据
        BoxCollider2D dispenserCollider = GetOrAdd<BoxCollider2D>(dispenser);
        dispenserCollider.isTrigger = true;
// 同步 Setup 的相关数据（Setup 后续步骤）
        WaterDispenserInvestigation2D dispenserLogic = GetOrAdd<WaterDispenserInvestigation2D>(dispenser);
        Set(dispenserLogic, "firstDialogue", Load<DialogueData>(DialogueDir + "Dlg_school_water_dispenser.asset"));
// 推进 Setup 中的必要步骤
        Set(dispenserLogic, "revealDialogue", Load<DialogueData>(DialogueDir + "Dlg_school_water_dispenser_reveal.asset"));
        Set(dispenserLogic, "repeatDialogue", Load<DialogueData>(DialogueDir + "Dlg_school_water_dispenser_repeat.asset"));
// 推进 Setup 中的必要步骤（Setup）
        Set(dispenserLogic, "clueToGrant", Load<ClueData>(ClueDir + "Clue_school_water_dispenser.asset"));
        Set(dispenserLogic, "initialLocalPosition", new Vector3(18f, 8f, 0f));
// 在 Setup 中处理 Set
        Set(dispenserLogic, "movedLocalPosition", new Vector3(12f, 8f, 0f));
        Set(dispenserLogic, "movedFlag", "school_water_dispenser_moved");
// 在 Setup 中处理 Set（Setup 后续步骤）
        Set(dispenserLogic, "roomReadyFlag", "school_water_dispenser_room_ready");

// 获取 FindSceneObject 所需引用（Setup）
        GameObject hiddenRoomEntrance = FindSceneObject("hidden_room_files");
        if (hiddenRoomEntrance != null)
        {
// 同步 Setup 的相关数据（Setup 后续步骤）（65）
            System.Type transitionType = System.Type.GetType("HiddenRoomTransition2D, Assembly-CSharp");
            if (transitionType != null)
            {
// 同步 Setup 的相关数据（Setup 后续步骤）（69）
                Component transition = hiddenRoomEntrance.GetComponent(transitionType);
                if (transition == null)
                {
// 同步 Setup 的状态
                    transition = Undo.AddComponent(hiddenRoomEntrance, transitionType);
                }
// 在 Setup 中处理 Set（Setup 后续步骤）（后续处理 2）
                Set(transition, "classroomRoot", classroom);
                Set(transition, "hiddenRoomRoot", hiddenRoom);
// 在 Setup 中处理 Set（Setup 后续步骤）（后续处理 3）
                Set(transition, "requiredFlag", "school_water_dispenser_room_ready");
            }

// 同步 Setup 的相关数据（Setup 后续步骤）（83）
            CluePickup2D entranceClue = hiddenRoomEntrance.GetComponent<CluePickup2D>();
            if (entranceClue != null)
            {
// 更新启用状态
                entranceClue.enabled = false;
            }
        }

// 在 Setup 中处理 CreateClue
        CreateClue("Rank Paper", classroom, new Vector3(16.5f, 1.5f, 0f), "school_paper_rank");
        CreateClue("Broken Counseling Paper", classroom, new Vector3(10f, 1.5f, 0f), "school_paper_counseling");
// 在 Setup 中处理 CreateClue（Setup 后续步骤）
        CreateClue("Notice Board", classroom, new Vector3(7f, 3f, 0f), "school_notice_board");
        CreateClue("Hidden Room Files", hiddenRoom, new Vector3(12f, 3f, 0f), "school_hidden_room_files");

// 在 Setup 中继续当前处理（Setup 后续步骤）
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
// 使用 Setup 所需功能
        AssetDatabase.SaveAssets();
        Debug.Log("[Level2SceneSetup] level2 补充线索与饮水机设置完成。");
    }

// 创建 CreateClue 对应对象
    private static void CreateClue(string name, GameObject parent, Vector3 position, string clueId)
    {
// 缓存 CreateClue 所需引用
        GameObject go = GetOrCreate(name, parent.transform);
        go.transform.position = position;
// 同步 CreateClue 的相关数据
        BoxCollider2D collider = GetOrAdd<BoxCollider2D>(go);
        collider.isTrigger = true;
// 同步 CreateClue 的内部状态
        collider.size = new Vector2(1.2f, 1.2f);
        CluePickup2D pickup = GetOrAdd<CluePickup2D>(go);
// 推进 CreateClue 中的必要步骤
        Set(pickup, "inspectDialogue", Load<DialogueData>(DialogueDir + "Dlg_" + clueId + ".asset"));
        Set(pickup, "clueToGrant", Load<ClueData>(ClueDir + "Clue_" + clueId + ".asset"));
// 推进 CreateClue 中的必要步骤（CreateClue）
        Set(pickup, "disappearAfterPickup", true);
        Set(pickup, "countsAsInvestigation", true);
    }

// 获取 FindSceneObject 所需引用
    private static GameObject FindSceneObject(string name)
    {
// 遍历全部元素
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
// 缓存 FindSceneObject 所需引用
            Transform match = FindInHierarchy(root.transform, name);
            if (match != null)
            {
// 返回 FindSceneObject 的处理结果
                return match.gameObject;
            }
        }

// 在 FindSceneObject 中处理 返回 FindSceneObject 的处理结果
        return null;
    }

// 获取 FindInHierarchy 所需引用
    private static Transform FindInHierarchy(Transform current, string name)
    {
// 检查 FindInHierarchy 的前置条件
        if (current.name == name)
        {
// 返回 FindInHierarchy 的处理结果
            return current;
        }

// 在 FindInHierarchy 中继续当前处理
        foreach (Transform child in current)
        {
// 获取 FindInHierarchy 所需引用（FindInHierarchy）
            Transform match = FindInHierarchy(child, name);
            if (match != null)
            {
// 返回 FindInHierarchy 的处理结果（FindInHierarchy）
                return match;
            }
        }

// 返回 FindInHierarchy 的处理结果（FindInHierarchy）（return）
        return null;
    }
// 获取 GetOrCreate 所需引用
    private static GameObject GetOrCreate(string name, Transform parent)
    {
// 获取 FindSceneObject 所需引用（GetOrCreate）
        GameObject go = FindSceneObject(name);
        if (go == null)
        {
// 同步 GetOrCreate 的内部状态
            go = new GameObject(name);
        }
// 检查 GetOrCreate 的前置条件
        if (parent != null && go.transform.parent != parent)
        {
// 使用 GetOrCreate 所需功能
            go.transform.SetParent(parent, true);
        }
// 返回 GetOrCreate 的处理结果
        return go;
    }

// 推进 GetOrCreate 的当前步骤
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
// 同步 GetOrCreate 的相关数据
        T component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

// 推进 GetOrCreate 的当前步骤（GetOrCreate）
    private static T Load<T>(string path) where T : Object
    {
// 同步 GetOrCreate 的相关数据（GetOrCreate 后续步骤）
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
// 在 GetOrCreate 中继续当前处理
            Debug.LogError("[Level2SceneSetup] 找不到资产: " + path);
        }
// 返回 GetOrCreate 的处理结果（GetOrCreate）
        return asset;
    }

// 设置 Set 的目标状态
    private static void Set(Object target, string property, Object value)
    {
// 同步 Set 的相关数据
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = value;
// 使用 Set 所需功能
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

// 在 Set 中处理 Set
    private static void Set(Object target, string property, string value)
    {
// 同步 Set 的相关数据（Set 后续步骤）
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).stringValue = value;
// 使用 Set 所需功能（Set）
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

// 在 Set 中处理 Set（Set 后续步骤）
    private static void Set(Object target, string property, bool value)
    {
// 同步 Set 的相关数据（Set 后续步骤）（236）
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).boolValue = value;
// 在 Set 中处理 ApplyModifiedPropertiesWithoutUndo
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

// 在 Set 中处理 Set（Set 后续步骤）（后续处理 2）
    private static void Set(Object target, string property, Vector3 value)
    {
// 同步 Set 的相关数据（Set 后续步骤）（246）
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).vector3Value = value;
// 在 Set 中处理 ApplyModifiedPropertiesWithoutUndo（Set 后续步骤）
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
