using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 定义 Level3DoorSequenceSetup 类型
public static class Level3DoorSequenceSetup
{
    private const string ScenePath = "Assets/Scenes/level3.unity";
// 更新当前逻辑
    private const string DialogueFolder = "Assets/GameData/Dialogues/level3";
    private const string HandprintBlockedDialoguePath = "Assets/GameData/Dialogues/Dlg_store_handprint_blocked.asset";
// 更新当前逻辑
    private const string DoorLockedDialoguePath = DialogueFolder + "/Dlg_store_door_locked.asset";
    private const string RevealDialoguePath = DialogueFolder + "/Dlg_store_shadow_reveal.asset";
// 更新当前逻辑
    private const string DeliveryDialoguePath = DialogueFolder + "/Dlg_store_shadow_delivery.asset";
    private const string DisappearanceDialoguePath = DialogueFolder + "/Dlg_store_shadow_disappearance.asset";
// 更新当前逻辑
    private const string ExitReadyDialoguePath = DialogueFolder + "/Dlg_store_exit_ready.asset";
    private const string MissingToyDialoguePath = DialogueFolder + "/Dlg_store_missing_toy.asset";
// 更新当前逻辑
    private const string RedToyDialoguePath = DialogueFolder + "/Dlg_store_red_toy.asset";
    private const string RedToyCluePath = "Assets/GameData/Clues/level3/Clue_store_red_toy.asset";
// 更新当前逻辑
    private const string CompletedFlag = "level3_door_sequence_done";
    private const string ResolvedFlag = "level3_store_shadow_resolved";
// 更新当前逻辑
    private const string ToyDeliveredFlag = "level3_store_toy_delivered";
    private const string RedToyPickupFlag = "picked_store_red_toy";

// 保存 InvestigationIds 数据
    private static readonly string[] InvestigationIds =
    {
// 更新当前逻辑
        "store_note",
        "store_poster",
// 更新当前逻辑
        "store_vegetables",
        "store_handprint",
// 更新当前逻辑
        "store_fruit",
        "store_toy"
// 更新当前逻辑
    };

// 调用 MenuItem
    [MenuItem("Trace Me/设置 level3 门前黑影流程")]
    public static void Setup()
    {
// 执行场景切换
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject door = FindSceneObject("Door");
// 定义 FindSceneObject 方法
        GameObject handprint = FindSceneObject("Handprint on the door");
        GameObject sps0Root = FindSceneObject("插画5 4_0 (1)");
// 定义 FindSceneObject 方法
        GameObject spsRoot = FindSceneObject("插画5 5_0");
        GameObject hand = FindSceneObject("hand");
// 定义 FindSceneObject 方法
        GameObject eye = FindSceneObject("eye");

// 空引用时直接退出
        if (door == null || handprint == null || sps0Root == null || spsRoot == null || hand == null || eye == null)
        {
// 输出调试信息
            Debug.LogError("[Level3DoorSequenceSetup] 缺少 Door、Handprint、sps0/sps 根对象或 hand/eye，未修改场景。");
            return;
        }

// 保存 sps0Animator 数据
        Animator sps0Animator = sps0Root.GetComponent<Animator>();
        Animator spsAnimator = spsRoot.GetComponent<Animator>();
// 保存 sceneDoor 数据
        SceneDoor sceneDoor = door.GetComponent<SceneDoor>();
        CluePickup2D handprintPickup = handprint.GetComponent<CluePickup2D>();
// 空引用时直接退出
        if (sps0Animator == null || spsAnimator == null || sceneDoor == null || handprintPickup == null)
        {
// 输出调试信息
            Debug.LogError("[Level3DoorSequenceSetup] Animator、SceneDoor 或 Handprint CluePickup2D 缺失，未修改场景。");
            return;
        }

// 定义 ConfigureDialogue 方法
        DialogueData handprintBlockedDialogue = ConfigureDialogue(
            HandprintBlockedDialoguePath,
// 调用 DialogueLine
            new DialogueLine("我", "这里被挡住了。"));
        DialogueData doorLockedDialogue = ConfigureDialogue(
// 更新当前逻辑
            DoorLockedDialoguePath,
            new DialogueLine("我", "真奇怪，明明进来的时候没上锁的"),
// 调用 DialogueLine
            new DialogueLine(string.Empty, "（似乎有股强大的推力把门抵住了）"));
        DialogueData revealDialogue = ConfigureDialogue(
// 更新当前逻辑
            RevealDialoguePath,
            new DialogueLine(string.Empty, "…… ……"),
// 调用 DialogueLine
            new DialogueLine(string.Empty, "嗡——"),
            new DialogueLine("我", "怎么回事！！"),
// 调用 DialogueLine
            new DialogueLine(string.Empty, "眼前浮现出一个无法辨认形状的身影。"),
            new DialogueLine("？？？", "嘻嘻……"),
// 调用 DialogueLine
            new DialogueLine("？？？", "我在找一个丢失的东西。"));
        DialogueData deliveryDialogue = ConfigureDialogue(
// 更新当前逻辑
            DeliveryDialoguePath,
            new DialogueLine("我", "给你。"),
// 调用 DialogueLine
            new DialogueLine("？？？", "哼哼……"));
        DialogueData disappearanceDialogue = ConfigureDialogue(
// 更新当前逻辑
            DisappearanceDialoguePath,
            new DialogueLine(string.Empty, "身影渐渐消失了。"),
// 调用 DialogueLine
            new DialogueLine(string.Empty, "远处传来东西碎落的声音。"));
        DialogueData exitReadyDialogue = ConfigureDialogue(
// 更新当前逻辑
            ExitReadyDialoguePath,
            new DialogueLine("我", "似乎可以出门了。"));
// 定义 ConfigureDialogue 方法
        DialogueData missingToyDialogue = ConfigureDialogue(
            MissingToyDialoguePath,
// 调用 DialogueLine
            new DialogueLine("？？？", "想出去嘛，只要找到我丢失的东西就可以啦"));
        DialogueData redToyDialogue = ConfigureDialogue(
// 更新当前逻辑
            RedToyDialoguePath,
            new DialogueLine(string.Empty, "货架旁边放着一只红色的玩偶。"),
// 调用 DialogueLine
            new DialogueLine("我", "这是它丢失的东西吗？"));
        ClueData redToyClue = ConfigureRedToyClue();

// 空引用时直接退出
        if (handprintBlockedDialogue == null || doorLockedDialogue == null || revealDialogue == null
            || deliveryDialogue == null || disappearanceDialogue == null || exitReadyDialogue == null
// 更新当前逻辑
            || missingToyDialogue == null || redToyDialogue == null || redToyClue == null)
        {
// 输出调试信息
            Debug.LogError("[Level3DoorSequenceSetup] 对白或线索资源创建失败，未保存场景。");
            return;
        }

// 定义 ConfigureRedToy 方法
        GameObject redToy = ConfigureRedToy(redToyClue, redToyDialogue);
        GameObject handBinding = FindSceneObject("插画5 10_0");
// 定义 FindSceneObject 方法
        GameObject eyeBinding = FindSceneObject("插画5 11");
        if (handBinding != null) handBinding.SetActive(true);
// 判断当前条件
        if (eyeBinding != null) eyeBinding.SetActive(true);

// 定义 GetOrCreate 方法
        GameObject trigger = GetOrCreate("Level3 Door Sequence Trigger", null);
        trigger.transform.position = door.transform.position + Vector3.left * 1.5f;
// 保存 collider 数据
        BoxCollider2D collider = GetOrAdd<BoxCollider2D>(trigger);
        collider.isTrigger = true;
// 更新当前状态
        collider.size = new Vector2(1.5f, 3f);

// 保存 sequence 数据
        Level3DoorSequence sequence = GetOrAdd<Level3DoorSequence>(trigger);
        SerializedObject serialized = new SerializedObject(sequence);
// 保存 steps 数据
        SerializedProperty steps = serialized.FindProperty("steps");
        steps.arraySize = 2;
// 执行 ConfigureStep
        ConfigureStep(steps.GetArrayElementAtIndex(0), "sps0", sps0Root, sps0Animator, "sps0", 0.2f, false, true);
        ConfigureStep(steps.GetArrayElementAtIndex(1), "sps", spsRoot, spsAnimator, "sps", 1.25f, false, false);

// 保存 investigations 数据
        SerializedProperty investigations = serialized.FindProperty("requiredInvestigationIds");
        investigations.arraySize = InvestigationIds.Length;
// 循环处理当前集合
        for (int i = 0; i < InvestigationIds.Length; i++)
        {
// 调用 GetArrayElementAtIndex
            investigations.GetArrayElementAtIndex(i).stringValue = InvestigationIds[i];
        }

// 调用 FindProperty
        serialized.FindProperty("hand").objectReferenceValue = hand;
        serialized.FindProperty("eye").objectReferenceValue = eye;
// 调用 FindProperty
        serialized.FindProperty("redToyGameObject").objectReferenceValue = redToy;
        SerializedProperty sequenceOnlyObjects = serialized.FindProperty("sequenceOnlyObjects");
// 更新当前状态
        sequenceOnlyObjects.arraySize = 0;
        serialized.FindProperty("revealDialogue").objectReferenceValue = revealDialogue;
// 调用 FindProperty
        serialized.FindProperty("missingToyDialogue").objectReferenceValue = missingToyDialogue;
        serialized.FindProperty("deliveryDialogue").objectReferenceValue = deliveryDialogue;
// 调用 FindProperty
        serialized.FindProperty("disappearanceDialogue").objectReferenceValue = disappearanceDialogue;
        serialized.FindProperty("exitReadyDialogue").objectReferenceValue = exitReadyDialogue;
// 调用 FindProperty
        serialized.FindProperty("fallingBreakingSound").objectReferenceValue = null;
        serialized.FindProperty("completedFlag").stringValue = CompletedFlag;
// 调用 FindProperty
        serialized.FindProperty("resolvedFlag").stringValue = ResolvedFlag;
        serialized.FindProperty("toyDeliveredFlag").stringValue = ToyDeliveredFlag;
// 调用 FindProperty
        serialized.FindProperty("redToyPickupFlag").stringValue = RedToyPickupFlag;
        serialized.ApplyModifiedPropertiesWithoutUndo();
// 调用 SetDirty
        EditorUtility.SetDirty(sequence);

// 执行 ConfigureDoor
        ConfigureDoor(sceneDoor, doorLockedDialogue);
        Set(handprintPickup, "lockedByFlag", CompletedFlag);
// 执行 Set
        Set(handprintPickup, "lockedDialogue", handprintBlockedDialogue);
        GameObject shelfToy = FindSceneObject("Toys on the shelf");
// 判断当前条件
        if (shelfToy != null)
        {
// 保存 shelfToyPickup 数据
            CluePickup2D shelfToyPickup = shelfToy.GetComponent<CluePickup2D>();
            if (shelfToyPickup != null)
            {
// 执行 Set
                Set(shelfToyPickup, "lockedByFlag", CompletedFlag);
                Set(shelfToyPickup, "lockedDialogue", (Object)null);
            }
        }
// 执行 DisableDuplicateLevel3ClearPresentation
        DisableDuplicateLevel3ClearPresentation();

// 切换显示状态
        hand.SetActive(false);
        eye.SetActive(false);
// 切换显示状态
        sps0Root.SetActive(false);
        spsRoot.SetActive(false);

// 执行场景切换
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
// 调用 SaveAssets
        AssetDatabase.SaveAssets();
        Debug.Log("[Level3DoorSequenceSetup] Level3 六调查、黑影交付和出口锁定接线完成。碎落音效未发现匹配资源，引用保持为空。");
    }

// 定义 ConfigureDoor 方法
    private static void ConfigureDoor(SceneDoor door, DialogueData lockedDialogue)
    {
// 保存 serialized 数据
        SerializedObject serialized = new SerializedObject(door);
        SerializedProperty condition = serialized.FindProperty("openCondition");
// 调用 FindPropertyRelative
        condition.FindPropertyRelative("minTimePeriod").intValue = -1;
        condition.FindPropertyRelative("maxTimePeriod").intValue = -1;
// 保存 requiredFlags 数据
        SerializedProperty requiredFlags = condition.FindPropertyRelative("requiredFlags");
        requiredFlags.arraySize = 1;
// 调用 GetArrayElementAtIndex
        requiredFlags.GetArrayElementAtIndex(0).stringValue = ResolvedFlag;
        condition.FindPropertyRelative("forbiddenFlags").arraySize = 0;
// 调用 FindPropertyRelative
        condition.FindPropertyRelative("requiredClues").arraySize = 0;
        serialized.FindProperty("lockedDialogue").objectReferenceValue = lockedDialogue;
// 调用 ApplyModifiedPropertiesWithoutUndo
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(door);
    }

// 定义 DisableDuplicateLevel3ClearPresentation 方法
    private static void DisableDuplicateLevel3ClearPresentation()
    {
// 保存 tracker 数据
        SceneClueTracker tracker = Object.FindFirstObjectByType<SceneClueTracker>();
        if (tracker == null)
        {
// 输出调试信息
            Debug.LogWarning("[Level3DoorSequenceSetup] 未找到 SceneClueTracker，未处理重复清场演出。");
            return;
        }

// 保存 serialized 数据
        SerializedObject serialized = new SerializedObject(tracker);
        serialized.FindProperty("brotherShadow").objectReferenceValue = null;
// 调用 FindProperty
        serialized.FindProperty("blackout").objectReferenceValue = null;
        serialized.FindProperty("clearMonologue").objectReferenceValue = null;
// 调用 ApplyModifiedPropertiesWithoutUndo
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(tracker);
    }

// 定义 ConfigureRedToyClue 方法
    private static ClueData ConfigureRedToyClue()
    {
// 保存 clue 引用
        ClueData clue = AssetDatabase.LoadAssetAtPath<ClueData>(RedToyCluePath);
        if (clue == null)
        {
// 更新当前状态
            clue = ScriptableObject.CreateInstance<ClueData>();
            AssetDatabase.CreateAsset(clue, RedToyCluePath);
        }

// 保存 serialized 数据
        SerializedObject serialized = new SerializedObject(clue);
        serialized.FindProperty("clueId").stringValue = "store_red_toy";
// 调用 FindProperty
        serialized.FindProperty("title").stringValue = "红色玩偶";
        serialized.FindProperty("description").stringValue = "一只被单独放在货架旁的红色玩偶。";
// 调用 FindProperty
        serialized.FindProperty("icon").objectReferenceValue = null;
        serialized.FindProperty("surfaceMeaning").stringValue = "它看起来像是某个身影正在寻找的东西。";
// 调用 FindProperty
        serialized.FindProperty("trueMeaning").stringValue = "这是黑影丢失的玩偶。";
        serialized.ApplyModifiedPropertiesWithoutUndo();
// 调用 SetDirty
        EditorUtility.SetDirty(clue);
        return clue;
    }

// 定义 ConfigureRedToy 方法
    private static GameObject ConfigureRedToy(ClueData redToyClue, DialogueData redToyDialogue)
    {
// 定义 FindSceneObject 方法
        GameObject shelfToy = FindSceneObject("Toys on the shelf");
        GameObject redToy = GetOrCreate("Store Red Toy", null);
// 判断当前条件
        if (shelfToy != null)
        {
// 保存 shelfRenderer 引用
            SpriteRenderer shelfRenderer = shelfToy.GetComponent<SpriteRenderer>();
            if (shelfRenderer != null)
            {
// 保存 redRenderer 引用
                SpriteRenderer redRenderer = GetOrAdd<SpriteRenderer>(redToy);
                redRenderer.sprite = shelfRenderer.sprite;
// 更新当前状态
                redRenderer.color = new Color(1f, 0.12f, 0.12f, 1f);
                redRenderer.sortingLayerID = shelfRenderer.sortingLayerID;
// 更新当前状态
                redRenderer.sortingOrder = shelfRenderer.sortingOrder + 1;
            }

// 更新当前位置
            redToy.transform.position = shelfToy.transform.position + new Vector3(1.4f, 0.15f, 0f);
            redToy.transform.localScale = shelfToy.transform.lossyScale * 0.45f;
        }
// 处理其他分支
        else
        {
// 更新当前位置
            redToy.transform.position = new Vector3(-4.4f, 1.4f, 0f);
            redToy.transform.localScale = Vector3.one;
        }

// 保存 collider 数据
        BoxCollider2D collider = GetOrAdd<BoxCollider2D>(redToy);
        collider.isTrigger = true;
// 更新当前状态
        collider.size = new Vector2(1.4f, 1.4f);

// 保存 pickup 数据
        CluePickup2D pickup = GetOrAdd<CluePickup2D>(redToy);
        SerializedObject serialized = new SerializedObject(pickup);
// 保存 condition 数据
        SerializedProperty condition = serialized.FindProperty("appearCondition");
        condition.FindPropertyRelative("minTimePeriod").intValue = -1;
// 调用 FindPropertyRelative
        condition.FindPropertyRelative("maxTimePeriod").intValue = -1;
        SerializedProperty requiredFlags = condition.FindPropertyRelative("requiredFlags");
// 更新当前状态
        requiredFlags.arraySize = 1;
        requiredFlags.GetArrayElementAtIndex(0).stringValue = CompletedFlag;
// 调用 FindPropertyRelative
        condition.FindPropertyRelative("forbiddenFlags").arraySize = 0;
        condition.FindPropertyRelative("requiredClues").arraySize = 0;
// 调用 FindProperty
        serialized.FindProperty("inspectDialogue").objectReferenceValue = redToyDialogue;
        serialized.FindProperty("clueToGrant").objectReferenceValue = redToyClue;
// 调用 FindProperty
        serialized.FindProperty("disappearAfterPickup").boolValue = true;
        serialized.FindProperty("countsAsInvestigation").boolValue = false;
// 调用 FindProperty
        serialized.FindProperty("lockedByFlag").stringValue = string.Empty;
        serialized.FindProperty("lockedDialogue").objectReferenceValue = null;
// 调用 ApplyModifiedPropertiesWithoutUndo
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pickup);
// 切换显示状态
        redToy.SetActive(true);
        return redToy;
    }

