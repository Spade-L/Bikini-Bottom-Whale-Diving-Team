using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 使用 当前脚本 所需功能
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
// 配置 移动设置 分组
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 4f;
// 记录 PlayerMovement2D 的当前状态
    [SerializeField] private bool allowDiagonalMovement = false;
    [Header("点击移动")]
    [SerializeField] private bool enableClickToMove = true;
    [SerializeField] private LayerMask clickMoveObstacleMask = ~0;
    [SerializeField] private float clickMoveStopDistance = 0.05f;
    [Header("动画设置（可选）")]
// 同步 PlayerMovement2D 的相关数据
    [SerializeField] private Animator animator;
    [SerializeField] private string walkDownState = "前进";
// 同步 PlayerMovement2D 的相关数据（PlayerMovement2D 后续步骤）
    [SerializeField] private string walkUpState = "背身";
    [SerializeField] private string walkLeftState = "左走";
// 同步 PlayerMovement2D 的相关数据（PlayerMovement2D 后续步骤）（19）
    [SerializeField] private string walkRightState = "右走";
    [Header("待机静止帧（停下时按最后朝向显示）")]
// 同步 PlayerMovement2D 的相关数据（PlayerMovement2D 后续步骤）（22）
    [SerializeField] private Sprite idleDown;
    [SerializeField] private Sprite idleUp;
// 同步 PlayerMovement2D 的相关数据（PlayerMovement2D 后续步骤）（25）
    [SerializeField] private Sprite idleLeft;
    [SerializeField] private Sprite idleRight;
// 配置 行走脚步声（循环） 分组
    [Header("行走脚步声（循环）")]
    [SerializeField] private AudioClip footstepLoop;
// 限制当前数值范围
    [Range(0f, 1f)] [SerializeField] private float footstepVolume = 0.5f;

// 同步 PlayerMovement2D 的相关数据（PlayerMovement2D 后续步骤）（34）
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
// 同步 PlayerMovement2D 的相关数据（PlayerMovement2D 后续步骤）（37）
    private AudioSource footstepSource;
    private Vector2 moveInput;
// 同步 PlayerMovement2D 的相关数据（PlayerMovement2D 后续步骤）（40）
    private Vector2 lastMoveDirection = Vector2.down;
    private string currentWalkState;
// 记录 PlayerMovement2D 的当前状态（PlayerMovement2D 后续步骤）
    private bool endingMovementActive;
    private Vector2 endingTarget;
// 设置 PlayerMovement2D 的配置数值
    private float endingSpeed;
    private float endingTolerance;
// 记录 PlayerMovement2D 的当前状态（PlayerMovement2D 后续步骤）（49）
    private bool endingMovementFinished;
    private Collider2D[] endingMovementColliders;
    private bool clickMoveActive;
    private Vector2 clickMoveTarget;
    private ContactFilter2D clickMoveFilter;
    private readonly RaycastHit2D[] clickMoveHits = new RaycastHit2D[8];
    private static readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

// 初始化组件引用和运行状态
    private void Awake()
    {
// 同步 Awake 的状态
        rb = GetComponent<Rigidbody2D>();
        endingMovementColliders = GetComponentsInChildren<Collider2D>(true);
        clickMoveFilter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = clickMoveObstacleMask
        };
// 同步 Awake 的内部状态
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
// 同步 Awake 的内部状态（Awake）
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
// 同步 Awake 的内部状态（Awake）（footstepSource）
        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.clip = footstepLoop;
// 在 Awake 中继续当前处理
        footstepSource.loop = true;
        footstepSource.playOnAwake = false;
// 在 Awake 中继续当前处理（Awake 后续步骤）
        footstepSource.spatialBlend = 0f;
        footstepSource.volume = GetEffectiveFootstepVolume();
    }

// 读取初始依赖并同步首帧状态
    private void Start()
    {
// 检查 Start 的前置条件
        if (SettingsManager.Instance != null) SettingsManager.Instance.SfxVolumeChanged += OnSfxVolumeChanged;
        if (animator != null) animator.enabled = false;
// 推进 Start 中的必要步骤
        ApplyIdleSprite();
    }

