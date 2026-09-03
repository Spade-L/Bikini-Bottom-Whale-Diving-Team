using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Level2SceneSetup
{
    private const string ScenePath = "Assets/Scenes/level2.unity";
    private const string ClueDir = "Assets/GameData/Clues/";
    private const string DialogueDir = "Assets/GameData/Dialogues/";

    [MenuItem("Trace Me/设置 level2 教室补充线索")]
    public static void Setup()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject classroom = FindSceneObject("jiaoshi");
        GameObject hiddenRoom = FindSceneObject("jiaoshi (1)");
        if (classroom == null || hiddenRoom == null)
        {
            Debug.LogError("[Level2SceneSetup] 找不到 jiaoshi 或 jiaoshi (1)。");
            return;
        }

        classroom.SetActive(true);
        hiddenRoom.SetActive(false);

        GameObject dispenser = GetOrCreate("Water Dispenser", null);
        dispenser.transform.position = new Vector3(18f, 2f, 0f);
        BoxCollider2D dispenserCollider = GetOrAdd<BoxCollider2D>(dispenser);
        dispenserCollider.isTrigger = true;
        dispenserCollider.size = new Vector2(1.5f, 2f);
        WaterDispenserInvestigation2D dispenserLogic = GetOrAdd<WaterDispenserInvestigation2D>(dispenser);
        Set(dispenserLogic, "firstDialogue", Load<DialogueData>(DialogueDir + "Dlg_school_water_dispenser.asset"));
        Set(dispenserLogic, "revealDialogue", Load<DialogueData>(DialogueDir + "Dlg_school_water_dispenser_reveal.asset"));
        Set(dispenserLogic, "repeatDialogue", Load<DialogueData>(DialogueDir + "Dlg_school_water_dispenser_repeat.asset"));
        Set(dispenserLogic, "clueToGrant", Load<ClueData>(ClueDir + "Clue_school_water_dispenser.asset"));
        Set(dispenserLogic, "classroomRoot", classroom);
        Set(dispenserLogic, "hiddenRoomRoot", hiddenRoom);
        Set(dispenserLogic, "initialPosition", new Vector3(18f, 2f, 0f));
        Set(dispenserLogic, "movedPosition", new Vector3(12f, 2f, 0f));
        Set(dispenserLogic, "movedFlag", "school_water_dispenser_moved");

        CreateClue("Rank Paper", classroom, new Vector3(16.5f, 1.5f, 0f), "school_paper_rank");
        CreateClue("Broken Counseling Paper", classroom, new Vector3(10f, 1.5f, 0f), "school_paper_counseling");
        CreateClue("Notice Board", classroom, new Vector3(7f, 3f, 0f), "school_notice_board");
        CreateClue("Cleaning Tools", classroom, new Vector3(4f, 1.5f, 0f), "school_cleaning_tools");
        CreateClue("Hidden Room Files", hiddenRoom, new Vector3(12f, 3f, 0f), "school_hidden_room_files");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Level2SceneSetup] level2 补充线索与饮水机设置完成。");
    }

    private static void CreateClue(string name, GameObject parent, Vector3 position, string clueId)
    {
        GameObject go = GetOrCreate(name, parent.transform);
        go.transform.position = position;
        BoxCollider2D collider = GetOrAdd<BoxCollider2D>(go);
        collider.isTrigger = true;
        collider.size = new Vector2(1.2f, 1.2f);
        CluePickup2D pickup = GetOrAdd<CluePickup2D>(go);
        Set(pickup, "inspectDialogue", Load<DialogueData>(DialogueDir + "Dlg_" + clueId + ".asset"));
        Set(pickup, "clueToGrant", Load<ClueData>(ClueDir + "Clue_" + clueId + ".asset"));
        Set(pickup, "disappearAfterPickup", false);
        Set(pickup, "countsAsInvestigation", true);
    }

    private static GameObject FindSceneObject(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform match = FindInHierarchy(root.transform, name);
            if (match != null)
            {
                return match.gameObject;
            }
        }

        return null;
    }

    private static Transform FindInHierarchy(Transform current, string name)
    {
        if (current.name == name)
        {
            return current;
        }

        foreach (Transform child in current)
        {
            Transform match = FindInHierarchy(child, name);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
    private static GameObject GetOrCreate(string name, Transform parent)
    {
        GameObject go = FindSceneObject(name);
        if (go == null)
        {
            go = new GameObject(name);
        }
        if (parent != null && go.transform.parent != parent)
        {
            go.transform.SetParent(parent, true);
        }
        return go;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

    private static T Load<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            Debug.LogError("[Level2SceneSetup] 找不到资产: " + path);
        }
        return asset;
    }

    private static void Set(Object target, string property, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(Object target, string property, string value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(Object target, string property, bool value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(Object target, string property, Vector3 value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).vector3Value = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
