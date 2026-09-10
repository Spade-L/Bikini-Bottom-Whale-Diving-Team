using System.Collections;
using UnityEngine;

// 调用 RequireComponent
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
// 配置 移动设置 分组
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 4f;
// 记录 allowDiagonalMovement 状态
    [SerializeField] private bool allowDiagonalMovement = false;
    [Header("动画设置（可选）")]
// 保存 animator 数据
    [SerializeField] private Animator animator;
    [SerializeField] private string walkDownState = "前进";
// 保存 walkUpState 数据
    [SerializeField] private string walkUpState = "背身";
    [SerializeField] private string walkLeftState = "左走";
// 保存 walkRightState 数据
    [SerializeField] private string walkRightState = "右走";
    [Header("待机静止帧（停下时按最后朝向显示）")]
// 保存 idleDown 数据
    [SerializeField] private Sprite idleDown;
    [SerializeField] private Sprite idleUp;
// 保存 idleLeft 数据
    [SerializeField] private Sprite idleLeft;
    [SerializeField] private Sprite idleRight;
// 配置 行走脚步声（循环） 分组
    [Header("行走脚步声（循环）")]
    [SerializeField] private AudioClip footstepLoop;
// 限制当前数值范围
    [Range(0f, 1f)] [SerializeField] private float footstepVolume = 0.5f;

// 保存 rb 数据
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
// 保存 footstepSource 数据
    private AudioSource footstepSource;
    private Vector2 moveInput;
// 保存 lastMoveDirection 数据
    private Vector2 lastMoveDirection = Vector2.down;
    private string currentWalkState;
// 记录 endingMovementActive 状态
    private bool endingMovementActive;
    private Vector2 endingTarget;
// 配置 endingSpeed 数值
    private float endingSpeed;
    private float endingTolerance;
// 记录 endingMovementFinished 状态
    private bool endingMovementFinished;
    private Collider2D[] endingMovementColliders;

// 定义 Awake 方法
    private void Awake()
    {
// 更新当前状态
        rb = GetComponent<Rigidbody2D>();
        endingMovementColliders = GetComponentsInChildren<Collider2D>(true);
// 更新当前状态
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
// 更新当前状态
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
// 更新当前状态
        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.clip = footstepLoop;
// 更新当前状态
        footstepSource.loop = true;
        footstepSource.playOnAwake = false;
// 更新当前状态
        footstepSource.spatialBlend = 0f;
        footstepSource.volume = GetEffectiveFootstepVolume();
    }

// 定义 Start 方法
    private void Start()
    {
// 判断当前条件
        if (SettingsManager.Instance != null) SettingsManager.Instance.SfxVolumeChanged += OnSfxVolumeChanged;
        if (animator != null) animator.enabled = false;
// 执行 ApplyIdleSprite
        ApplyIdleSprite();
    }

// 定义 Update 方法
    private void Update()
    {
// 判断当前条件
        if (endingMovementActive)
        {
// 返回当前结果
            return;
        }

// 执行 ReadMovementInput
        ReadMovementInput();
        UpdateAnimator();
// 执行 UpdateFootsteps
        UpdateFootsteps();
    }

// 定义 FixedUpdate 方法
    private void FixedUpdate()
    {
// 判断当前条件
        if (endingMovementActive)
        {
// 定义 MoveTowards 方法
            Vector2 next = Vector2.MoveTowards(rb.position, endingTarget, endingSpeed * Time.fixedDeltaTime);
            rb.MovePosition(next);
// 判断当前条件
            if (Vector2.Distance(next, endingTarget) <= endingTolerance)
            {
// 调用 MovePosition
                rb.MovePosition(endingTarget);
                endingMovementActive = false;
// 更新当前状态
                endingMovementFinished = true;
                moveInput = Vector2.zero;
// 执行 StopEndingMovement
                StopEndingMovement();
            }
// 返回当前结果
            return;
        }

// 调用 MovePosition
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    /// <summary>按指定朝向缓慢走到结局演出目标，不读取玩家输入。</summary>
    public IEnumerator MoveToEndingTarget(Vector3 target, Vector2 direction, float speedMultiplier = 0.5f, float timeout = 8f, float tolerance = 0.03f)
    {
// 执行 SetEndingCollisionsEnabled
        SetEndingCollisionsEnabled(false);
        direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.down;
// 更新当前状态
        lastMoveDirection = direction;
        endingTarget = target;
// 判断当前条件
        if (animator != null) animator.speed = Mathf.Max(0.01f, speedMultiplier);
        endingSpeed = Mathf.Max(0.01f, moveSpeed * speedMultiplier);
// 更新当前状态
        endingTolerance = Mathf.Max(0.001f, tolerance);
        endingMovementFinished = false;
// 更新当前状态
        endingMovementActive = true;
        moveInput = direction;
// 执行 PlayWalkState
        PlayWalkState(direction);

// 配置 elapsed 数值
        float elapsed = 0f;
        while (!endingMovementFinished && elapsed < Mathf.Max(0.1f, timeout))
        {
// 更新当前逻辑
            elapsed += Time.deltaTime;
            yield return null;
        }

// 判断当前条件
        if (endingMovementActive)
        {
// 更新当前状态
            endingMovementActive = false;
            moveInput = Vector2.zero;
// 执行 StopEndingMovement
            StopEndingMovement();
        }
    }

// 定义 SetEndingIdleLeft 方法
    public void SetEndingIdleLeft()
    {
// 更新当前状态
        lastMoveDirection = Vector2.left;
        moveInput = Vector2.zero;
// 执行 StopEndingMovement
        StopEndingMovement();
    }

// 定义 SetEndingSprite 方法
    public void SetEndingSprite(Sprite sprite)
    {
// 执行 StopEndingMovement
        StopEndingMovement();
        if (spriteRenderer != null && sprite != null)
        {
// 更新启用状态
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = sprite;
        }
    }

// 定义 StopEndingMovement 方法
    private void StopEndingMovement()
    {
// 判断当前条件
        if (footstepSource != null && footstepSource.isPlaying) footstepSource.Stop();
        if (animator != null)
        {
// 更新当前状态
            animator.speed = 1f;
            animator.enabled = false;
// 更新当前状态
            currentWalkState = null;
        }
// 执行 ApplyIdleSprite
        ApplyIdleSprite();
        SetEndingCollisionsEnabled(true);
    }

// 定义 SetEndingCollisionsEnabled 方法
    private void SetEndingCollisionsEnabled(bool enabled)
    {
// 空引用时直接退出
        if (endingMovementColliders == null) return;
        foreach (Collider2D collider in endingMovementColliders)
        {
// 判断当前条件
            if (collider != null) collider.enabled = enabled;
        }
    }

// 定义 PlayWalkState 方法
    private void PlayWalkState(Vector2 direction)
    {
// 空引用时直接退出
        if (animator == null) return;
        string state = ResolveWalkState(direction);
// 判断当前条件
        if (!animator.enabled) { animator.enabled = true; currentWalkState = null; }
        animator.Play(state, 0, 0f);
// 更新当前状态
        currentWalkState = state;
    }

// 定义 IsMovementLocked 方法
    private bool IsMovementLocked()
    {
// 判断当前条件
        if (GameplayInputLock.IsMovementLocked) return true;
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen) return true;
// 判断当前条件
        if (InvestigationDirector.Instance != null && InvestigationDirector.Instance.IsPlayingFlashback) return true;
        return ScreenFader.IsFading;
    }

