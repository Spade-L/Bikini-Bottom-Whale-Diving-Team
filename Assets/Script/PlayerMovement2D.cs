using System.Collections;
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
    private bool endingMovementActive;
    private Vector2 endingTarget;
    private float endingSpeed;
    private float endingTolerance;
    private bool endingMovementFinished;
    private Collider2D[] endingMovementColliders;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        endingMovementColliders = GetComponentsInChildren<Collider2D>(true);
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
        if (endingMovementActive)
        {
            return;
        }

        ReadMovementInput();
        UpdateAnimator();
        UpdateFootsteps();
    }

    private void FixedUpdate()
    {
        if (endingMovementActive)
        {
            Vector2 next = Vector2.MoveTowards(rb.position, endingTarget, endingSpeed * Time.fixedDeltaTime);
            rb.MovePosition(next);
            if (Vector2.Distance(next, endingTarget) <= endingTolerance)
            {
                rb.MovePosition(endingTarget);
                endingMovementActive = false;
                endingMovementFinished = true;
                moveInput = Vector2.zero;
                StopEndingMovement();
            }
            return;
        }

        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    /// <summary>按指定朝向缓慢走到结局演出目标，不读取玩家输入。</summary>
    public IEnumerator MoveToEndingTarget(Vector3 target, Vector2 direction, float speedMultiplier = 0.5f, float timeout = 8f, float tolerance = 0.03f)
    {
        SetEndingCollisionsEnabled(false);
        direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.down;
        lastMoveDirection = direction;
        endingTarget = target;
        if (animator != null) animator.speed = Mathf.Max(0.01f, speedMultiplier);
        endingSpeed = Mathf.Max(0.01f, moveSpeed * speedMultiplier);
        endingTolerance = Mathf.Max(0.001f, tolerance);
        endingMovementFinished = false;
        endingMovementActive = true;
        moveInput = direction;
        PlayWalkState(direction);

        float elapsed = 0f;
        while (!endingMovementFinished && elapsed < Mathf.Max(0.1f, timeout))
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (endingMovementActive)
        {
            endingMovementActive = false;
            moveInput = Vector2.zero;
            StopEndingMovement();
        }
    }

    public void SetEndingIdleLeft()
    {
        lastMoveDirection = Vector2.left;
        moveInput = Vector2.zero;
        StopEndingMovement();
    }

    public void SetEndingSprite(Sprite sprite)
    {
        StopEndingMovement();
        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = sprite;
        }
    }

    private void StopEndingMovement()
    {
        if (footstepSource != null && footstepSource.isPlaying) footstepSource.Stop();
        if (animator != null)
        {
            animator.speed = 1f;
            animator.enabled = false;
            currentWalkState = null;
        }
        ApplyIdleSprite();
        SetEndingCollisionsEnabled(true);
    }

    private void SetEndingCollisionsEnabled(bool enabled)
    {
        if (endingMovementColliders == null) return;
        foreach (Collider2D collider in endingMovementColliders)
        {
            if (collider != null) collider.enabled = enabled;
        }
    }

    private void PlayWalkState(Vector2 direction)
    {
        if (animator == null) return;
        string state = ResolveWalkState(direction);
        if (!animator.enabled) { animator.enabled = true; currentWalkState = null; }
        animator.Play(state, 0, 0f);
        currentWalkState = state;
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
        if (moving && !footstepSource.isPlaying) { footstepSource.volume = GetEffectiveFootstepVolume(); footstepSource.Play(); }
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
