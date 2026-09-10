using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 定义 Level2SceneSetup 类型
public static class Level2SceneSetup
{
// 更新当前逻辑
    private const string ScenePath = "Assets/Scenes/level2.unity";
    private const string ClueDir = "Assets/GameData/Clues/";
// 更新当前逻辑
    private const string DialogueDir = "Assets/GameData/Dialogues/";

// 调用 MenuItem
    [MenuItem("Trace Me/设置 level2 教室补充线索")]
    public static void Setup()
    {
// 执行场景切换
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject classroom = FindSceneObject("jiaoshi");
// 定义 jiaoshi 方法
        GameObject hiddenRoom = FindSceneObject("jiaoshi (1)");
        if (classroom == null || hiddenRoom == null)
        {
// 输出调试信息
            Debug.LogError("[Level2SceneSetup] 找不到 jiaoshi 或 jiaoshi (1)。");
            return;
        }

// 切换显示状态
        classroom.SetActive(true);
        hiddenRoom.SetActive(false);

// 定义 FindSceneObject 方法
        GameObject dispenser = FindSceneObject("插画5 5_0");
        if (dispenser == null)
        {
// 输出调试信息
            Debug.LogError("[Level2SceneSetup] 找不到饮水机对象：插画5 5_0。");
            return;
        }

// 保存 dispenserCollider 数据
        BoxCollider2D dispenserCollider = GetOrAdd<BoxCollider2D>(dispenser);
        dispenserCollider.isTrigger = true;
// 保存 dispenserLogic 数据
        WaterDispenserInvestigation2D dispenserLogic = GetOrAdd<WaterDispenserInvestigation2D>(dispenser);
        Set(dispenserLogic, "firstDialogue", Load<DialogueData>(DialogueDir + "Dlg_school_water_dispenser.asset"));
// 执行 Set
        Set(dispenserLogic, "revealDialogue", Load<DialogueData>(DialogueDir + "Dlg_school_water_dispenser_reveal.asset"));
        Set(dispenserLogic, "repeatDialogue", Load<DialogueData>(DialogueDir + "Dlg_school_water_dispenser_repeat.asset"));
// 执行 Set
        Set(dispenserLogic, "clueToGrant", Load<ClueData>(ClueDir + "Clue_school_water_dispenser.asset"));
        Set(dispenserLogic, "initialLocalPosition", new Vector3(18f, 8f, 0f));
// 执行 Set
        Set(dispenserLogic, "movedLocalPosition", new Vector3(12f, 8f, 0f));
        Set(dispenserLogic, "movedFlag", "school_water_dispenser_moved");
// 执行 Set
        Set(dispenserLogic, "roomReadyFlag", "school_water_dispenser_room_ready");

// 定义 FindSceneObject 方法
        GameObject hiddenRoomEntrance = FindSceneObject("hidden_room_files");
        if (hiddenRoomEntrance != null)
        {
// 保存 transitionType 数据
            System.Type transitionType = System.Type.GetType("HiddenRoomTransition2D, Assembly-CSharp");
            if (transitionType != null)
            {
// 保存 transition 数据
                Component transition = hiddenRoomEntrance.GetComponent(transitionType);
                if (transition == null)
                {
// 更新当前状态
                    transition = Undo.AddComponent(hiddenRoomEntrance, transitionType);
                }
// 执行 Set
                Set(transition, "classroomRoot", classroom);
                Set(transition, "hiddenRoomRoot", hiddenRoom);
// 执行 Set
                Set(transition, "requiredFlag", "school_water_dispenser_room_ready");
            }

// 保存 entranceClue 数据
            CluePickup2D entranceClue = hiddenRoomEntrance.GetComponent<CluePickup2D>();
            if (entranceClue != null)
            {
// 更新启用状态
                entranceClue.enabled = false;
            }
        }

// 执行 CreateClue
        CreateClue("Rank Paper", classroom, new Vector3(16.5f, 1.5f, 0f), "school_paper_rank");
        CreateClue("Broken Counseling Paper", classroom, new Vector3(10f, 1.5f, 0f), "school_paper_counseling");
// 执行 CreateClue
        CreateClue("Notice Board", classroom, new Vector3(7f, 3f, 0f), "school_notice_board");
        CreateClue("Hidden Room Files", hiddenRoom, new Vector3(12f, 3f, 0f), "school_hidden_room_files");

// 执行场景切换
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
// 调用 SaveAssets
        AssetDatabase.SaveAssets();
        Debug.Log("[Level2SceneSetup] level2 补充线索与饮水机设置完成。");
    }

// 定义 CreateClue 方法
    private static void CreateClue(string name, GameObject parent, Vector3 position, string clueId)
    {
// 定义 GetOrCreate 方法
        GameObject go = GetOrCreate(name, parent.transform);
        go.transform.position = position;
// 保存 collider 数据
        BoxCollider2D collider = GetOrAdd<BoxCollider2D>(go);
        collider.isTrigger = true;
// 更新当前状态
        collider.size = new Vector2(1.2f, 1.2f);
        CluePickup2D pickup = GetOrAdd<CluePickup2D>(go);
// 执行 Set
        Set(pickup, "inspectDialogue", Load<DialogueData>(DialogueDir + "Dlg_" + clueId + ".asset"));
        Set(pickup, "clueToGrant", Load<ClueData>(ClueDir + "Clue_" + clueId + ".asset"));
// 执行 Set
        Set(pickup, "disappearAfterPickup", true);
        Set(pickup, "countsAsInvestigation", true);
    }

// 定义 FindSceneObject 方法
    private static GameObject FindSceneObject(string name)
    {
// 遍历全部元素
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
// 定义 FindInHierarchy 方法
            Transform match = FindInHierarchy(root.transform, name);
            if (match != null)
            {
// 返回当前结果
                return match.gameObject;
            }
        }

// 返回当前结果
        return null;
    }

