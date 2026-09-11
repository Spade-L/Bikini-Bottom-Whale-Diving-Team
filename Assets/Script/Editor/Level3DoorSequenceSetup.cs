using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 一键生成 level3 红色身影演出配置
public static class Level3DoorSequenceSetup
{
    private const string ScenePath = "Assets/Scenes/level3.unity";
// 推进 Level3DoorSequenceSetup 的当前步骤
    private const string DialogueFolder = "Assets/GameData/Dialogues/level3";
    private const string HandprintBlockedDialoguePath = "Assets/GameData/Dialogues/Dlg_store_handprint_blocked.asset";
// 在 Level3DoorSequenceSetup 中处理 推进 Level3DoorSequenceSetup 的当前步骤
    private const string DoorLockedDialoguePath = DialogueFolder + "/Dlg_store_door_locked.asset";
    private const string RevealDialoguePath = DialogueFolder + "/Dlg_store_shadow_reveal.asset";
// 推进 Level3DoorSequenceSetup 的当前步骤（Level3DoorSequenceSetup）
    private const string DeliveryDialoguePath = DialogueFolder + "/Dlg_store_shadow_delivery.asset";
    private const string DisappearanceDialoguePath = DialogueFolder + "/Dlg_store_shadow_disappearance.asset";
// 推进 Level3DoorSequenceSetup 的当前步骤（Level3DoorSequenceSetup）（private）
    private const string ExitReadyDialoguePath = DialogueFolder + "/Dlg_store_exit_ready.asset";
    private const string MissingToyDialoguePath = DialogueFolder + "/Dlg_store_missing_toy.asset";
// 在 Level3DoorSequenceSetup 中继续当前处理
    private const string RedToyDialoguePath = DialogueFolder + "/Dlg_store_red_toy.asset";
    private const string RedToyCluePath = "Assets/GameData/Clues/level3/Clue_store_red_toy.asset";
// 在 Level3DoorSequenceSetup 中继续当前处理（Level3DoorSequenceSetup 后续步骤）
    private const string CompletedFlag = "level3_door_sequence_done";
    private const string ResolvedFlag = "level3_store_shadow_resolved";
// 在 Level3DoorSequenceSetup 中继续当前处理（Level3DoorSequenceSetup 后续步骤）（27）
    private const string ToyDeliveredFlag = "level3_store_toy_delivered";
    private const string RedToyPickupFlag = "picked_store_red_toy";

// 同步 Level3DoorSequenceSetup 的相关数据
    private static readonly string[] InvestigationIds =
    {
// 推进 Level3DoorSequenceSetup 的当前步骤（Level3DoorSequenceSetup）（store_note）
        "store_note",
        "store_poster",
// 推进 Level3DoorSequenceSetup 的当前步骤（Level3DoorSequenceSetup）（store_vegetables）
        "store_vegetables",
        "store_handprint",
// 推进 Level3DoorSequenceSetup 的当前步骤（Level3DoorSequenceSetup）（store_fruit）
        "store_fruit",
        "store_toy"
// 推进 Level3DoorSequenceSetup 的当前步骤（Level3DoorSequenceSetup）（MenuItem）
    };

// 设置 Setup 的目标状态
    [MenuItem("Trace Me/设置 level3 门前黑影流程")]
    public static void Setup()
    {
// 执行场景切换
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject door = FindSceneObject("Door");
// 缓存 Setup 所需引用
        GameObject handprint = FindSceneObject("Handprint on the door");
        GameObject sps0Root = FindSceneObject("插画5 4_0 (1)");
// 获取 FindSceneObject 所需引用（Setup）
        GameObject spsRoot = FindSceneObject("插画5 5_0");
        GameObject hand = FindSceneObject("hand");
// 获取 FindSceneObject 所需引用（Setup）（GameObject）
        GameObject eye = FindSceneObject("eye");

// Setup 缺少引用时提前结束
        if (door == null || handprint == null || sps0Root == null || spsRoot == null || hand == null || eye == null)
        {
// 输出调试信息
            Debug.LogError("[Level3DoorSequenceSetup] 缺少 Door、Handprint、sps0/sps 根对象或 hand/eye，未修改场景。");
            return;
        }

// 同步 Setup 的相关数据
        Animator sps0Animator = sps0Root.GetComponent<Animator>();
        Animator spsAnimator = spsRoot.GetComponent<Animator>();
// 同步 Setup 的相关数据（Setup 后续步骤）
        SceneDoor sceneDoor = door.GetComponent<SceneDoor>();
        CluePickup2D handprintPickup = handprint.GetComponent<CluePickup2D>();
// 缺少必要引用时退出 Setup
        if (sps0Animator == null || spsAnimator == null || sceneDoor == null || handprintPickup == null)
        {
// 在 Setup 中继续当前处理
            Debug.LogError("[Level3DoorSequenceSetup] Animator、SceneDoor 或 Handprint CluePickup2D 缺失，未修改场景。");
            return;
        }

// 配置 ConfigureDialogue 对应字段
        DialogueData handprintBlockedDialogue = ConfigureDialogue(
            HandprintBlockedDialoguePath,
// 使用 Setup 所需功能
            new DialogueLine("我", "这里被挡住了。"));
        DialogueData doorLockedDialogue = ConfigureDialogue(
// 推进 Setup 的当前步骤
            DoorLockedDialoguePath,
            new DialogueLine("我", "真奇怪，明明进来的时候没上锁的"),
// 使用 Setup 所需功能（Setup）
            new DialogueLine(string.Empty, "（似乎有股强大的推力把门抵住了）"));
        DialogueData revealDialogue = ConfigureDialogue(
// 推进 Setup 的当前步骤（Setup）
            RevealDialoguePath,
            new DialogueLine(string.Empty, "…… ……"),
// 在 Setup 中处理 DialogueLine
            new DialogueLine(string.Empty, "嗡——"),
            new DialogueLine("我", "怎么回事！！"),
// 在 Setup 中处理 DialogueLine（Setup 后续步骤）
            new DialogueLine(string.Empty, "眼前浮现出一个无法辨认形状的身影。"),
            new DialogueLine("？？？", "嘻嘻……"),
// 在 Setup 中处理 DialogueLine（Setup 后续步骤）（后续处理 2）
            new DialogueLine("？？？", "我在找一个丢失的东西。"));
        DialogueData deliveryDialogue = ConfigureDialogue(
// 推进 Setup 的当前步骤（Setup）（DeliveryDialoguePath）
            DeliveryDialoguePath,
            new DialogueLine("我", "给你。"),
// 在 Setup 中处理 DialogueLine（Setup 后续步骤）（后续处理 3）
            new DialogueLine("？？？", "哼哼……"));
        DialogueData disappearanceDialogue = ConfigureDialogue(
// 推进 Setup 的当前步骤（Setup）（DisappearanceDialoguePath）
            DisappearanceDialoguePath,
            new DialogueLine(string.Empty, "身影渐渐消失了。"),
// 在 Setup 中处理 DialogueLine（Setup 后续步骤）（后续处理 4）
            new DialogueLine(string.Empty, "远处传来东西碎落的声音。"));
        DialogueData exitReadyDialogue = ConfigureDialogue(
// 推进 Setup 的当前步骤（Setup）（ExitReadyDialoguePath）
            ExitReadyDialoguePath,
            new DialogueLine("我", "似乎可以出门了。"));
// 配置 ConfigureDialogue 对应字段（Setup）
        DialogueData missingToyDialogue = ConfigureDialogue(
            MissingToyDialoguePath,
// 在 Setup 中处理 DialogueLine（Setup 后续步骤）（后续处理 5）
            new DialogueLine("？？？", "想出去嘛，只要找到我丢失的东西就可以啦"));
        DialogueData redToyDialogue = ConfigureDialogue(
// 推进 Setup 的当前步骤（Setup）（RedToyDialoguePath）
            RedToyDialoguePath,
            new DialogueLine(string.Empty, "货架旁边放着一只红色的玩偶。"),
// 在 Setup 中处理 DialogueLine（Setup 后续步骤）（后续处理 6）
            new DialogueLine("我", "这是它丢失的东西吗？"));
        ClueData redToyClue = ConfigureRedToyClue();

// 缺少必要引用时退出 Setup（Setup）
        if (handprintBlockedDialogue == null || doorLockedDialogue == null || revealDialogue == null
            || deliveryDialogue == null || disappearanceDialogue == null || exitReadyDialogue == null
// 推进 Setup 的当前步骤（Setup）（missingToyDialogue）
            || missingToyDialogue == null || redToyDialogue == null || redToyClue == null)
        {
// 在 Setup 中继续当前处理（Setup 后续步骤）
            Debug.LogError("[Level3DoorSequenceSetup] 对白或线索资源创建失败，未保存场景。");
            return;
        }

// 配置 ConfigureRedToy 对应字段
        GameObject redToy = ConfigureRedToy(redToyClue, redToyDialogue);
        GameObject handBinding = FindSceneObject("插画5 10_0");
// 在 Setup 中继续当前处理（Setup 后续步骤）（150）
        GameObject eyeBinding = FindSceneObject("插画5 11");
        if (handBinding != null) handBinding.SetActive(true);
// 检查 Setup 的前置条件
        if (eyeBinding != null) eyeBinding.SetActive(true);

// 缓存 Setup 所需引用（Setup）
        GameObject trigger = GetOrCreate("Level3 Door Sequence Trigger", null);
        trigger.transform.position = door.transform.position + Vector3.left * 1.5f;
// 同步 Setup 的相关数据（Setup 后续步骤）（159）
        BoxCollider2D collider = GetOrAdd<BoxCollider2D>(trigger);
        collider.isTrigger = true;
// 同步 Setup 的状态
        collider.size = new Vector2(1.5f, 3f);

// 同步 Setup 的相关数据（Setup 后续步骤）（165）
        Level3DoorSequence sequence = GetOrAdd<Level3DoorSequence>(trigger);
        SerializedObject serialized = new SerializedObject(sequence);
// 同步 Setup 的相关数据（Setup 后续步骤）（168）
        SerializedProperty steps = serialized.FindProperty("steps");
        steps.arraySize = 2;
// 推进 Setup 中的必要步骤
        ConfigureStep(steps.GetArrayElementAtIndex(0), "sps0", sps0Root, sps0Animator, "sps0", 0.2f, false, true);
        ConfigureStep(steps.GetArrayElementAtIndex(1), "sps", spsRoot, spsAnimator, "sps", 1.25f, false, false);

// 同步 Setup 的相关数据（Setup 后续步骤）（175）
        SerializedProperty investigations = serialized.FindProperty("requiredInvestigationIds");
        investigations.arraySize = InvestigationIds.Length;
// 循环处理当前集合
        for (int i = 0; i < InvestigationIds.Length; i++)
        {
// 在 Setup 中处理 GetArrayElementAtIndex
            investigations.GetArrayElementAtIndex(i).stringValue = InvestigationIds[i];
        }

// 在 Setup 中处理 FindProperty
        serialized.FindProperty("hand").objectReferenceValue = hand;
        serialized.FindProperty("eye").objectReferenceValue = eye;
// 在 Setup 中处理 FindProperty（Setup 后续步骤）
        serialized.FindProperty("redToyGameObject").objectReferenceValue = redToy;
        SerializedProperty sequenceOnlyObjects = serialized.FindProperty("sequenceOnlyObjects");
// 同步 Setup 的内部状态
        sequenceOnlyObjects.arraySize = 0;
        serialized.FindProperty("revealDialogue").objectReferenceValue = revealDialogue;
// 在 Setup 中处理 FindProperty（Setup 后续步骤）（后续处理 2）
        serialized.FindProperty("missingToyDialogue").objectReferenceValue = missingToyDialogue;
        serialized.FindProperty("deliveryDialogue").objectReferenceValue = deliveryDialogue;
// 在 Setup 中处理 FindProperty（Setup 后续步骤）（后续处理 3）
        serialized.FindProperty("disappearanceDialogue").objectReferenceValue = disappearanceDialogue;
        serialized.FindProperty("exitReadyDialogue").objectReferenceValue = exitReadyDialogue;
// 在 Setup 中处理 FindProperty（Setup 后续步骤）（后续处理 4）
        serialized.FindProperty("fallingBreakingSound").objectReferenceValue = null;
        serialized.FindProperty("completedFlag").stringValue = CompletedFlag;
// 在 Setup 中处理 FindProperty（Setup 后续步骤）（后续处理 5）
        serialized.FindProperty("resolvedFlag").stringValue = ResolvedFlag;
        serialized.FindProperty("toyDeliveredFlag").stringValue = ToyDeliveredFlag;
// 在 Setup 中处理 FindProperty（Setup 后续步骤）（后续处理 6）
        serialized.FindProperty("redToyPickupFlag").stringValue = RedToyPickupFlag;
        serialized.ApplyModifiedPropertiesWithoutUndo();
// 在 Setup 中处理 SetDirty
        EditorUtility.SetDirty(sequence);

// 推进 Setup 中的必要步骤（Setup）
        ConfigureDoor(sceneDoor, doorLockedDialogue);
        Set(handprintPickup, "lockedByFlag", CompletedFlag);
// 在 Setup 中处理 Set
        Set(handprintPickup, "lockedDialogue", handprintBlockedDialogue);
        GameObject shelfToy = FindSceneObject("Toys on the shelf");
// 在 Setup 中处理 检查 Setup 的前置条件
        if (shelfToy != null)
        {
// 同步 Setup 的相关数据（Setup 后续步骤）（221）
            CluePickup2D shelfToyPickup = shelfToy.GetComponent<CluePickup2D>();
            if (shelfToyPickup != null)
            {
// 在 Setup 中处理 Set（Setup 后续步骤）
                Set(shelfToyPickup, "lockedByFlag", CompletedFlag);
                Set(shelfToyPickup, "lockedDialogue", (Object)null);
            }
        }
// 在 Setup 中处理 DisableDuplicateLevel3ClearPresentation
        DisableDuplicateLevel3ClearPresentation();

// 切换 Setup 的显示状态
        hand.SetActive(false);
        eye.SetActive(false);
// 切换 Setup 的显示状态（Setup 后续步骤）
        sps0Root.SetActive(false);
        spsRoot.SetActive(false);

// 在 Setup 中继续当前处理（Setup 后续步骤）（240）
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
// 在 Setup 中处理 SaveAssets
        AssetDatabase.SaveAssets();
        Debug.Log("[Level3DoorSequenceSetup] Level3 六调查、黑影交付和出口锁定接线完成。碎落音效未发现匹配资源，引用保持为空。");
    }

// 配置 ConfigureDoor 对应字段
    private static void ConfigureDoor(SceneDoor door, DialogueData lockedDialogue)
    {
// 同步 ConfigureDoor 的相关数据
        SerializedObject serialized = new SerializedObject(door);
        SerializedProperty condition = serialized.FindProperty("openCondition");
// 使用 ConfigureDoor 所需功能
        condition.FindPropertyRelative("minTimePeriod").intValue = -1;
        condition.FindPropertyRelative("maxTimePeriod").intValue = -1;
// 同步 ConfigureDoor 的相关数据（ConfigureDoor 后续步骤）
        SerializedProperty requiredFlags = condition.FindPropertyRelative("requiredFlags");
        requiredFlags.arraySize = 1;
// 使用 ConfigureDoor 所需功能（ConfigureDoor）
        requiredFlags.GetArrayElementAtIndex(0).stringValue = ResolvedFlag;
        condition.FindPropertyRelative("forbiddenFlags").arraySize = 0;
// 在 ConfigureDoor 中处理 FindPropertyRelative
        condition.FindPropertyRelative("requiredClues").arraySize = 0;
        serialized.FindProperty("lockedDialogue").objectReferenceValue = lockedDialogue;
// 在 ConfigureDoor 中处理 ApplyModifiedPropertiesWithoutUndo
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(door);
    }

// 处理 DisableDuplicateLevel3ClearPresentation 对应逻辑
    private static void DisableDuplicateLevel3ClearPresentation()
    {
// 同步 DisableDuplicateLevel3ClearPresentation 的相关数据
        SceneClueTracker tracker = Object.FindFirstObjectByType<SceneClueTracker>();
        if (tracker == null)
        {
// 在 DisableDuplicateLevel3ClearPresentation 中继续当前处理
            Debug.LogWarning("[Level3DoorSequenceSetup] 未找到 SceneClueTracker，未处理重复清场演出。");
            return;
        }

// 同步 DisableDuplicateLevel3ClearPresentation 的相关数据（DisableDuplicateLevel3ClearPresentation 后续步骤）
        SerializedObject serialized = new SerializedObject(tracker);
        serialized.FindProperty("brotherShadow").objectReferenceValue = null;
// 使用 DisableDuplicateLevel3ClearPresentation 所需功能
        serialized.FindProperty("blackout").objectReferenceValue = null;
        serialized.FindProperty("clearMonologue").objectReferenceValue = null;
// 使用 DisableDuplicateLevel3ClearPresentation 所需功能（DisableDuplicateLevel3ClearPresentation）
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(tracker);
    }

// 配置 ConfigureRedToyClue 对应字段
    private static ClueData ConfigureRedToyClue()
    {
// 保存 clue 引用
        ClueData clue = AssetDatabase.LoadAssetAtPath<ClueData>(RedToyCluePath);
        if (clue == null)
        {
// 同步 ConfigureRedToyClue 的内部状态
            clue = ScriptableObject.CreateInstance<ClueData>();
            AssetDatabase.CreateAsset(clue, RedToyCluePath);
        }

// 同步 ConfigureRedToyClue 的相关数据
        SerializedObject serialized = new SerializedObject(clue);
        serialized.FindProperty("clueId").stringValue = "store_red_toy";
// 使用 ConfigureRedToyClue 所需功能
        serialized.FindProperty("title").stringValue = "红色玩偶";
        serialized.FindProperty("description").stringValue = "一只被单独放在货架旁的红色玩偶。";
// 使用 ConfigureRedToyClue 所需功能（ConfigureRedToyClue）
        serialized.FindProperty("icon").objectReferenceValue = null;
        serialized.FindProperty("surfaceMeaning").stringValue = "它看起来像是某个身影正在寻找的东西。";
// 在 ConfigureRedToyClue 中处理 FindProperty
        serialized.FindProperty("trueMeaning").stringValue = "这是黑影丢失的玩偶。";
        serialized.ApplyModifiedPropertiesWithoutUndo();
// 在 ConfigureRedToyClue 中处理 SetDirty
        EditorUtility.SetDirty(clue);
        return clue;
    }

// 在 ConfigureRedToyClue 中处理 ConfigureRedToy
    private static GameObject ConfigureRedToy(ClueData redToyClue, DialogueData redToyDialogue)
    {
// 获取 FindSceneObject 所需引用（ConfigureRedToy）
        GameObject shelfToy = FindSceneObject("Toys on the shelf");
        GameObject redToy = GetOrCreate("Store Red Toy", null);
// 检查 ConfigureRedToy 的前置条件
        if (shelfToy != null)
        {
// 保存 shelfRenderer 引用
            SpriteRenderer shelfRenderer = shelfToy.GetComponent<SpriteRenderer>();
            if (shelfRenderer != null)
            {
// 保存 redRenderer 引用
                SpriteRenderer redRenderer = GetOrAdd<SpriteRenderer>(redToy);
                redRenderer.sprite = shelfRenderer.sprite;
// 同步 ConfigureRedToy 的内部状态
                redRenderer.color = new Color(1f, 0.12f, 0.12f, 1f);
                redRenderer.sortingLayerID = shelfRenderer.sortingLayerID;
// 同步 ConfigureRedToy 的内部状态（ConfigureRedToy）
                redRenderer.sortingOrder = shelfRenderer.sortingOrder + 1;
            }

// 更新当前位置
            redToy.transform.position = shelfToy.transform.position + new Vector3(1.4f, 0.15f, 0f);
            redToy.transform.localScale = shelfToy.transform.lossyScale * 0.45f;
        }
// 处理 ConfigureRedToy 的备用分支
        else
        {
// 在 ConfigureRedToy 中继续当前处理
            redToy.transform.position = new Vector3(-4.4f, 1.4f, 0f);
            redToy.transform.localScale = Vector3.one;
        }

// 同步 ConfigureRedToy 的相关数据
        BoxCollider2D collider = GetOrAdd<BoxCollider2D>(redToy);
        collider.isTrigger = true;
// 同步 ConfigureRedToy 的内部状态（ConfigureRedToy）（collider）
        collider.size = new Vector2(1.4f, 1.4f);

// 同步 ConfigureRedToy 的相关数据（ConfigureRedToy 后续步骤）
        CluePickup2D pickup = GetOrAdd<CluePickup2D>(redToy);
        SerializedObject serialized = new SerializedObject(pickup);
// 同步 ConfigureRedToy 的相关数据（ConfigureRedToy 后续步骤）（367）
        SerializedProperty condition = serialized.FindProperty("appearCondition");
        condition.FindPropertyRelative("minTimePeriod").intValue = -1;
// 使用 ConfigureRedToy 所需功能
        condition.FindPropertyRelative("maxTimePeriod").intValue = -1;
        SerializedProperty requiredFlags = condition.FindPropertyRelative("requiredFlags");
// 同步 ConfigureRedToy 的内部状态（ConfigureRedToy）（requiredFlags）
        requiredFlags.arraySize = 1;
        requiredFlags.GetArrayElementAtIndex(0).stringValue = CompletedFlag;
// 使用 ConfigureRedToy 所需功能（ConfigureRedToy）
        condition.FindPropertyRelative("forbiddenFlags").arraySize = 0;
        condition.FindPropertyRelative("requiredClues").arraySize = 0;
// 在 ConfigureRedToy 中处理 FindProperty
        serialized.FindProperty("inspectDialogue").objectReferenceValue = redToyDialogue;
        serialized.FindProperty("clueToGrant").objectReferenceValue = redToyClue;
// 在 ConfigureRedToy 中处理 FindProperty（ConfigureRedToy 后续步骤）
        serialized.FindProperty("disappearAfterPickup").boolValue = true;
        serialized.FindProperty("countsAsInvestigation").boolValue = false;
// 在 ConfigureRedToy 中处理 FindProperty（ConfigureRedToy 后续步骤）（后续处理 2）
        serialized.FindProperty("lockedByFlag").stringValue = string.Empty;
        serialized.FindProperty("lockedDialogue").objectReferenceValue = null;
// 在 ConfigureRedToy 中处理 ApplyModifiedPropertiesWithoutUndo
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pickup);
// 切换 ConfigureRedToy 的显示状态
        redToy.SetActive(true);
        return redToy;
    }

