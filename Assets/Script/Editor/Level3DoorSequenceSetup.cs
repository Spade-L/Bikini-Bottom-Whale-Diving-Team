using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Level3DoorSequenceSetup
{
    private const string ScenePath = "Assets/Scenes/level3.unity";
    private const string DialogueFolder = "Assets/GameData/Dialogues/level3";
    private const string HandprintBlockedDialoguePath = "Assets/GameData/Dialogues/Dlg_store_handprint_blocked.asset";
    private const string DoorLockedDialoguePath = DialogueFolder + "/Dlg_store_door_locked.asset";
    private const string RevealDialoguePath = DialogueFolder + "/Dlg_store_shadow_reveal.asset";
    private const string DeliveryDialoguePath = DialogueFolder + "/Dlg_store_shadow_delivery.asset";
    private const string DisappearanceDialoguePath = DialogueFolder + "/Dlg_store_shadow_disappearance.asset";
    private const string ExitReadyDialoguePath = DialogueFolder + "/Dlg_store_exit_ready.asset";
    private const string CompletedFlag = "level3_door_sequence_done";
    private const string ResolvedFlag = "level3_store_shadow_resolved";
    private const string ToyDeliveredFlag = "level3_store_toy_delivered";

    private static readonly string[] InvestigationIds =
    {
        "store_note",
        "store_poster",
        "store_vegetables",
        "store_handprint",
        "store_fruit",
        "store_toy"
    };

    [MenuItem("Trace Me/设置 level3 门前黑影流程")]
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
        SceneDoor sceneDoor = door.GetComponent<SceneDoor>();
        CluePickup2D handprintPickup = handprint.GetComponent<CluePickup2D>();
        if (sps0Animator == null || spsAnimator == null || sceneDoor == null || handprintPickup == null)
        {
            Debug.LogError("[Level3DoorSequenceSetup] Animator、SceneDoor 或 Handprint CluePickup2D 缺失，未修改场景。");
            return;
        }

        DialogueData handprintBlockedDialogue = ConfigureDialogue(
            HandprintBlockedDialoguePath,
            new DialogueLine("我", "这里被挡住了。"));
        DialogueData doorLockedDialogue = ConfigureDialogue(
            DoorLockedDialoguePath,
            new DialogueLine("我", "真奇怪，明明进来的时候没上锁的"),
            new DialogueLine(string.Empty, "（似乎有股强大的推力把门抵住了）"));
        DialogueData revealDialogue = ConfigureDialogue(
            RevealDialoguePath,
            new DialogueLine(string.Empty, "…… ……"),
            new DialogueLine(string.Empty, "嗡——"),
            new DialogueLine("我", "怎么回事！！"),
            new DialogueLine(string.Empty, "眼前浮现出一个无法辨认形状的身影。"),
            new DialogueLine("？？？", "嘻嘻……"),
            new DialogueLine("？？？", "我在找一个丢失的东西。"));
        DialogueData deliveryDialogue = ConfigureDialogue(
            DeliveryDialoguePath,
            new DialogueLine("我", "给你。"),
            new DialogueLine("？？？", "哼哼……"));
        DialogueData disappearanceDialogue = ConfigureDialogue(
            DisappearanceDialoguePath,
            new DialogueLine(string.Empty, "身影渐渐消失了。"),
            new DialogueLine(string.Empty, "远处传来东西碎落的声音。"));
        DialogueData exitReadyDialogue = ConfigureDialogue(
            ExitReadyDialoguePath,
            new DialogueLine("我", "似乎可以出门了。"));

        if (handprintBlockedDialogue == null || doorLockedDialogue == null || revealDialogue == null
            || deliveryDialogue == null || disappearanceDialogue == null || exitReadyDialogue == null)
        {
            Debug.LogError("[Level3DoorSequenceSetup] 对白资源创建失败，未保存场景。");
            return;
        }

        GameObject handBinding = FindSceneObject("插画5 10_0");
        GameObject eyeBinding = FindSceneObject("插画5 11");
        if (handBinding != null) handBinding.SetActive(true);
        if (eyeBinding != null) eyeBinding.SetActive(true);

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

        SerializedProperty investigations = serialized.FindProperty("requiredInvestigationIds");
        investigations.arraySize = InvestigationIds.Length;
        for (int i = 0; i < InvestigationIds.Length; i++)
        {
            investigations.GetArrayElementAtIndex(i).stringValue = InvestigationIds[i];
        }

        serialized.FindProperty("hand").objectReferenceValue = hand;
        serialized.FindProperty("eye").objectReferenceValue = eye;
        SerializedProperty sequenceOnlyObjects = serialized.FindProperty("sequenceOnlyObjects");
        sequenceOnlyObjects.arraySize = 0;
        serialized.FindProperty("revealDialogue").objectReferenceValue = revealDialogue;
        serialized.FindProperty("deliveryDialogue").objectReferenceValue = deliveryDialogue;
        serialized.FindProperty("disappearanceDialogue").objectReferenceValue = disappearanceDialogue;
        serialized.FindProperty("exitReadyDialogue").objectReferenceValue = exitReadyDialogue;
        serialized.FindProperty("fallingBreakingSound").objectReferenceValue = null;
        serialized.FindProperty("completedFlag").stringValue = CompletedFlag;
        serialized.FindProperty("resolvedFlag").stringValue = ResolvedFlag;
        serialized.FindProperty("toyDeliveredFlag").stringValue = ToyDeliveredFlag;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(sequence);

        ConfigureDoor(sceneDoor, doorLockedDialogue);
        Set(handprintPickup, "lockedByFlag", CompletedFlag);
        Set(handprintPickup, "lockedDialogue", handprintBlockedDialogue);
        DisableDuplicateLevel3ClearPresentation();

        hand.SetActive(false);
        eye.SetActive(false);
        sps0Root.SetActive(false);
        spsRoot.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Level3DoorSequenceSetup] Level3 六调查、黑影交付和出口锁定接线完成。碎落音效未发现匹配资源，引用保持为空。");
    }

    private static void ConfigureDoor(SceneDoor door, DialogueData lockedDialogue)
    {
        SerializedObject serialized = new SerializedObject(door);
        SerializedProperty condition = serialized.FindProperty("openCondition");
        condition.FindPropertyRelative("minTimePeriod").intValue = -1;
        condition.FindPropertyRelative("maxTimePeriod").intValue = -1;
        SerializedProperty requiredFlags = condition.FindPropertyRelative("requiredFlags");
        requiredFlags.arraySize = 1;
        requiredFlags.GetArrayElementAtIndex(0).stringValue = ResolvedFlag;
        condition.FindPropertyRelative("forbiddenFlags").arraySize = 0;
        condition.FindPropertyRelative("requiredClues").arraySize = 0;
        serialized.FindProperty("lockedDialogue").objectReferenceValue = lockedDialogue;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(door);
    }

    private static void DisableDuplicateLevel3ClearPresentation()
    {
        SceneClueTracker tracker = Object.FindFirstObjectByType<SceneClueTracker>();
        if (tracker == null)
        {
            Debug.LogWarning("[Level3DoorSequenceSetup] 未找到 SceneClueTracker，未处理重复清场演出。");
            return;
        }

        SerializedObject serialized = new SerializedObject(tracker);
        serialized.FindProperty("brotherShadow").objectReferenceValue = null;
        serialized.FindProperty("blackout").objectReferenceValue = null;
        serialized.FindProperty("clearMonologue").objectReferenceValue = null;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(tracker);
    }

    private static DialogueData ConfigureDialogue(string path, params DialogueLine[] dialogueLines)
    {
        DialogueData dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
        if (dialogue == null)
        {
            dialogue = ScriptableObject.CreateInstance<DialogueData>();
            AssetDatabase.CreateAsset(dialogue, path);
        }

        SerializedObject serialized = new SerializedObject(dialogue);
        SerializedProperty lines = serialized.FindProperty("lines");
        lines.arraySize = dialogueLines.Length;
        for (int i = 0; i < dialogueLines.Length; i++)
        {
            SerializedProperty line = lines.GetArrayElementAtIndex(i);
            line.FindPropertyRelative("character").objectReferenceValue = null;
            line.FindPropertyRelative("expression").stringValue = string.Empty;
            line.FindPropertyRelative("speakerName").stringValue = dialogueLines[i].speaker;
            line.FindPropertyRelative("text").stringValue = dialogueLines[i].text;
        }

        serialized.FindProperty("countsAsInvestigation").boolValue = false;
        serialized.FindProperty("setFlagsOnComplete").arraySize = 0;
        serialized.FindProperty("advanceTimeOnComplete").intValue = 0;
        serialized.FindProperty("grantCluesOnComplete").arraySize = 0;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dialogue);
        return dialogue;
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
        if (go == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        }
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
        EditorUtility.SetDirty(target);
    }

    private static void Set(Object target, string property, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private readonly struct DialogueLine
    {
        public readonly string speaker;
        public readonly string text;

        public DialogueLine(string speaker, string text)
        {
            this.speaker = speaker;
            this.text = text;
        }
    }
}
