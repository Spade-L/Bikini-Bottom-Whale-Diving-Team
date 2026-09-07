using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Level3DoorSequenceSetup
{
    private const string ScenePath = "Assets/Scenes/level3.unity";
    private const string DialoguePath = "Assets/GameData/Dialogues/Dlg_store_handprint_blocked.asset";

    [MenuItem("Trace Me/设置 level3 门前动画")]
    public static void Setup()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject door = FindSceneObject("Door");
        GameObject handprint = FindSceneObject("Handprint on the door");
        GameObject sps0Root = FindSceneObject("插画5 4_0 (1)");
        GameObject spsRoot = FindSceneObject("插画5 5_0");
        GameObject hand = FindSceneObject("hand");
        GameObject eye = FindSceneObject("eye");

        if (door == null || handprint == null || sps0Root == null || spsRoot == null || hand == null || eye == null)
        {
            Debug.LogError("[Level3DoorSequenceSetup] 缺少 Door、Handprint、sps0/sps 根对象或 hand/eye，未修改场景。");
            return;
        }

        Animator sps0Animator = sps0Root.GetComponent<Animator>();
        Animator spsAnimator = spsRoot.GetComponent<Animator>();
        if (sps0Animator == null || spsAnimator == null)
        {
            Debug.LogError("[Level3DoorSequenceSetup] sps0 或 sps 根对象缺少 Animator，未修改场景。");
            return;
        }

        DialogueData blockedDialogue = GetOrCreateBlockedDialogue();
        if (blockedDialogue == null)
        {
            return;
        }

        GameObject handBinding = EnsureSpsBindingObject(spsRoot.transform, "插画5 10_0", hand.GetComponent<SpriteRenderer>(), 7);
        GameObject eyeBinding = EnsureSpsBindingObject(spsRoot.transform, "插画5 11", eye.GetComponent<SpriteRenderer>(), 2);

        GameObject trigger = GetOrCreate("Level3 Door Sequence Trigger", null);
        trigger.transform.position = door.transform.position + Vector3.left * 1.5f;
        BoxCollider2D collider = GetOrAdd<BoxCollider2D>(trigger);
        collider.isTrigger = true;
        collider.size = new Vector2(1.5f, 3f);

        Level3DoorSequence sequence = GetOrAdd<Level3DoorSequence>(trigger);
        SerializedObject serialized = new SerializedObject(sequence);
        SerializedProperty steps = serialized.FindProperty("steps");
        steps.arraySize = 2;
        ConfigureStep(steps.GetArrayElementAtIndex(0), "sps0", sps0Root, sps0Animator, "sps0", 0.6666667f, false, true);
        ConfigureStep(steps.GetArrayElementAtIndex(1), "sps", spsRoot, spsAnimator, "sps", 1.25f, false, false);
        serialized.FindProperty("hand").objectReferenceValue = hand;
        serialized.FindProperty("eye").objectReferenceValue = eye;
        SerializedProperty sequenceOnlyObjects = serialized.FindProperty("sequenceOnlyObjects");
        sequenceOnlyObjects.arraySize = 2;
        sequenceOnlyObjects.GetArrayElementAtIndex(0).objectReferenceValue = handBinding;
        sequenceOnlyObjects.GetArrayElementAtIndex(1).objectReferenceValue = eyeBinding;
        serialized.FindProperty("completedFlag").stringValue = "level3_door_sequence_done";
        serialized.ApplyModifiedPropertiesWithoutUndo();

        CluePickup2D pickup = handprint.GetComponent<CluePickup2D>();
        if (pickup == null)
        {
            Debug.LogError("[Level3DoorSequenceSetup] Handprint on the door 缺少 CluePickup2D，未接线封锁对白。");
            return;
        }

        Set(pickup, "lockedByFlag", "level3_door_sequence_done");
        Set(pickup, "lockedDialogue", blockedDialogue);
        hand.SetActive(false);
        eye.SetActive(false);
        sps0Root.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Level3DoorSequenceSetup] level3 门前动画与手印封锁对白设置完成。");
    }

    private static void ConfigureStep(SerializedProperty step, string label, GameObject root, Animator animator,
        string stateName, float duration, bool showResults, bool hideRoot)
    {
        step.FindPropertyRelative("label").stringValue = label;
        step.FindPropertyRelative("root").objectReferenceValue = root;
        step.FindPropertyRelative("animator").objectReferenceValue = animator;
        step.FindPropertyRelative("stateName").stringValue = stateName;
        step.FindPropertyRelative("showResultObjectsDuringStep").boolValue = showResults;
        step.FindPropertyRelative("hideRootAfterStep").boolValue = hideRoot;
        step.FindPropertyRelative("duration").floatValue = duration;
    }

    private static DialogueData GetOrCreateBlockedDialogue()
    {
        DialogueData dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(DialoguePath);
        if (dialogue == null)
        {
            dialogue = ScriptableObject.CreateInstance<DialogueData>();
            AssetDatabase.CreateAsset(dialogue, DialoguePath);
        }

        SerializedObject serialized = new SerializedObject(dialogue);
        SerializedProperty lines = serialized.FindProperty("lines");
        lines.arraySize = 1;
        SerializedProperty line = lines.GetArrayElementAtIndex(0);
        line.FindPropertyRelative("speakerName").stringValue = "我";
        line.FindPropertyRelative("text").stringValue = "这里被挡住了。";
        serialized.FindProperty("countsAsInvestigation").boolValue = false;
        serialized.FindProperty("setFlagsOnComplete").arraySize = 0;
        serialized.FindProperty("advanceTimeOnComplete").intValue = 0;
        serialized.FindProperty("grantCluesOnComplete").arraySize = 0;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dialogue);
        return dialogue;
    }

    private static GameObject EnsureSpsBindingObject(Transform parent, string name, SpriteRenderer source, int sortingOrder)
    {
        Transform child = parent.Find(name);
        GameObject go = child != null ? child.gameObject : new GameObject(name);
        go.transform.SetParent(parent, false);
        SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(go);
        renderer.sprite = source != null ? source.sprite : null;
        renderer.sortingLayerID = source != null ? source.sortingLayerID : 0;
        renderer.sortingOrder = sortingOrder;
        renderer.sharedMaterial = source != null ? source.sharedMaterial : null;
        go.SetActive(true);
        return go;
    }

    private static GameObject FindSceneObject(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform match = FindInHierarchy(root.transform, name);
            if (match != null) return match.gameObject;
        }
        return null;
    }

    private static Transform FindInHierarchy(Transform current, string name)
    {
        if (current.name == name) return current;
        foreach (Transform child in current)
        {
            Transform match = FindInHierarchy(child, name);
            if (match != null) return match;
        }
        return null;
    }

    private static GameObject GetOrCreate(string name, Transform parent)
    {
        GameObject go = FindSceneObject(name);
        if (go == null) go = new GameObject(name);
        if (parent != null && go.transform.parent != parent) go.transform.SetParent(parent, true);
        return go;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

    private static void Set(Object target, string property, string value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(Object target, string property, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