// 在 ConfigureRedToy 中处理 ConfigureDialogue
    private static DialogueData ConfigureDialogue(string path, params DialogueLine[] dialogueLines)
    {
// 保存 dialogue 引用
        DialogueData dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
        if (dialogue == null)
        {
// 同步 ConfigureDialogue 的内部状态
            dialogue = ScriptableObject.CreateInstance<DialogueData>();
            AssetDatabase.CreateAsset(dialogue, path);
        }

// 同步 ConfigureDialogue 的相关数据
        SerializedObject serialized = new SerializedObject(dialogue);
        SerializedProperty lines = serialized.FindProperty("lines");
// 同步 ConfigureDialogue 的内部状态（ConfigureDialogue）
        lines.arraySize = dialogueLines.Length;
        for (int i = 0; i < dialogueLines.Length; i++)
        {
// 同步 ConfigureDialogue 的相关数据（ConfigureDialogue 后续步骤）
            SerializedProperty line = lines.GetArrayElementAtIndex(i);
            line.FindPropertyRelative("character").objectReferenceValue = null;
// 使用 ConfigureDialogue 所需功能
            line.FindPropertyRelative("expression").stringValue = string.Empty;
            line.FindPropertyRelative("speakerName").stringValue = dialogueLines[i].speaker;
// 使用 ConfigureDialogue 所需功能（ConfigureDialogue）
            line.FindPropertyRelative("text").stringValue = dialogueLines[i].text;
        }

// 在 ConfigureDialogue 中处理 FindProperty
        serialized.FindProperty("countsAsInvestigation").boolValue = false;
        serialized.FindProperty("setFlagsOnComplete").arraySize = 0;
// 在 ConfigureDialogue 中处理 FindProperty（ConfigureDialogue 后续步骤）
        serialized.FindProperty("advanceTimeOnComplete").intValue = 0;
        serialized.FindProperty("grantCluesOnComplete").arraySize = 0;
// 在 ConfigureDialogue 中处理 ApplyModifiedPropertiesWithoutUndo
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dialogue);
// 返回 ConfigureDialogue 的处理结果
        return dialogue;
    }