// 定义 FindInHierarchy 方法
    private static Transform FindInHierarchy(Transform current, string name)
    {
// 判断当前条件
        if (current.name == name)
        {
// 返回当前结果
            return current;
        }

// 遍历全部元素
        foreach (Transform child in current)
        {
// 定义 FindInHierarchy 方法
            Transform match = FindInHierarchy(child, name);
            if (match != null)
            {
// 返回当前结果
                return match;
            }
        }

// 返回当前结果
        return null;
    }
// 定义 GetOrCreate 方法
    private static GameObject GetOrCreate(string name, Transform parent)
    {
// 定义 FindSceneObject 方法
        GameObject go = FindSceneObject(name);
        if (go == null)
        {
// 更新当前状态
            go = new GameObject(name);
        }
// 判断当前条件
        if (parent != null && go.transform.parent != parent)
        {
// 调用 SetParent
            go.transform.SetParent(parent, true);
        }
// 返回当前结果
        return go;
    }

// 更新当前逻辑
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
// 保存 component 数据
        T component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

// 更新当前逻辑
    private static T Load<T>(string path) where T : Object
    {
// 保存 asset 数据
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
// 输出调试信息
            Debug.LogError("[Level2SceneSetup] 找不到资产: " + path);
        }
// 返回当前结果
        return asset;
    }

// 定义 Set 方法
    private static void Set(Object target, string property, Object value)
    {
// 保存 serialized 数据
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = value;
// 调用 ApplyModifiedPropertiesWithoutUndo
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

// 定义 Set 方法
    private static void Set(Object target, string property, string value)
    {
// 保存 serialized 数据
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).stringValue = value;
// 调用 ApplyModifiedPropertiesWithoutUndo
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

// 定义 Set 方法
    private static void Set(Object target, string property, bool value)
    {
// 保存 serialized 数据
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).boolValue = value;
// 调用 ApplyModifiedPropertiesWithoutUndo
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

// 定义 Set 方法
    private static void Set(Object target, string property, Vector3 value)
    {
// 保存 serialized 数据
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).vector3Value = value;
// 调用 ApplyModifiedPropertiesWithoutUndo
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
