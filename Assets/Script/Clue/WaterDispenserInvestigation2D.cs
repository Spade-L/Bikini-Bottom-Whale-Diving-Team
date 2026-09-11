using System.Collections.Generic;
using UnityEngine;

// 使用 当前脚本 所需功能
[RequireComponent(typeof(BoxCollider2D))]
public class WaterDispenserInvestigation2D : MonoBehaviour, IInteractionPromptSource
{
// 配置 调查内容 分组
    [Header("调查内容")]
    [SerializeField] private DialogueData firstDialogue;
// 保存 revealDialogue 引用
    [SerializeField] private DialogueData revealDialogue;
    [SerializeField] private DialogueData repeatDialogue;
// 保存 clueToGrant 引用
    [SerializeField] private ClueData clueToGrant;

// 配置 移动 分组
    [Header("移动")]
    [SerializeField] private Vector3 initialLocalPosition = new Vector3(18f, 8f, 0f);
// 同步 WaterDispenserInvestigation2D 的相关数据
    [SerializeField] private Vector3 movedLocalPosition = new Vector3(12f, 8f, 0f);
    [SerializeField] private string movedFlag = "school_water_dispenser_moved";
// 同步 WaterDispenserInvestigation2D 的相关数据（WaterDispenserInvestigation2D 后续步骤）
    [SerializeField] private string roomReadyFlag = "school_water_dispenser_room_ready";

// 配置 交互 UI 分组
    [Header("交互 UI")]
    [SerializeField] private GameObject interactionUI;
// 同步 WaterDispenserInvestigation2D 的相关数据（WaterDispenserInvestigation2D 后续步骤）（28）
    [SerializeField] private string playerTag = "Player";

// 同步 WaterDispenserInvestigation2D 的相关数据（WaterDispenserInvestigation2D 后续步骤）（31）
    private readonly HashSet<Collider2D> overlappingPlayerColliders = new HashSet<Collider2D>();
    private bool playerInRange => overlappingPlayerColliders.Count > 0;
// 记录 WaterDispenserInvestigation2D 的当前状态
    private bool dialoguePlaying;
    private bool interactionSuppressed;

// 推进 WaterDispenserInvestigation2D 的当前步骤
    public bool IsInteractionPromptEligible
    {
// 在 WaterDispenserInvestigation2D 中处理 推进 WaterDispenserInvestigation2D 的当前步骤
        get
        {
// 返回 WaterDispenserInvestigation2D 的处理结果
            return isActiveAndEnabled
                && playerInRange
// 推进 WaterDispenserInvestigation2D 的当前步骤（WaterDispenserInvestigation2D）
                && !dialoguePlaying
                && !interactionSuppressed;
        }
    }

// 初始化组件引用和运行状态
    private void Awake()
    {
// 获取 Awake 的组件引用
        GetComponent<BoxCollider2D>().isTrigger = true;
        HidePrompt();
    }

// 读取初始依赖并同步首帧状态
    private void Start()
    {
// 推进 Start 中的必要步骤
        ApplyState(IsRoomReady());
    }

// 禁用时取消订阅并清理临时状态
    private void OnDisable()
    {
// 使用 OnDisable 所需功能
        overlappingPlayerColliders.Clear();
        PlayerInteractionPromptController.UnregisterSource(this);
// 使用 OnDisable 所需功能（OnDisable）
        CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
    }

// 销毁时释放事件订阅和静态引用
    private void OnDestroy()
    {
// 使用 OnDestroy 所需功能
        PlayerInteractionPromptController.UnregisterSource(this);
        CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
// 推进 OnDestroy 中的必要步骤
        HidePrompt();
    }

// 每帧检查输入与状态变化
    private void Update()
    {
// 检查 Update 的前置条件
        if (GameplayInputLock.IsInteractionLocked)
        {
// 同步 Update 的状态
            interactionSuppressed = true;
            HidePrompt();
// 返回 Update 的处理结果
            return;
        }

// 在 Update 中处理 检查 Update 的前置条件
        if (interactionSuppressed)
        {
// 同步 Update 的内部状态
            interactionSuppressed = false;
            ShowPromptIfInRange();
        }

// 检测按键输入
        if (!playerInRange || dialoguePlaying || !Input.GetKeyDown(KeyCode.F))
        {
// 返回 Update 的处理结果（Update）
            return;
        }

// Update 缺少引用时提前结束
        if (DialogueUIManager.Instance == null || !DialogueUIManager.Instance.CanOpenDialogue)
        {
// 返回 Update 的处理结果（Update）（return）
            return;
        }

// 判断 HasCompletedFirstInteraction 对应条件
        bool completingFirst = !HasCompletedFirstInteraction();
        bool completingMove = !completingFirst && !IsRoomReady();
// 保存 dialogue 引用
        DialogueData dialogue = completingFirst
            ? firstDialogue
// 推进 Update 的当前步骤
            : completingMove ? revealDialogue : repeatDialogue;

// 推进 Update 中的必要步骤
        HidePrompt();
        dialoguePlaying = true;
// 缺少必要引用时退出 Update
        if (dialogue == null)
        {
// 推进 Update 中的必要步骤（Update）
            FinishInteraction(completingFirst, completingMove);
            return;
        }

// 开始当前对话
        DialogueUIManager.Instance.StartDialogue(
            dialogue,
// 使用 Update 所需功能
            () => FinishInteraction(completingFirst, completingMove));
    }

// 处理 FinishInteraction 对应逻辑
    private void FinishInteraction(bool completingFirst, bool completingMove)
    {
// 同步 FinishInteraction 的内部状态
        dialoguePlaying = false;

// 检查 FinishInteraction 的前置条件
        if (completingFirst)
        {
// 检查 FinishInteraction 的前置条件（FinishInteraction）
            if (clueToGrant != null && GameManager.Instance != null)
            {
// 记录当前线索
                GameManager.Instance.CollectClue(clueToGrant);
            }

// 检查 FinishInteraction 的前置条件（FinishInteraction）（if）
            if (GameManager.Instance != null)
            {
// 累加调查次数
                GameManager.Instance.AddInvestigation();
                GameManager.Instance.SetFlag(movedFlag);
            }
        }
// 检查其他条件
        else if (completingMove)
        {
// 在 FinishInteraction 中继续当前处理
            if (GameManager.Instance != null)
            {
// 更新剧情标记
                GameManager.Instance.SetFlag(roomReadyFlag);
            }

// 使用 FinishInteraction 所需功能
            overlappingPlayerColliders.Clear();
            CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
// 使用 FinishInteraction 所需功能（FinishInteraction）
            PlayerInteractionPromptController.UnregisterSource(this);
            HidePrompt();
// 推进 FinishInteraction 中的必要步骤
            ApplyState(true);
        }

// 推进 FinishInteraction 中的必要步骤（FinishInteraction）
        ShowPromptIfInRange();
    }

// 显示 ShowPromptIfInRange 对应界面
    private void ShowPromptIfInRange()
    {
// 检查 ShowPromptIfInRange 的前置条件
        if (playerInRange)
        {
// 推进 ShowPromptIfInRange 中的必要步骤
            ShowPrompt();
        }
    }

// 在 ShowPromptIfInRange 中处理 HasCompletedFirstInteraction
    private bool HasCompletedFirstInteraction()
    {
// 返回 HasCompletedFirstInteraction 的处理结果
        return GameManager.Instance != null && GameManager.Instance.HasFlag(movedFlag);
    }

// 判断 IsRoomReady 对应条件
    private bool IsRoomReady()
    {
// 返回 IsRoomReady 的处理结果
        return GameManager.Instance != null && GameManager.Instance.HasFlag(roomReadyFlag);
    }

// 应用 ApplyState 对应设置
    private void ApplyState(bool moved)
    {
// 更新局部位置
        transform.localPosition = moved ? movedLocalPosition : initialLocalPosition;
    }

// 玩家进入范围后登记可交互状态
    private void OnTriggerEnter2D(Collider2D other)
    {
// 检查 OnTriggerEnter2D 的前置条件
        if (other.CompareTag(playerTag))
        {
// 记录 OnTriggerEnter2D 的当前状态
            bool wasInRange = playerInRange;
            overlappingPlayerColliders.Add(other);
// 检查 OnTriggerEnter2D 的前置条件（OnTriggerEnter2D）
            if (!wasInRange)
            {
// 使用 OnTriggerEnter2D 所需功能
                PlayerInteractionPromptController.RegisterSource(this);
                CluePickup2D.SetExclusiveInteractionTarget(gameObject);
            }
// 使用 OnTriggerEnter2D 所需功能（OnTriggerEnter2D）
            PlayerInteractionPromptController.RefreshSource(this);
        }
    }

// 玩家离开范围后移除可交互状态
    private void OnTriggerExit2D(Collider2D other)
    {
// 检查 OnTriggerExit2D 的前置条件
        if (other.CompareTag(playerTag))
        {
// 使用 OnTriggerExit2D 所需功能
            overlappingPlayerColliders.Remove(other);
            if (!playerInRange)
            {
// 使用 OnTriggerExit2D 所需功能（OnTriggerExit2D）
                PlayerInteractionPromptController.UnregisterSource(this);
                CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
            }
// 在 OnTriggerExit2D 中处理 RefreshSource
            PlayerInteractionPromptController.RefreshSource(this);
        }
    }

// 显示 ShowPrompt 对应界面
    private void ShowPrompt()
    {
// 使用 ShowPrompt 所需功能
        PlayerInteractionPromptController.RefreshSource(this);
    }

// 隐藏 HidePrompt 对应界面
    private void HidePrompt()
    {
// 使用 HidePrompt 所需功能
        PlayerInteractionPromptController.RefreshSource(this);
    }
}