// 配置 ConfigureStep 对应字段
    private static void ConfigureStep(SerializedProperty step, string label, GameObject root, Animator animator,
        string stateName, float duration, bool showResults, bool hideRoot)
    {
// 使用 ConfigureStep 所需功能
        step.FindPropertyRelative("label").stringValue = label;
        step.FindPropertyRelative("root").objectReferenceValue = root;
// 使用 ConfigureStep 所需功能（ConfigureStep）
        step.FindPropertyRelative("animator").objectReferenceValue = animator;
        step.FindPropertyRelative("stateName").stringValue = stateName;
// 在 ConfigureStep 中处理 FindPropertyRelative
        step.FindPropertyRelative("showResultObjectsDuringStep").boolValue = showResults;
        step.FindPropertyRelative("hideRootAfterStep").boolValue = hideRoot;
// 在 ConfigureStep 中处理 FindPropertyRelative（ConfigureStep 后续步骤）
        step.FindPropertyRelative("duration").floatValue = duration;
    }

// 获取 FindSceneObject 所需引用
    private static GameObject FindSceneObject(string name)
    {
// 遍历全部元素
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
// 缓存 FindSceneObject 所需引用
            Transform match = FindInHierarchy(root.transform, name);
            if (match != null) return match.gameObject;
        }
