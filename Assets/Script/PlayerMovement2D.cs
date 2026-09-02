using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    // 移动参数。
    // 速度控制。
    // 斜向开关。
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private bool allowDiagonalMovement = false;

    // 动画引用。
    // 四向状态。
    // 可选配置。
    [Header("动画设置（可选）")]
    [SerializeField] private Animator animator;
    [Tooltip("四向行走动画在控制器中的状态名")]
    [SerializeField] private string walkDownState = "前进";
    [SerializeField] private string walkUpState = "背身";
    [SerializeField] private string walkLeftState = "左走";
    [SerializeField] private string walkRightState = "右走";

    // 待机贴图。
    // 保留朝向。
    // 四个方向。
    [Header("待机静止帧（停下时按最后朝向显示）")]
    [SerializeField] private Sprite idleDown;
    [SerializeField] private Sprite idleUp;
    [SerializeField] private Sprite idleLeft;
    [SerializeField] private Sprite idleRight;

    // 脚步音效。
    // 循环播放。
    // 音量设置。
    [Header("行走脚步声（循环）")]
    [SerializeField] private AudioClip footstepLoop;
    [Range(0f, 1f)]
    [SerializeField] private float footstepVolume = 0.5f;

    // 物理组件。
    // 渲染组件。
    // 音频组件。
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private AudioSource footstepSource;
    // 当前输入。
    // 最后朝向。
    // 动画状态。
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;
    private string currentWalkState;

    // 获取组件。
    // 配置刚体。
    // 准备音源。
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.clip = footstepLoop;
        footstepSource.loop = true;
        footstepSource.playOnAwake = false;
        footstepSource.spatialBlend = 0f;
        footstepSource.volume = footstepVolume;
    }

    // 初始化待机。
    // 停用动画。
    // 应用贴图。
    private void Start()
    {
        // 出生时静止：关掉 Animator，显示朝下的待机帧
        if (animator != null)
        {
            animator.enabled = false;
        }

        ApplyIdleSprite();
    }

    // 读取输入。
    // 更新动画。
    // 更新音效。
    private void Update()
    {
        ReadMovementInput();
        UpdateAnimator();
        UpdateFootsteps();
    }

    // 执行物理移动。
    // 使用固定步长。
    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    // 判断移动锁定。
    // 对话期间锁定。
    // 演出期间锁定。
    // 渐变期间锁定。
    /// <summary>对话框打开、闪回演出或黑幕渐变期间禁止移动。</summary>
    private bool IsMovementLocked()
    {
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
        {
            return true;
        }

        if (InvestigationDirector.Instance != null && InvestigationDirector.Instance.IsPlayingFlashback)
        {
            return true;
        }

        if (ScreenFader.IsFading)
        {
            return true;
        }

        return false;
    }

    // 处理移动输入。
    // 锁定时清零。
    // 读取轴值。
    // 限制斜向。
    // 记录方向。
    private void ReadMovementInput()
    {
        if (IsMovementLocked())
        {
            moveInput = Vector2.zero;
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (!allowDiagonalMovement)
        {
            if (Mathf.Abs(horizontal) > 0f)
            {
                vertical = 0f;
            }
            else if (Mathf.Abs(vertical) > 0f)
            {
                horizontal = 0f;
            }
        }

        moveInput = new Vector2(horizontal, vertical).normalized;

        if (moveInput != Vector2.zero)
        {
            lastMoveDirection = moveInput;
        }
    }

    // 更新行走动画。
    // 无组件时跳过。
    // 移动时播放。
    // 静止时待机。
    // 同步显示。
    // 避免重复。
    // 方向明确。
    // 及时切换。
    // 保持稳定。
    // 处理过渡。
    // 防止覆盖。
    // 回到目标。
    // 清理状态。
    // 应用待机。
    // 逻辑独立。
    // 每帧调用。
    // 配置可选。
    // 行走优先。
    // 静止优先。
    // 保持一致。
    // 状态缓存。
    // 避免闪烁。
    // 朝向同步。
    // 贴图优先。
    // 动画可控。
    // 结果直观。
    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        if (moveInput != Vector2.zero)
        {
            // 移动中：启用 Animator 并直接播放对应方向的行走动画
            string targetState = ResolveWalkState(moveInput);

            if (!animator.enabled)
            {
                animator.enabled = true;
                currentWalkState = null;
            }

            if (targetState != currentWalkState)
            {
                animator.Play(targetState, 0, 0f);
                currentWalkState = targetState;
            }
            else
            {
                // 控制器里带 Exit Time 的自动过渡会把状态带去别的方向，这里拉回来
                bool leavingTarget = animator.IsInTransition(0)
                    ? !animator.GetNextAnimatorStateInfo(0).IsName(targetState)
                    : !animator.GetCurrentAnimatorStateInfo(0).IsName(targetState);

                if (leavingTarget)
                {
                    animator.Play(targetState, 0, 0f);
                }
            }
        }
        else if (animator.enabled)
        {
            // 停下：关闭 Animator（防止它继续覆盖 Sprite），按最后朝向显示待机帧
            animator.enabled = false;
            currentWalkState = null;
            ApplyIdleSprite();
        }
    }

    // 匹配行走状态。
    // 水平优先。
    // 返回方向。
    private string ResolveWalkState(Vector2 direction)
    {
        // 斜向移动时以水平朝向优先
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            return direction.x < 0f ? walkLeftState : walkRightState;
        }

        return direction.y < 0f ? walkDownState : walkUpState;
    }

    // 更新脚步声音。
    // 无音频时跳过。
    // 移动时播放。
    // 停止时关闭。
    private void UpdateFootsteps()
    {
        if (footstepSource == null || footstepSource.clip == null)
        {
            return;
        }

        bool moving = moveInput != Vector2.zero;

        if (moving && !footstepSource.isPlaying)
        {
            footstepSource.volume = footstepVolume;
            footstepSource.Play();
        }
        else if (!moving && footstepSource.isPlaying)
        {
            footstepSource.Stop();
        }
    }

    // 应用待机贴图。
    // 无渲染时跳过。
    // 根据朝向选择。
    // 贴图有效时应用。
    private void ApplyIdleSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Sprite idle;

        if (Mathf.Abs(lastMoveDirection.x) >= Mathf.Abs(lastMoveDirection.y))
        {
            idle = lastMoveDirection.x < 0f ? idleLeft : idleRight;
        }
        else
        {
            idle = lastMoveDirection.y < 0f ? idleDown : idleUp;
        }

        if (idle != null)
        {
            spriteRenderer.sprite = idle;
        }
    }
}