// 每帧检查输入与状态变化
    private void Update()
    {
// 检查 Update 的前置条件
        if (endingMovementActive)
        {
// 返回 Update 的处理结果
            return;
        }

// 推进 Update 中的必要步骤
        ReadClickToMoveInput();
        ReadMovementInput();
        UpdateAnimator();
// 推进 Update 中的必要步骤（Update）
        UpdateFootsteps();
    }

// 按物理帧推进移动与碰撞
    private void FixedUpdate()
    {
// 检查 FixedUpdate 的前置条件
        if (endingMovementActive)
        {
// 完成 FixedUpdate 的主要职责
            Vector2 next = Vector2.MoveTowards(rb.position, endingTarget, endingSpeed * Time.fixedDeltaTime);
            rb.MovePosition(next);
// 检查 FixedUpdate 的前置条件（FixedUpdate）
            if (Vector2.Distance(next, endingTarget) <= endingTolerance)
            {
// 使用 FixedUpdate 所需功能
                rb.MovePosition(endingTarget);
                endingMovementActive = false;
// 同步 FixedUpdate 的内部状态
                endingMovementFinished = true;
                moveInput = Vector2.zero;
// 推进 FixedUpdate 中的必要步骤
                StopEndingMovement();
            }
// 返回 FixedUpdate 的处理结果
            return;
        }

        if (clickMoveActive && moveInput != Vector2.zero)
        {
            Vector2 step = moveInput * moveSpeed * Time.fixedDeltaTime;
            if (IsClickMoveBlocked(step))
            {
                CancelClickToMove();
                moveInput = Vector2.zero;
                return;
            }
        }

// 使用 FixedUpdate 所需功能（FixedUpdate）
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    // 处理 MoveToEndingTarget 对应逻辑
    public IEnumerator MoveToEndingTarget(Vector3 target, Vector2 direction, float speedMultiplier = 0.5f, float timeout = 8f, float tolerance = 0.03f)
    {
// 推进 MoveToEndingTarget 中的必要步骤
        SetEndingCollisionsEnabled(false);
        direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.down;
// 同步 MoveToEndingTarget 的内部状态
        lastMoveDirection = direction;
        endingTarget = target;
// 检查 MoveToEndingTarget 的前置条件
        if (animator != null) animator.speed = Mathf.Max(0.01f, speedMultiplier);
        endingSpeed = Mathf.Max(0.01f, moveSpeed * speedMultiplier);
// 同步 MoveToEndingTarget 的内部状态（MoveToEndingTarget）
        endingTolerance = Mathf.Max(0.001f, tolerance);
        endingMovementFinished = false;
// 同步 MoveToEndingTarget 的内部状态（MoveToEndingTarget）（endingMovementActive）
        endingMovementActive = true;
        moveInput = direction;
// 推进 MoveToEndingTarget 中的必要步骤（MoveToEndingTarget）
        PlayWalkState(direction);

// 设置 MoveToEndingTarget 的配置数值
        float elapsed = 0f;
        while (!endingMovementFinished && elapsed < Mathf.Max(0.1f, timeout))
        {
// 推进 MoveToEndingTarget 的当前步骤
            elapsed += Time.deltaTime;
            yield return null;
        }

// 检查 MoveToEndingTarget 的前置条件（MoveToEndingTarget）
        if (endingMovementActive)
        {
// 在 MoveToEndingTarget 中继续当前处理
            endingMovementActive = false;
            moveInput = Vector2.zero;
// 在 MoveToEndingTarget 中处理 StopEndingMovement
            StopEndingMovement();
        }
    }

// 设置 SetEndingIdleLeft 的目标状态
    public void SetEndingIdleLeft()
    {
// 同步 SetEndingIdleLeft 的内部状态
        lastMoveDirection = Vector2.left;
        moveInput = Vector2.zero;
// 推进 SetEndingIdleLeft 中的必要步骤
        StopEndingMovement();
    }

// 设置 SetEndingSprite 的目标状态
    public void SetEndingSprite(Sprite sprite)
    {
// 推进 SetEndingSprite 中的必要步骤
        StopEndingMovement();
        if (spriteRenderer != null && sprite != null)
        {
// 更新启用状态
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = sprite;
        }
    }

// 停止 StopEndingMovement 对应流程
    private void StopEndingMovement()
    {
// 检查 StopEndingMovement 的前置条件
        if (footstepSource != null && footstepSource.isPlaying) footstepSource.Stop();
        if (animator != null)
        {
// 同步 StopEndingMovement 的内部状态
            animator.speed = 1f;
            animator.enabled = false;
// 同步 StopEndingMovement 的内部状态（StopEndingMovement）
            currentWalkState = null;
        }
// 推进 StopEndingMovement 中的必要步骤
        ApplyIdleSprite();
        SetEndingCollisionsEnabled(true);
    }

// 设置 SetEndingCollisionsEnabled 的目标状态
    private void SetEndingCollisionsEnabled(bool enabled)
    {
// SetEndingCollisionsEnabled 缺少引用时提前结束
        if (endingMovementColliders == null) return;
        foreach (Collider2D collider in endingMovementColliders)
        {
// 检查 SetEndingCollisionsEnabled 的前置条件
            if (collider != null) collider.enabled = enabled;
        }
    }

// 播放 PlayWalkState 对应演出
    private void PlayWalkState(Vector2 direction)
    {
// 缺少必要引用时退出 PlayWalkState
        if (animator == null) return;
        string state = ResolveWalkState(direction);
// 检查 PlayWalkState 的前置条件
        if (!animator.enabled) { animator.enabled = true; currentWalkState = null; }
        animator.Play(state, 0, 0f);
// 同步 PlayWalkState 的内部状态
        currentWalkState = state;
    }

// 判断 IsMovementLocked 对应条件
    private bool IsMovementLocked()
    {
// 检查 IsMovementLocked 的前置条件
        if (GameplayInputLock.IsMovementLocked) return true;
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen) return true;
// 检查 IsMovementLocked 的前置条件（IsMovementLocked）
        if (InvestigationDirector.Instance != null && InvestigationDirector.Instance.IsPlayingFlashback) return true;
        return ScreenFader.IsFading;
    }

    private void ReadClickToMoveInput()
    {
        if (!enableClickToMove || endingMovementActive || IsMovementLocked())
        {
            return;
        }

        if (IsPointerOverInteractiveUI())
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            return;
        }

        Vector3 world = targetCamera.ScreenToWorldPoint(Input.mousePosition);
        clickMoveTarget = new Vector2(world.x, world.y);
        clickMoveActive = true;
    }