// 返回 FindSceneObject 的处理结果
        return null;
    }

// 获取 FindInHierarchy 所需引用
    private static Transform FindInHierarchy(Transform current, string name)
    {
// 检查 FindInHierarchy 的前置条件
        if (current.name == name) return current;
        foreach (Transform child in current)
        {
// 获取 FindInHierarchy 所需引用（FindInHierarchy）
            Transform match = FindInHierarchy(child, name);
            if (match != null) return match;
        }
// 返回 FindInHierarchy 的处理结果
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
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        }
// 检查 GetOrCreate 的前置条件
        if (parent != null && go.transform.parent != parent) go.transform.SetParent(parent, true);
        return go;
    }

// 推进 GetOrCreate 的当前步骤
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
// 同步 GetOrCreate 的相关数据
        T component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

// 设置 Set 的目标状态
    private static void Set(Object target, string property, string value)
    {
// 同步 Set 的相关数据
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).stringValue = value;
// 使用 Set 所需功能
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

// 在 Set 中处理 Set
    private static void Set(Object target, string property, Object value)
    {
// 同步 Set 的相关数据（Set 后续步骤）
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = value;
// 使用 Set 所需功能（Set）
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

// 推进 Set 的当前步骤
    private readonly struct DialogueLine
    {
// 同步 Set 的相关数据（Set 后续步骤）（533）
        public readonly string speaker;
        public readonly string text;

// 完成 Set 的主要职责
        public DialogueLine(string speaker, string text)
        {
// 同步 Set 的内部状态
            this.speaker = speaker;
            this.text = text;
        }
    }
}
