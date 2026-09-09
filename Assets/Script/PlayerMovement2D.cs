using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private bool allowDiagonalMovement = false;
    [Header("动画设置（可选）")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkDownState = "前进";
    [SerializeField] private string walkUpState = "背身";
    [SerializeField] private string walkLeftState = "左走";
    [SerializeField] private string walkRightState = "右走";
    [Header("待机静止帧（停下时按最后朝向显示）")]
    [SerializeField] private Sprite idleDown;
    [SerializeField] private Sprite idleUp;
    [SerializeField] private Sprite idleLeft;
    [SerializeField] private Sprite idleRight;
    [Header("行走脚步声（循环）")]
    [SerializeField] private AudioClip footstepLoop;
    [Range(0f, 1f)] [SerializeField] private float footstepVolume = 0.5f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private AudioSource footstepSource;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;
    private string currentWalkState;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.clip = footstepLoop;
        footstepSource.loop = true;
        footstepSource.playOnAwake = false;
        footstepSource.spatialBlend = 0f;
        footstepSource.volume = GetEffectiveFootstepVolume();
    }

    private void Start()
    {
        if (SettingsManager.Instance != null) SettingsManager.Instance.SfxVolumeChanged += OnSfxVolumeChanged;
        if (animator != null) animator.enabled = false;
        ApplyIdleSprite();
    }

    private void Update()
    {
        ReadMovementInput();
        UpdateAnimator();
        UpdateFootsteps();
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    private bool IsMovementLocked()
    {
        if (GameplayInputLock.IsMovementLocked) return true;
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen) return true;
        if (InvestigationDirector.Instance != null && InvestigationDirector.Instance.IsPlayingFlashback) return true;
        return ScreenFader.IsFading;
    }

    private void ReadMovementInput()
    {
        if (IsMovementLocked()) { moveInput = Vector2.zero; return; }
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        if (!allowDiagonalMovement)
        {
            if (Mathf.Abs(horizontal) > 0f) vertical = 0f;
            else if (Mathf.Abs(vertical) > 0f) horizontal = 0f;
        }
        moveInput = new Vector2(horizontal, vertical).normalized;
        if (moveInput != Vector2.zero) lastMoveDirection = moveInput;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        if (moveInput != Vector2.zero)
        {
            string targetState = ResolveWalkState(moveInput);
            if (!animator.enabled) { animator.enabled = true; currentWalkState = null; }
            if (targetState != currentWalkState) { animator.Play(targetState, 0, 0f); currentWalkState = targetState; }
            else
            {
                bool leavingTarget = animator.IsInTransition(0)
                    ? !animator.GetNextAnimatorStateInfo(0).IsName(targetState)
                    : !animator.GetCurrentAnimatorStateInfo(0).IsName(targetState);
                if (leavingTarget) animator.Play(targetState, 0, 0f);
            }
        }
        else if (animator.enabled)
        {
            animator.enabled = false;
            currentWalkState = null;
            ApplyIdleSprite();
        }
    }

    private string ResolveWalkState(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)) return direction.x < 0f ? walkLeftState : walkRightState;
        return direction.y < 0f ? walkDownState : walkUpState;
    }

    private void UpdateFootsteps()
    {
        if (footstepSource == null || footstepSource.clip == null) return;
        bool moving = moveInput != Vector2.zero;
        if (moving && !footstepSource.isPlaying)
        {
            footstepSource.volume = GetEffectiveFootstepVolume();
            footstepSource.Play();
        }
        else if (!moving && footstepSource.isPlaying) footstepSource.Stop();
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (footstepSource != null) footstepSource.volume = GetEffectiveFootstepVolume();
    }

    private float GetEffectiveFootstepVolume()
    {
        float globalVolume = SettingsManager.Instance == null ? 1f : SettingsManager.Instance.SfxVolume;
        return Mathf.Clamp01(footstepVolume * globalVolume);
    }

    private void OnDestroy()
    {
        if (SettingsManager.Instance != null) SettingsManager.Instance.SfxVolumeChanged -= OnSfxVolumeChanged;
    }

    private void ApplyIdleSprite()
    {
        if (spriteRenderer == null) return;
        Sprite idle;
        if (Mathf.Abs(lastMoveDirection.x) >= Mathf.Abs(lastMoveDirection.y)) idle = lastMoveDirection.x < 0f ? idleLeft : idleRight;
        else idle = lastMoveDirection.y < 0f ? idleDown : idleUp;
        if (idle != null) spriteRenderer.sprite = idle;
    }
}