// 定义 ReadMovementInput 方法
    private void ReadMovementInput()
    {
// 判断当前条件
        if (IsMovementLocked()) { moveInput = Vector2.zero; return; }
        float horizontal = Input.GetAxisRaw("Horizontal");
// 定义 GetAxisRaw 方法
        float vertical = Input.GetAxisRaw("Vertical");
        if (!allowDiagonalMovement)
        {
// 判断当前条件
            if (Mathf.Abs(horizontal) > 0f) vertical = 0f;
            else if (Mathf.Abs(vertical) > 0f) horizontal = 0f;
        }
// 更新当前状态
        moveInput = new Vector2(horizontal, vertical).normalized;
        if (moveInput != Vector2.zero) lastMoveDirection = moveInput;
    }

// 定义 UpdateAnimator 方法
    private void UpdateAnimator()
    {
// 空引用时直接退出
        if (animator == null) return;
        if (moveInput != Vector2.zero)
        {
// 定义 ResolveWalkState 方法
            string targetState = ResolveWalkState(moveInput);
            if (!animator.enabled) { animator.enabled = true; currentWalkState = null; }
// 判断当前条件
            if (targetState != currentWalkState) { animator.Play(targetState, 0, 0f); currentWalkState = targetState; }
            else
            {
// 定义 IsInTransition 方法
                bool leavingTarget = animator.IsInTransition(0)
                    ? !animator.GetNextAnimatorStateInfo(0).IsName(targetState)
// 调用 GetCurrentAnimatorStateInfo
                    : !animator.GetCurrentAnimatorStateInfo(0).IsName(targetState);
                if (leavingTarget) animator.Play(targetState, 0, 0f);
            }
        }
// 检查其他条件
        else if (animator.enabled)
        {
// 更新启用状态
            animator.enabled = false;
            currentWalkState = null;
// 执行 ApplyIdleSprite
            ApplyIdleSprite();
        }
    }

// 定义 ResolveWalkState 方法
    private string ResolveWalkState(Vector2 direction)
    {
// 判断当前条件
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)) return direction.x < 0f ? walkLeftState : walkRightState;
        return direction.y < 0f ? walkDownState : walkUpState;
    }

// 定义 UpdateFootsteps 方法
    private void UpdateFootsteps()
    {
// 空引用时直接退出
        if (footstepSource == null || footstepSource.clip == null) return;
        bool moving = moveInput != Vector2.zero;
// 判断当前条件
        if (moving && !footstepSource.isPlaying) { footstepSource.volume = GetEffectiveFootstepVolume(); footstepSource.Play(); }
        else if (!moving && footstepSource.isPlaying) footstepSource.Stop();
    }

// 定义 OnSfxVolumeChanged 方法
    private void OnSfxVolumeChanged(float value)
    {
// 判断当前条件
        if (footstepSource != null) footstepSource.volume = GetEffectiveFootstepVolume();
    }

// 定义 GetEffectiveFootstepVolume 方法
    private float GetEffectiveFootstepVolume()
    {
// 配置 globalVolume 数值
        float globalVolume = SettingsManager.Instance == null ? 1f : SettingsManager.Instance.SfxVolume;
        return Mathf.Clamp01(footstepVolume * globalVolume);
    }

// 定义 OnDestroy 方法
    private void OnDestroy()
    {
// 判断当前条件
        if (SettingsManager.Instance != null) SettingsManager.Instance.SfxVolumeChanged -= OnSfxVolumeChanged;
    }

// 定义 ApplyIdleSprite 方法
    private void ApplyIdleSprite()
    {
// 空引用时直接退出
        if (spriteRenderer == null) return;
        Sprite idle;
// 判断当前条件
        if (Mathf.Abs(lastMoveDirection.x) >= Mathf.Abs(lastMoveDirection.y)) idle = lastMoveDirection.x < 0f ? idleLeft : idleRight;
        else idle = lastMoveDirection.y < 0f ? idleDown : idleUp;
// 判断当前条件
        if (idle != null) spriteRenderer.sprite = idle;
    }
}
