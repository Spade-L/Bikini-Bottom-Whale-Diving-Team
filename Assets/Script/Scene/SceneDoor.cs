using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider2D))]
// 控制场景门条件、过场和目标场景切换
public class SceneDoor : MonoBehaviour, IInteractionPromptSource
{
    // Inspector 中指定的目标场景名称
    [Header("目标场景")]
    // 为空时会记录警告而不加载场景
    [SerializeField] private string targetSceneName;

    // 门可用的故事条件配置
    [Header("开启条件（如 requiredFlags: scene_cleared_home）")]
    // 由条件对象集中判定 Flag 等要求
    [SerializeField] private StoryCondition openCondition = new StoryCondition();

    // 交互时使用的可选对话资源
    [Header("台词")]
    // 条件失败时播放的反馈对话
    [Tooltip("条件不满足时（如“门锁着，好像还缺少什么线索”）")]
    // 可不配置，届时仅阻止通行
    [SerializeField] private DialogueData lockedDialogue;
    // 成功进门前播放的可选过场对话
    [Tooltip("进门前播放的对话（可空，播完才切场景）")]
    [SerializeField] private DialogueData enterDialogue;
// 说明当前配置
    [Tooltip("出口视觉演出（可空，演出完成后才切场景）")]
    [SerializeField] private SceneStoryPresentation exitPresentation;

    // 可选的屏幕交互提示对象
    [Header("交互 UI")]
    // 玩家进入范围时显示，离开时隐藏
    [SerializeField] private GameObject interactionUI;
    // 用于筛选触发器中代表玩家的对象
    [SerializeField] private string playerTag = "Player";

    // 仅在玩家处于触发区域内时接受交互输入
    private bool playerInRange;
    // 对话播完至场景切换期间，阻止重复触发进门流程
    private bool isTransitioning;
    private bool interactionSuppressed;

// 推进 SceneDoor 的当前步骤
    public bool IsInteractionPromptEligible
    {
        get
        {
// 检查 SceneDoor 的前置条件
            if (!isActiveAndEnabled || !playerInRange || isTransitioning || interactionSuppressed)
            {
                return false;
            }

// 返回 SceneDoor 的处理结果
            return openCondition.IsMet() || lockedDialogue != null;
        }
    }

    // 初始化组件引用和运行状态
    private void Awake()
    {
// 获取 Awake 的组件引用
        GetComponent<BoxCollider2D>().isTrigger = true;
        PlayerInteractionPromptController.RefreshSource(this);
    }

// 销毁时释放事件订阅和静态引用
    private void OnDestroy()
    {
        PlayerInteractionPromptController.UnregisterSource(this);
    }

// 禁用时取消订阅并清理临时状态
    private void OnDisable()
    {
        PlayerInteractionPromptController.UnregisterSource(this);
    }

// 每帧检查输入与状态变化
    private void Update()
    {
        if (GameplayInputLock.IsInteractionLocked)
        {
// 同步 Update 的状态
            interactionSuppressed = true;
            PlayerInteractionPromptController.RefreshSource(this);
// 返回 Update 的处理结果
            return;
        }

        if (interactionSuppressed)
        {
// 同步 Update 的内部状态
            interactionSuppressed = false;
            PlayerInteractionPromptController.RefreshSource(this);
        }

        if (!Input.GetKeyDown(KeyCode.F))
        {
            return;
        }

        TriggerInteraction();
    }

    public void TriggerInteraction()
    {
        if (GameplayInputLock.IsInteractionLocked || !IsInteractionPromptEligible)
        {
            return;
        }

        // 对话系统忙碌或不可用时，不抢占当前对话
        if (DialogueUIManager.Instance == null || !DialogueUIManager.Instance.CanOpenDialogue)
        {
            return;
        }

        // 条件不足时仅播放锁门提示，不进入转场状态
        if (!openCondition.IsMet())
        {
            // 未配置提示台词时保持静默
            if (lockedDialogue != null)
            {
// 开始当前对话
                DialogueUIManager.Instance.StartDialogue(lockedDialogue);
            }
            return;
        }

        // 有进门对话则等待其回调后再切换场景
        if (enterDialogue != null)
        {
            // 先上锁，避免对话期间重复按键注册多个回调
            isTransitioning = true;
// 使用 Update 所需功能
            PlayerInteractionPromptController.RefreshSource(this);
            // 对话完成后由回调统一调用加载方法
            DialogueUIManager.Instance.StartDialogue(enterDialogue, LoadTargetScene);
        }
// 处理 Update 的备用分支
        else
        {
            // 无进门对白时也先锁定，避免同一帧重复加载场景
            isTransitioning = true;
// 使用 Update 所需功能（Update）
            PlayerInteractionPromptController.RefreshSource(this);
            LoadTargetScene();
        }
    }

    // 加载 LoadTargetScene 对应数据
    private void LoadTargetScene()
    {
        if (exitPresentation != null)
        {
// 使用 LoadTargetScene 所需功能
            exitPresentation.Play(LoadTargetSceneAfterPresentation);
            return;
        }

// 推进 LoadTargetScene 中的必要步骤
        LoadTargetSceneAfterPresentation();
    }

    // 加载 LoadTargetSceneAfterPresentation 对应数据
    private void LoadTargetSceneAfterPresentation()
    {
        // 目标名为空时保留当前场景并解除转场锁
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning($"[SceneDoor] {name} 未设置目标场景名");
// 同步 LoadTargetSceneAfterPresentation 的内部状态
            isTransitioning = false;
            PlayerInteractionPromptController.RefreshSource(this);
// 返回 LoadTargetSceneAfterPresentation 的处理结果
            return;
        }

        // 可用时通过淡出过渡加载场景，否则立即加载
        if (ScreenFader.Instance != null)
        {
// 执行场景切换
            ScreenFader.Instance.FadeOutThen(() => SceneManager.LoadScene(targetSceneName));
        }
        else
        {
// 在 LoadTargetSceneAfterPresentation 中继续当前处理
            SceneManager.LoadScene(targetSceneName);
        }
    }

    // 玩家进入范围后登记可交互状态
    private void OnTriggerEnter2D(Collider2D other)
    {
// 检查 OnTriggerEnter2D 的前置条件
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
// 使用 OnTriggerEnter2D 所需功能
            PlayerInteractionPromptController.RegisterSource(this);
            PlayerInteractionPromptController.RefreshSource(this);
        }
    }

    // 玩家离开范围后移除可交互状态
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
// 同步 OnTriggerExit2D 的内部状态
            playerInRange = false;
            PlayerInteractionPromptController.UnregisterSource(this);
        }
    }
}