// 定义 ConfigureDialogue 方法
    private static DialogueData ConfigureDialogue(string path, params DialogueLine[] dialogueLines)
    {
// 保存 dialogue 引用
        DialogueData dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
        if (dialogue == null)
        {
// 更新当前状态
            dialogue = ScriptableObject.CreateInstance<DialogueData>();
            AssetDatabase.CreateAsset(dialogue, path);
        }

// 保存 serialized 数据
        SerializedObject serialized = new SerializedObject(dialogue);
        SerializedProperty lines = serialized.FindProperty("lines");
// 更新当前状态
        lines.arraySize = dialogueLines.Length;
        for (int i = 0; i < dialogueLines.Length; i++)
        {
// 保存 line 数据
            SerializedProperty line = lines.GetArrayElementAtIndex(i);
            line.FindPropertyRelative("character").objectReferenceValue = null;
// 调用 FindPropertyRelative
            line.FindPropertyRelative("expression").stringValue = string.Empty;
            line.FindPropertyRelative("speakerName").stringValue = dialogueLines[i].speaker;
// 调用 FindPropertyRelative
            line.FindPropertyRelative("text").stringValue = dialogueLines[i].text;
        }

// 调用 FindProperty
        serialized.FindProperty("countsAsInvestigation").boolValue = false;
        serialized.FindProperty("setFlagsOnComplete").arraySize = 0;
// 调用 FindProperty
        serialized.FindProperty("advanceTimeOnComplete").intValue = 0;
        serialized.FindProperty("grantCluesOnComplete").arraySize = 0;
// 调用 ApplyModifiedPropertiesWithoutUndo
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dialogue);
// 返回当前结果
        return dialogue;
    }

