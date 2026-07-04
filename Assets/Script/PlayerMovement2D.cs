using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private bool allowDiagonalMovement = false;

    [Header("动画设置（可选）")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
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

        animator.SetFloat("MoveX", moveInput.x);
        animator.SetFloat("MoveY", moveInput.y);
        animator.SetFloat("LastMoveX", lastMoveDirection.x);
        animator.SetFloat("LastMoveY", lastMoveDirection.y);
        animator.SetBool("IsMoving", moveInput != Vector2.zero);
    }
}
