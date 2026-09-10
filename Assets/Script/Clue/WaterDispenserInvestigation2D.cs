using System.Collections.Generic;
using UnityEngine;

// 调用 RequireComponent
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
// 保存 movedLocalPosition 数据
    [SerializeField] private Vector3 movedLocalPosition = new Vector3(12f, 8f, 0f);
    [SerializeField] private string movedFlag = "school_water_dispenser_moved";
// 保存 roomReadyFlag 数据
    [SerializeField] private string roomReadyFlag = "school_water_dispenser_room_ready";

// 配置 交互 UI 分组
    [Header("交互 UI")]
    [SerializeField] private GameObject interactionUI;
// 保存 playerTag 数据
    [SerializeField] private string playerTag = "Player";

// 保存 overlappingPlayerColliders 数据
    private readonly HashSet<Collider2D> overlappingPlayerColliders = new HashSet<Collider2D>();
    private bool playerInRange => overlappingPlayerColliders.Count > 0;
// 记录 dialoguePlaying 状态
    private bool dialoguePlaying;
    private bool interactionSuppressed;

// 更新当前逻辑
    public bool IsInteractionPromptEligible
    {
// 更新当前逻辑
        get
        {
// 返回当前结果
            return isActiveAndEnabled
                && playerInRange
// 更新当前逻辑
                && !dialoguePlaying
                && !interactionSuppressed;
        }
    }

// 定义 Awake 方法
    private void Awake()
    {
// 获取组件引用
        GetComponent<BoxCollider2D>().isTrigger = true;
        HidePrompt();
    }

// 定义 Start 方法
    private void Start()
    {
// 执行 ApplyState
        ApplyState(IsRoomReady());
    }

// 定义 OnDisable 方法
    private void OnDisable()
    {
// 调用 Clear
        overlappingPlayerColliders.Clear();
        PlayerInteractionPromptController.UnregisterSource(this);
// 调用 ClearExclusiveInteractionTarget
        CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
    }

// 定义 OnDestroy 方法
    private void OnDestroy()
    {
// 调用 UnregisterSource
        PlayerInteractionPromptController.UnregisterSource(this);
        CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
// 执行 HidePrompt
        HidePrompt();
    }

// 定义 Update 方法
    private void Update()
    {
// 判断当前条件
        if (GameplayInputLock.IsInteractionLocked)
        {
// 更新当前状态
            interactionSuppressed = true;
            HidePrompt();
// 返回当前结果
            return;
        }

// 判断当前条件
        if (interactionSuppressed)
        {
// 更新当前状态
            interactionSuppressed = false;
            ShowPromptIfInRange();
        }

// 检测按键输入
        if (!playerInRange || dialoguePlaying || !Input.GetKeyDown(KeyCode.F))
        {
// 返回当前结果
            return;
        }

// 空引用时直接退出
        if (DialogueUIManager.Instance == null || !DialogueUIManager.Instance.CanOpenDialogue)
        {
// 返回当前结果
            return;
        }

// 定义 HasCompletedFirstInteraction 方法
        bool completingFirst = !HasCompletedFirstInteraction();
        bool completingMove = !completingFirst && !IsRoomReady();
// 保存 dialogue 引用
        DialogueData dialogue = completingFirst
            ? firstDialogue
// 更新当前逻辑
            : completingMove ? revealDialogue : repeatDialogue;

// 执行 HidePrompt
        HidePrompt();
        dialoguePlaying = true;
// 空引用时直接退出
        if (dialogue == null)
        {
// 执行 FinishInteraction
            FinishInteraction(completingFirst, completingMove);
            return;
        }

// 开始当前对话
        DialogueUIManager.Instance.StartDialogue(
            dialogue,
// 调用 FinishInteraction
            () => FinishInteraction(completingFirst, completingMove));
    }

// 定义 FinishInteraction 方法
    private void FinishInteraction(bool completingFirst, bool completingMove)
    {
// 更新当前状态
        dialoguePlaying = false;

// 判断当前条件
        if (completingFirst)
        {
// 判断当前条件
            if (clueToGrant != null && GameManager.Instance != null)
            {
// 记录当前线索
                GameManager.Instance.CollectClue(clueToGrant);
            }

// 判断当前条件
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
// 判断当前条件
            if (GameManager.Instance != null)
            {
// 更新剧情标记
                GameManager.Instance.SetFlag(roomReadyFlag);
            }

// 调用 Clear
            overlappingPlayerColliders.Clear();
            CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
// 调用 UnregisterSource
            PlayerInteractionPromptController.UnregisterSource(this);
            HidePrompt();
// 执行 ApplyState
            ApplyState(true);
        }

// 执行 ShowPromptIfInRange
        ShowPromptIfInRange();
    }

// 定义 ShowPromptIfInRange 方法
    private void ShowPromptIfInRange()
    {
// 判断当前条件
        if (playerInRange)
        {
// 执行 ShowPrompt
            ShowPrompt();
        }
    }

// 定义 HasCompletedFirstInteraction 方法
    private bool HasCompletedFirstInteraction()
    {
// 返回当前结果
        return GameManager.Instance != null && GameManager.Instance.HasFlag(movedFlag);
    }

// 定义 IsRoomReady 方法
    private bool IsRoomReady()
    {
// 返回当前结果
        return GameManager.Instance != null && GameManager.Instance.HasFlag(roomReadyFlag);
    }

// 定义 ApplyState 方法
    private void ApplyState(bool moved)
    {
// 更新局部位置
        transform.localPosition = moved ? movedLocalPosition : initialLocalPosition;
    }

// 定义 OnTriggerEnter2D 方法
    private void OnTriggerEnter2D(Collider2D other)
    {
// 判断当前条件
        if (other.CompareTag(playerTag))
        {
// 记录 wasInRange 状态
            bool wasInRange = playerInRange;
            overlappingPlayerColliders.Add(other);
// 判断当前条件
            if (!wasInRange)
            {
// 调用 RegisterSource
                PlayerInteractionPromptController.RegisterSource(this);
                CluePickup2D.SetExclusiveInteractionTarget(gameObject);
            }
// 调用 RefreshSource
            PlayerInteractionPromptController.RefreshSource(this);
        }
    }

// 定义 OnTriggerExit2D 方法
    private void OnTriggerExit2D(Collider2D other)
    {
// 判断当前条件
        if (other.CompareTag(playerTag))
        {
// 调用 Remove
            overlappingPlayerColliders.Remove(other);
            if (!playerInRange)
            {
// 调用 UnregisterSource
                PlayerInteractionPromptController.UnregisterSource(this);
                CluePickup2D.ClearExclusiveInteractionTarget(gameObject);
            }
// 调用 RefreshSource
            PlayerInteractionPromptController.RefreshSource(this);
        }
    }

// 定义 ShowPrompt 方法
    private void ShowPrompt()
    {
// 调用 RefreshSource
        PlayerInteractionPromptController.RefreshSource(this);
    }

// 定义 HidePrompt 方法
    private void HidePrompt()
    {
// 调用 RefreshSource
        PlayerInteractionPromptController.RefreshSource(this);
    }
}