// 定义 ConfigureStep 方法
    private static void ConfigureStep(SerializedProperty step, string label, GameObject root, Animator animator,
        string stateName, float duration, bool showResults, bool hideRoot)
    {
// 调用 FindPropertyRelative
        step.FindPropertyRelative("label").stringValue = label;
        step.FindPropertyRelative("root").objectReferenceValue = root;
// 调用 FindPropertyRelative
        step.FindPropertyRelative("animator").objectReferenceValue = animator;
        step.FindPropertyRelative("stateName").stringValue = stateName;
// 调用 FindPropertyRelative
        step.FindPropertyRelative("showResultObjectsDuringStep").boolValue = showResults;
        step.FindPropertyRelative("hideRootAfterStep").boolValue = hideRoot;
// 调用 FindPropertyRelative
        step.FindPropertyRelative("duration").floatValue = duration;
    }

// 定义 FindSceneObject 方法
    private static GameObject FindSceneObject(string name)
    {
// 遍历全部元素
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
// 定义 FindInHierarchy 方法
            Transform match = FindInHierarchy(root.transform, name);
            if (match != null) return match.gameObject;
        }
// 返回当前结果
        return null;
    }

// 定义 FindInHierarchy 方法
    private static Transform FindInHierarchy(Transform current, string name)
    {
// 判断当前条件
        if (current.name == name) return current;
        foreach (Transform child in current)
        {
// 定义 FindInHierarchy 方法
            Transform match = FindInHierarchy(child, name);
            if (match != null) return match;
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
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        }
// 判断当前条件
        if (parent != null && go.transform.parent != parent) go.transform.SetParent(parent, true);
        return go;
    }

// 更新当前逻辑
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
// 保存 component 数据
        T component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

// 定义 Set 方法
    private static void Set(Object target, string property, string value)
    {
// 保存 serialized 数据
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).stringValue = value;
// 调用 ApplyModifiedPropertiesWithoutUndo
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

// 定义 Set 方法
    private static void Set(Object target, string property, Object value)
    {
// 保存 serialized 数据
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = value;
// 调用 ApplyModifiedPropertiesWithoutUndo
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

// 更新当前逻辑
    private readonly struct DialogueLine
    {
// 保存 speaker 数据
        public readonly string speaker;
        public readonly string text;

// 定义 DialogueLine 方法
        public DialogueLine(string speaker, string text)
        {
// 更新当前状态
            this.speaker = speaker;
            this.text = text;
        }
    }
}