// 处理 ReadMovementInput 对应逻辑
    private void ReadMovementInput()
    {
// 检查 ReadMovementInput 的前置条件
        if (IsMovementLocked())
        {
            CancelClickToMove();
            moveInput = Vector2.zero;
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
// 缓存 ReadMovementInput 所需引用
        float vertical = Input.GetAxisRaw("Vertical");
        if (!allowDiagonalMovement)
        {
// 检查 ReadMovementInput 的前置条件（ReadMovementInput）
            if (Mathf.Abs(horizontal) > 0f) vertical = 0f;
            else if (Mathf.Abs(vertical) > 0f) horizontal = 0f;
        }

        Vector2 manualInput = new Vector2(horizontal, vertical).normalized;
        if (manualInput != Vector2.zero)
        {
            CancelClickToMove();
            moveInput = manualInput;
            lastMoveDirection = manualInput;
            return;
        }

        if (clickMoveActive)
        {
            Vector2 toTarget = clickMoveTarget - rb.position;
            if (toTarget.sqrMagnitude <= clickMoveStopDistance * clickMoveStopDistance)
            {
                CancelClickToMove();
                moveInput = Vector2.zero;
                return;
            }

            moveInput = toTarget.normalized;
            lastMoveDirection = moveInput;
            return;
        }

        moveInput = Vector2.zero;
    }

    private static bool IsPointerOverInteractiveUI()
    {
        if (PlayerInteractionPromptController.IsPointerOverActivePrompt(Input.mousePosition))
        {
            return true;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        uiRaycastResults.Clear();
        eventSystem.RaycastAll(pointerData, uiRaycastResults);
        foreach (RaycastResult result in uiRaycastResults)
        {
            GameObject target = result.gameObject;
            if (target == null)
            {
                continue;
            }

            if (target.GetComponentInParent<Selectable>() != null
                || ExecuteEvents.GetEventHandler<IPointerClickHandler>(target) != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsClickMoveBlocked(Vector2 step)
    {
        if (step.sqrMagnitude <= 0.000001f)
        {
            return false;
        }

        clickMoveFilter.layerMask = clickMoveObstacleMask;
        return rb.Cast(step.normalized, clickMoveFilter, clickMoveHits, step.magnitude) > 0;
    }

    private void CancelClickToMove()
    {
        clickMoveActive = false;
        clickMoveTarget = Vector2.zero;
    }

// 刷新 UpdateAnimator 对应状态
    private void UpdateAnimator()
    {
// 缺少必要引用时退出 UpdateAnimator
        if (animator == null) return;
        if (moveInput != Vector2.zero)
        {
// 解析 ResolveWalkState 对应结果
            string targetState = ResolveWalkState(moveInput);
            if (!animator.enabled) { animator.enabled = true; currentWalkState = null; }
// 检查 UpdateAnimator 的前置条件
            if (targetState != currentWalkState) { animator.Play(targetState, 0, 0f); currentWalkState = targetState; }
            else
            {
// 判断 IsInTransition 对应条件
                bool leavingTarget = animator.IsInTransition(0)
                    ? !animator.GetNextAnimatorStateInfo(0).IsName(targetState)
// 使用 UpdateAnimator 所需功能
                    : !animator.GetCurrentAnimatorStateInfo(0).IsName(targetState);
                if (leavingTarget) animator.Play(targetState, 0, 0f);
            }
        }
// 检查其他条件
        else if (animator.enabled)
        {
// 在 UpdateAnimator 中继续当前处理
            animator.enabled = false;
            currentWalkState = null;
// 推进 UpdateAnimator 中的必要步骤
            ApplyIdleSprite();
        }
    }

// 在 UpdateAnimator 中处理 ResolveWalkState
    private string ResolveWalkState(Vector2 direction)
    {
// 检查 ResolveWalkState 的前置条件
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)) return direction.x < 0f ? walkLeftState : walkRightState;
        return direction.y < 0f ? walkDownState : walkUpState;
    }

// 刷新 UpdateFootsteps 对应状态
    private void UpdateFootsteps()
    {
// 缺少必要引用时退出 UpdateFootsteps
        if (footstepSource == null || footstepSource.clip == null) return;
        bool moving = moveInput != Vector2.zero;
// 检查 UpdateFootsteps 的前置条件
        if (moving && !footstepSource.isPlaying) { footstepSource.volume = GetEffectiveFootstepVolume(); footstepSource.Play(); }
        else if (!moving && footstepSource.isPlaying) footstepSource.Stop();
    }

// 响应 OnSfxVolumeChanged 生命周期
    private void OnSfxVolumeChanged(float value)
    {
// 检查 OnSfxVolumeChanged 的前置条件
        if (footstepSource != null) footstepSource.volume = GetEffectiveFootstepVolume();
    }

// 获取 GetEffectiveFootstepVolume 所需引用
    private float GetEffectiveFootstepVolume()
    {
// 设置 GetEffectiveFootstepVolume 的配置数值
        float globalVolume = SettingsManager.Instance == null ? 1f : SettingsManager.Instance.SfxVolume;
        return Mathf.Clamp01(footstepVolume * globalVolume);
    }

// 销毁时释放事件订阅和静态引用
    private void OnDestroy()
    {
// 检查 OnDestroy 的前置条件
        if (SettingsManager.Instance != null) SettingsManager.Instance.SfxVolumeChanged -= OnSfxVolumeChanged;
    }

// 应用 ApplyIdleSprite 对应设置
    private void ApplyIdleSprite()
    {
// 缺少必要引用时退出 ApplyIdleSprite
        if (spriteRenderer == null) return;
        Sprite idle;
// 检查 ApplyIdleSprite 的前置条件
        if (Mathf.Abs(lastMoveDirection.x) >= Mathf.Abs(lastMoveDirection.y)) idle = lastMoveDirection.x < 0f ? idleLeft : idleRight;
        else idle = lastMoveDirection.y < 0f ? idleDown : idleUp;
// 检查 ApplyIdleSprite 的前置条件（ApplyIdleSprite）
        if (idle != null) spriteRenderer.sprite = idle;
    }
}
