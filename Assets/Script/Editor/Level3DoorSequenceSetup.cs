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
    private const string MissingToyDialoguePath = DialogueFolder + "/Dlg_store_missing_toy.asset";
    private const string RedToyDialoguePath = DialogueFolder + "/Dlg_store_red_toy.asset";
    private const string RedToyCluePath = "Assets/GameData/Clues/level3/Clue_store_red_toy.asset";
    private const string CompletedFlag = "level3_door_sequence_done";
    private const string ResolvedFlag = "level3_store_shadow_resolved";
    private const string ToyDeliveredFlag = "level3_store_toy_delivered";
    private const string RedToyPickupFlag = "picked_store_red_toy";

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
        DialogueData missingToyDialogue = ConfigureDialogue(
            MissingToyDialoguePath,
            new DialogueLine("？？？", "想出去嘛，只要找到我丢失的东西就可以啦"));
        DialogueData redToyDialogue = ConfigureDialogue(
            RedToyDialoguePath,
            new DialogueLine(string.Empty, "货架旁边放着一只红色的玩偶。"),
            new DialogueLine("我", "这是它丢失的东西吗？"));
        ClueData redToyClue = ConfigureRedToyClue();

        if (handprintBlockedDialogue == null || doorLockedDialogue == null || revealDialogue == null
            || deliveryDialogue == null || disappearanceDialogue == null || exitReadyDialogue == null
            || missingToyDialogue == null || redToyDialogue == null || redToyClue == null)
        {
            Debug.LogError("[Level3DoorSequenceSetup] 对白或线索资源创建失败，未保存场景。");
            return;
        }

        GameObject redToy = ConfigureRedToy(redToyClue, redToyDialogue);
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
        ConfigureStep(steps.GetArrayElementAtIndex(0), "sps0", sps0Root, sps0Animator, "sps0", 0.2f, false, true);
        ConfigureStep(steps.GetArrayElementAtIndex(1), "sps", spsRoot, spsAnimator, "sps", 1.25f, false, false);

        SerializedProperty investigations = serialized.FindProperty("requiredInvestigationIds");
        investigations.arraySize = InvestigationIds.Length;
        for (int i = 0; i < InvestigationIds.Length; i++)
        {
            investigations.GetArrayElementAtIndex(i).stringValue = InvestigationIds[i];
        }

        serialized.FindProperty("hand").objectReferenceValue = hand;
        serialized.FindProperty("eye").objectReferenceValue = eye;
        serialized.FindProperty("redToyGameObject").objectReferenceValue = redToy;
        SerializedProperty sequenceOnlyObjects = serialized.FindProperty("sequenceOnlyObjects");
        sequenceOnlyObjects.arraySize = 0;
        serialized.FindProperty("revealDialogue").objectReferenceValue = revealDialogue;
        serialized.FindProperty("missingToyDialogue").objectReferenceValue = missingToyDialogue;
        serialized.FindProperty("deliveryDialogue").objectReferenceValue = deliveryDialogue;
        serialized.FindProperty("disappearanceDialogue").objectReferenceValue = disappearanceDialogue;
        serialized.FindProperty("exitReadyDialogue").objectReferenceValue = exitReadyDialogue;
        serialized.FindProperty("fallingBreakingSound").objectReferenceValue = null;
        serialized.FindProperty("completedFlag").stringValue = CompletedFlag;
        serialized.FindProperty("resolvedFlag").stringValue = ResolvedFlag;
        serialized.FindProperty("toyDeliveredFlag").stringValue = ToyDeliveredFlag;
        serialized.FindProperty("redToyPickupFlag").stringValue = RedToyPickupFlag;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(sequence);

        ConfigureDoor(sceneDoor, doorLockedDialogue);
        Set(handprintPickup, "lockedByFlag", CompletedFlag);
        Set(handprintPickup, "lockedDialogue", handprintBlockedDialogue);
        GameObject shelfToy = FindSceneObject("Toys on the shelf");
        if (shelfToy != null)
        {
            CluePickup2D shelfToyPickup = shelfToy.GetComponent<CluePickup2D>();
            if (shelfToyPickup != null)
            {
                Set(shelfToyPickup, "lockedByFlag", CompletedFlag);
                Set(shelfToyPickup, "lockedDialogue", (Object)null);
            }
        }
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

    private static ClueData ConfigureRedToyClue()
    {
        ClueData clue = AssetDatabase.LoadAssetAtPath<ClueData>(RedToyCluePath);
        if (clue == null)
        {
            clue = ScriptableObject.CreateInstance<ClueData>();
            AssetDatabase.CreateAsset(clue, RedToyCluePath);
        }

        SerializedObject serialized = new SerializedObject(clue);
        serialized.FindProperty("clueId").stringValue = "store_red_toy";
        serialized.FindProperty("title").stringValue = "红色玩偶";
        serialized.FindProperty("description").stringValue = "一只被单独放在货架旁的红色玩偶。";
        serialized.FindProperty("icon").objectReferenceValue = null;
        serialized.FindProperty("surfaceMeaning").stringValue = "它看起来像是某个身影正在寻找的东西。";
        serialized.FindProperty("trueMeaning").stringValue = "这是黑影丢失的玩偶。";
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(clue);
        return clue;
    }

    private static GameObject ConfigureRedToy(ClueData redToyClue, DialogueData redToyDialogue)
    {
        GameObject shelfToy = FindSceneObject("Toys on the shelf");
        GameObject redToy = GetOrCreate("Store Red Toy", null);
        if (shelfToy != null)
        {
            SpriteRenderer shelfRenderer = shelfToy.GetComponent<SpriteRenderer>();
            if (shelfRenderer != null)
            {
                SpriteRenderer redRenderer = GetOrAdd<SpriteRenderer>(redToy);
                redRenderer.sprite = shelfRenderer.sprite;
                redRenderer.color = new Color(1f, 0.12f, 0.12f, 1f);
                redRenderer.sortingLayerID = shelfRenderer.sortingLayerID;
                redRenderer.sortingOrder = shelfRenderer.sortingOrder + 1;
            }

            redToy.transform.position = shelfToy.transform.position + new Vector3(1.4f, 0.15f, 0f);
            redToy.transform.localScale = shelfToy.transform.lossyScale * 0.45f;
        }
        else
        {
            redToy.transform.position = new Vector3(-4.4f, 1.4f, 0f);
            redToy.transform.localScale = Vector3.one;
        }

        BoxCollider2D collider = GetOrAdd<BoxCollider2D>(redToy);
        collider.isTrigger = true;
        collider.size = new Vector2(1.4f, 1.4f);

        CluePickup2D pickup = GetOrAdd<CluePickup2D>(redToy);
        SerializedObject serialized = new SerializedObject(pickup);
        SerializedProperty condition = serialized.FindProperty("appearCondition");
        condition.FindPropertyRelative("minTimePeriod").intValue = -1;
        condition.FindPropertyRelative("maxTimePeriod").intValue = -1;
        SerializedProperty requiredFlags = condition.FindPropertyRelative("requiredFlags");
        requiredFlags.arraySize = 1;
        requiredFlags.GetArrayElementAtIndex(0).stringValue = CompletedFlag;
        condition.FindPropertyRelative("forbiddenFlags").arraySize = 0;
        condition.FindPropertyRelative("requiredClues").arraySize = 0;
        serialized.FindProperty("inspectDialogue").objectReferenceValue = redToyDialogue;
        serialized.FindProperty("clueToGrant").objectReferenceValue = redToyClue;
        serialized.FindProperty("disappearAfterPickup").boolValue = true;
        serialized.FindProperty("countsAsInvestigation").boolValue = false;
        serialized.FindProperty("lockedByFlag").stringValue = string.Empty;
        serialized.FindProperty("lockedDialogue").objectReferenceValue = null;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pickup);
        redToy.SetActive(true);
        return redToy;
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
