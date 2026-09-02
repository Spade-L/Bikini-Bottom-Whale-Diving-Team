using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景出入口门。满足条件（通常 requiredFlags: scene_cleared_xxx 或 final_door_open）
/// 时可通行，否则播放“门是关着的”台词。
/// 也用于封锁回头路：returnDoor 勾选 + lockedByFlag 填 lock_early_scenes，
/// 28 次调查后播放“后路被封了”。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class SceneDoor : MonoBehaviour
{
    // Inspector 中指定的目标场景名称。
    [Header("目标场景")]
    // 为空时会记录警告而不加载场景。
    [SerializeField] private string targetSceneName;

    // 门可用的故事条件配置。
    [Header("开启条件（如 requiredFlags: scene_cleared_home）")]
    // 由条件对象集中判定 Flag 等要求。
    [SerializeField] private StoryCondition openCondition = new StoryCondition();

    // 交互时使用的可选对话资源。
    [Header("台词")]
    // 条件失败时播放的反馈对话。
    [Tooltip("条件不满足时（如“门锁着，好像还缺少什么线索”）")]
    // 可不配置，届时仅阻止通行。
    [SerializeField] private DialogueData lockedDialogue;
    // 成功进门前播放的可选过场对话。
    [Tooltip("进门前播放的对话（可空，播完才切场景）")]
    [SerializeField] private DialogueData enterDialogue;

    // 可选的屏幕交互提示对象。
    [Header("交互 UI")]
    // 玩家进入范围时显示，离开时隐藏。
    [SerializeField] private GameObject interactionUI;
    // 用于筛选触发器中代表玩家的对象。
    [SerializeField] private string playerTag = "Player";

    // 仅在玩家处于触发区域内时接受交互输入。
    private bool playerInRange;
    // 对话播完至场景切换期间，阻止重复触发进门流程。
    private bool isTransitioning;

    // 初始化门的触发器与提示状态。
    private void Awake()
    {
        // 强制门的碰撞器只用于检测进入与离开。
        GetComponent<BoxCollider2D>().isTrigger = true;

        // 场景加载时先隐藏交互提示。
        if (interactionUI != null)
        {
            interactionUI.SetActive(false);
        }
    }

    // 每帧读取玩家的门交互输入。
    private void Update()
    {
        // 需同时满足范围内、未转场和按下 E 才继续。
        if (!playerInRange || isTransitioning || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        // 对话系统忙碌或不可用时，不抢占当前对话。
        if (DialogueUIManager.Instance == null || !DialogueUIManager.Instance.CanOpenDialogue)
        {
            return;
        }

        // 条件不足时仅播放锁门提示，不进入转场状态。
        if (!openCondition.IsMet())
        {
            // 未配置提示台词时保持静默。
            if (lockedDialogue != null)
            {
                DialogueUIManager.Instance.StartDialogue(lockedDialogue);
            }
            return;
        }

        // 有进门对话则等待其回调后再切换场景。
        if (enterDialogue != null)
        {
            // 先上锁，避免对话期间重复按键注册多个回调。
            isTransitioning = true;
            // 对话完成后由回调统一调用加载方法。
            DialogueUIManager.Instance.StartDialogue(enterDialogue, LoadTargetScene);
        }
        else
        {
            // 未配置过场对话时直接前往目标场景。
            LoadTargetScene();
        }
    }

    // 在满足门条件后负责执行实际场景加载。
    private void LoadTargetScene()
    {
        // 目标名为空时保留当前场景并解除转场锁。
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning($"[SceneDoor] {name} 未设置目标场景名");
            isTransitioning = false;
            return;
        }

        // 可用时通过淡出过渡加载场景，否则立即加载。
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeOutThen(() => SceneManager.LoadScene(targetSceneName));
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    // 玩家进入门的触发区域时开启交互。
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 只有指定标签的玩家进入才显示交互提示。
        // 统一使用配置的标签，避免硬编码玩家对象。
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;

            // 提示对象可选，缺失时仅保留可交互状态。
            if (interactionUI != null)
            {
                interactionUI.SetActive(true);
            }
        }
    }

    // 玩家离开门的触发区域时关闭交互。
    private void OnTriggerExit2D(Collider2D other)
    {
        // 玩家离开后撤销范围状态和交互提示。
        // 统一使用配置的标签，避免硬编码玩家对象。
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;

            if (interactionUI != null)
            {
                interactionUI.SetActive(false);
            }
        }
    }
}
