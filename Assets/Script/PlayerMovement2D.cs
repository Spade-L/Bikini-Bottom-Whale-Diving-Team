using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private bool allowDiagonalMovement = false;

    [Header("动画设置（可选）")]
    [SerializeField] private Animator animator;
    [Tooltip("四向行走动画在控制器中的状态名")]
    [SerializeField] private string walkDownState = "前进";
    [SerializeField] private string walkUpState = "背身";
    [SerializeField] private string walkLeftState = "左走";
    [SerializeField] private string walkRightState = "右走";

    [Header("待机静止帧（停下时按最后朝向显示）")]
    [SerializeField] private Sprite idleDown;
    [SerializeField] private Sprite idleUp;
    [SerializeField] private Sprite idleLeft;
    [SerializeField] private Sprite idleRight;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;
    private string currentWalkState;

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
    }

    private void Start()
    {
        // 出生时静止：关掉 Animator，显示朝下的待机帧
        if (animator != null)
        {
            animator.enabled = false;
        }

        ApplyIdleSprite();
    }

    private void Update()
    {
        ReadMovementInput();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

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

    private string ResolveWalkState(Vector2 direction)
    {
        // 斜向移动时以水平朝向优先
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            return direction.x < 0f ? walkLeftState : walkRightState;
        }

        return direction.y < 0f ? walkDownState : walkUpState;
    }

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
