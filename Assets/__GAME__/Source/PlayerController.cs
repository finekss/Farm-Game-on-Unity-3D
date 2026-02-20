using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Character : MonoBehaviour
{
    public static Character player;

    #region CachedComponents
    private Rigidbody rb;
    private Collider col;
    private CapsuleCollider capsule;
    private Camera cam;
    private InputSystem_Actions input;
    private Animator animator;
    #endregion

    #region Model
    [Header("Model")]
    [Tooltip("Корневой Transform 3D-модели персонажа (для поворота отдельно от корня)")]
    [SerializeField] private Transform model;
    [SerializeField] private Animator animatorOverride;
    #endregion

    #region MovementSettings
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 720f;
    [Tooltip("Время разгона до полной скорости (сек)")]
    [SerializeField] private float accelerationTime = 0.1f;
    [Tooltip("Время торможения до остановки (сек)")]
    [SerializeField] private float decelerationTime = 0.25f;
    #endregion

    #region JumpSettings
    [Header("Jump")]
    [SerializeField] private float jumpForce = 12f;
    [Tooltip("Множитель обрезки вертикальной скорости при раннем отпускании (0.1 = короткий прыжок, 0.5 = средний)")]
    [Range(0.05f, 0.9f)]
    [SerializeField] private float jumpCutMultiplier = 0.35f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Gravity")]
    [Tooltip("Множитель гравитации при падении (>1 = резче падение)")]
    [SerializeField] private float fallGravityMultiplier = 2.5f;
    [Tooltip("Множитель гравитации на вершине прыжка (делает дугу плавнее)")]
    [SerializeField] private float apexGravityMultiplier = 1.2f;
    [Tooltip("Порог скорости Y для зоны вершины прыжка")]
    [SerializeField] private float apexThreshold = 1.5f;
    [Tooltip("Максимальная скорость падения")]
    [SerializeField] private float maxFallSpeed = 25f;

    [Header("Collider In Air")]
    [Tooltip("Множитель высоты коллайдера в прыжке (0.7 = сжимается на 30%)")]
    [Range(0.3f, 1f)]
    [SerializeField] private float airColliderHeightMul = 0.7f;
    [Tooltip("Дополнительное смещение groundCheck относительно дна коллайдера в прыжке")]
    [SerializeField] private Vector3 groundCheckAirOffset = new Vector3(0f, -0.05f, 0f);
    #endregion

    #region RollSettings
    [Header("Roll")]
    [SerializeField] private float rollSpeed = 14f;
    [SerializeField] private float rollDuration = 0.35f;
    [SerializeField] private float rollCooldown = 0.8f;
    [SerializeField] private float rollInvulTime = 0.25f;
    #endregion

    #region HealthSettings
    [Header("Health")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private float invulOnHitTime = 0.5f;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    #endregion

    #region RuntimeState
    private Vector2 moveInput;
    private Vector3 moveDirection;
    private Vector3 currentVelocityXZ;
    private Vector3 lastNonZeroDirection = Vector3.forward;

    private bool isGrounded;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool jumpConsumed;

    private bool isRolling;
    private bool isJumping;
    private bool jumpButtonHeld;
    private bool jumpWasCut;
    private bool jumpReleased;
    private float rollCDTimer;
    private float invulTimer;

    private float defaultColliderHeight;
    private Vector3 defaultColliderCenter;
    private Vector3 defaultGroundCheckLocalPos;
    #endregion

    #region Animator
    private static readonly int DirectionHash = Animator.StringToHash("Direction");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsRollingHash = Animator.StringToHash("IsRolling");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int IsAirborneHash = Animator.StringToHash("IsAirborne");
    private static readonly int CameraRotationHash = Animator.StringToHash("CameraRotation");
    private static readonly int JumpTriggerHash = Animator.StringToHash("Jump");
    private static readonly int RollTriggerHash = Animator.StringToHash("Roll");
    private static readonly int HurtTriggerHash = Animator.StringToHash("Hurt");
    private static readonly int LandTriggerHash = Animator.StringToHash("Land");

    private enum AnimDirection { Idle = 0, Forward = 1, Back = 2, Left = 3, Right = 4, 
                                  ForwardLeft = 5, ForwardRight = 6, BackLeft = 7, BackRight = 8 }
    private AnimDirection currentDirection = AnimDirection.Idle;
    private bool wasGrounded;
    private bool jumpExecuted;
    private float prevCameraYaw;
    #endregion

    #region Lifecycle
    private void Awake()
    {
        player = this;
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        cam = Camera.main;
        input = new InputSystem_Actions();
        CurrentHealth = maxHealth;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        animator = animatorOverride != null ? animatorOverride : GetComponentInChildren<Animator>();

        capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            defaultColliderHeight = capsule.height;
            defaultColliderCenter = capsule.center;
        }

        if (groundCheck != null)
            defaultGroundCheckLocalPos = groundCheck.localPosition;
    }

    private void OnEnable() => input.Enable();
    private void OnDisable() => input.Disable();

    private void Update()
    {
        if (IsDead) return;
        ReadInput();
        TickTimers();
        RotateModel();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (IsDead) return;
        CheckGround();
        ApplyMovement();
        ApplyJump();
        ApplyJumpCut();
        ApplyGravityModifiers();
        UpdateCollider();
    }
    #endregion

    #region Input
    private void ReadInput()
    {
        moveInput = input.Player.Move.ReadValue<Vector2>();

        Vector3 camForward = cam != null ? cam.transform.forward : transform.forward;
        Vector3 camRight = cam != null ? cam.transform.right : transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        moveDirection = (camForward * moveInput.y) + (camRight * moveInput.x);
        if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();
        if (moveDirection.sqrMagnitude > 0.01f) lastNonZeroDirection = moveDirection.normalized;

        if (input.Player.Jump.triggered)
        {
            jumpBufferTimer = jumpBufferTime;
            jumpConsumed = false;
        }

        // Фиксируем отпускание кнопки как событие — сохраняется до потребления в FixedUpdate
        bool prevHeld = jumpButtonHeld;
        jumpButtonHeld = input.Player.Jump.IsPressed();
        if (prevHeld && !jumpButtonHeld) jumpReleased = true;

        if (input.Player.Roll.triggered && CanRoll()) StartCoroutine(RollRoutine());
    }
    #endregion

    #region Timers
    private void TickTimers()
    {
        float dt = Time.deltaTime;
        invulTimer = Mathf.Max(0f, invulTimer - dt);
        rollCDTimer = Mathf.Max(0f, rollCDTimer - dt);
        jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - dt);
    }
    #endregion

    #region GroundCheck
    private void CheckGround()
    {
        isGrounded = groundCheck != null && Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            isJumping = false;
        }
        else if (wasGrounded)
        {
            coyoteTimer = coyoteTime;
        }

        coyoteTimer = Mathf.Max(0f, coyoteTimer - Time.fixedDeltaTime);
    }
    #endregion

    #region Movement
    private void ApplyMovement()
    {
        if (isRolling) return;

        Vector3 targetVelocity = new Vector3(moveDirection.x * moveSpeed, 0f, moveDirection.z * moveSpeed);
        bool hasInput = moveDirection.sqrMagnitude > 0.01f;
        float smoothTime = hasInput ? accelerationTime : decelerationTime;

        currentVelocityXZ = Vector3.MoveTowards(currentVelocityXZ, targetVelocity, 
            moveSpeed / Mathf.Max(smoothTime, 0.001f) * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(currentVelocityXZ.x, rb.linearVelocity.y, currentVelocityXZ.z);
    }

    private void RotateModel()
    {
        if (isRolling || moveDirection.sqrMagnitude < 0.01f) return;
        Quaternion target = Quaternion.LookRotation(lastNonZeroDirection, Vector3.up);
        Transform pivot = model != null ? model : transform;
        pivot.rotation = Quaternion.RotateTowards(pivot.rotation, target, rotationSpeed * Time.deltaTime);
    }
    #endregion

    #region Jump
    private void ApplyJump()
    {
        bool canJump = (isGrounded || coyoteTimer > 0f) && !isRolling && !isJumping;
        if (jumpBufferTimer > 0f && canJump && !jumpConsumed)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpConsumed = true;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            isJumping = true;
            jumpWasCut = false;
            jumpReleased = false;
            jumpExecuted = true;
        }
    }

    private void ApplyJumpCut()
    {
        if (!isJumping)
        {
            jumpReleased = false;
            return;
        }

        if (jumpReleased && !jumpWasCut && rb.linearVelocity.y > 0.01f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier, rb.linearVelocity.z);
            jumpWasCut = true;
            jumpReleased = false;
        }
    }

    private void ApplyGravityModifiers()
    {
        float vy = rb.linearVelocity.y;

        if (vy < -0.01f)
        {
            // Падение — усиленная гравитация для резкого снижения
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;

            // Ограничиваем скорость падения
            if (rb.linearVelocity.y < -maxFallSpeed)
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -maxFallSpeed, rb.linearVelocity.z);
        }
        else if (Mathf.Abs(vy) < apexThreshold && !isGrounded)
        {
            // Вершина прыжка — лёгкая гравитация для «зависания»
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (apexGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    private void UpdateCollider()
    {
        if (capsule == null) return;

        if (!isGrounded)
        {
            float targetHeight = defaultColliderHeight * airColliderHeightMul;
            capsule.height = targetHeight;
            // Сжимаем снизу вверх — верх коллайдера остаётся на месте
            float yShift = (defaultColliderHeight - targetHeight) * 0.5f;
            capsule.center = defaultColliderCenter + Vector3.up * yShift;

            // Перемещаем groundCheck к нижней точке коллайдера
            if (groundCheck != null)
            {
                float bottomY = capsule.center.y - capsule.height * 0.5f;
                groundCheck.localPosition = new Vector3(
                    defaultGroundCheckLocalPos.x + groundCheckAirOffset.x,
                    bottomY + groundCheckAirOffset.y,
                    defaultGroundCheckLocalPos.z + groundCheckAirOffset.z
                );
            }
        }
        else
        {
            capsule.height = defaultColliderHeight;
            capsule.center = defaultColliderCenter;

            if (groundCheck != null)
                groundCheck.localPosition = defaultGroundCheckLocalPos;
        }
    }
    #endregion

    #region Roll
    private bool CanRoll() => !isRolling && rollCDTimer <= 0f;

    private IEnumerator RollRoutine()
    {
        isRolling = true;
        invulTimer = rollInvulTime;
        input.Disable();
        TriggerAnimator(RollTriggerHash);

        Vector3 rollDir = lastNonZeroDirection;
        float elapsed = 0f;
        while (elapsed < rollDuration)
        {
            rb.linearVelocity = new Vector3(rollDir.x * rollSpeed, rb.linearVelocity.y, rollDir.z * rollSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        input.Enable();
        isRolling = false;
        rollCDTimer = rollCooldown;
    }
    #endregion

    #region Health
    public void TakeDamage(int dmg)
    {
        if (invulTimer > 0f || IsDead) return;
        CurrentHealth -= dmg;
        invulTimer = invulOnHitTime;
        TriggerAnimator(HurtTriggerHash);
        if (CurrentHealth <= 0) Die();
    }

    private void Die()
    {
        IsDead = true;
        input.Disable();
        rb.linearVelocity = Vector3.zero;
        col.enabled = false;
        SetAnimatorBool(IsDeadHash, true);
        gameObject.SetActive(false);
    }
    #endregion

    #region AnimatorHelpers
    private void UpdateAnimator()
    {
        if (animator == null) return;

        UpdateDirectionState();
        UpdateCameraRotation();

        animator.SetBool(IsAirborneHash, !isGrounded);

        if (jumpExecuted)
        {
            TriggerAnimator(JumpTriggerHash);
            jumpExecuted = false;
        }

        if (!wasGrounded && isGrounded)
        {
            TriggerAnimator(LandTriggerHash);
        }

        animator.SetBool(IsGroundedHash, isGrounded);
        animator.SetBool(IsRollingHash, isRolling);
        animator.SetBool(IsDeadHash, IsDead);

        wasGrounded = isGrounded;
    }

    private void UpdateCameraRotation()
    {
        if (cam == null) return;
        float currentYaw = cam.transform.eulerAngles.y;
        float delta = Mathf.DeltaAngle(prevCameraYaw, currentYaw);
        animator.SetFloat(CameraRotationHash, delta / Mathf.Max(Time.deltaTime, 0.0001f));
        prevCameraYaw = currentYaw;
    }

    private void UpdateDirectionState()
    {
        if (moveInput.sqrMagnitude < 0.1f)
        {
            currentDirection = AnimDirection.Idle;
        }
        else
        {
            float angle = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            if (angle >= 337.5f || angle < 22.5f) currentDirection = AnimDirection.Forward;
            else if (angle >= 22.5f && angle < 67.5f) currentDirection = AnimDirection.ForwardRight;
            else if (angle >= 67.5f && angle < 112.5f) currentDirection = AnimDirection.Right;
            else if (angle >= 112.5f && angle < 157.5f) currentDirection = AnimDirection.BackRight;
            else if (angle >= 157.5f && angle < 202.5f) currentDirection = AnimDirection.Back;
            else if (angle >= 202.5f && angle < 247.5f) currentDirection = AnimDirection.BackLeft;
            else if (angle >= 247.5f && angle < 292.5f) currentDirection = AnimDirection.Left;
            else if (angle >= 292.5f && angle < 337.5f) currentDirection = AnimDirection.ForwardLeft;
        }

        animator.SetInteger(DirectionHash, (int)currentDirection);
    }

    private void TriggerAnimator(int triggerHash)
    {
        if (animator == null) return;
        animator.ResetTrigger(triggerHash);
        animator.SetTrigger(triggerHash);
    }

    private void SetAnimatorBool(int boolHash, bool value)
    {
        if (animator == null) return;
        animator.SetBool(boolHash, value);
    }
    #endregion

    #region Gizmos
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
#endif
    #endregion
}
