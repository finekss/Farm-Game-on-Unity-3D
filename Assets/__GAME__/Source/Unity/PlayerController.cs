using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class Character : MonoBehaviour
{
    public static Character Player;

    #region CachedComponents
    private Rigidbody _rb;
    private Collider _col;
    private CapsuleCollider _capsule;
    private Camera _cam;
    private InputSystem_Actions _input;
    private Animator _animator;
    #endregion

    #region Model
    [FormerlySerializedAs("_model")]
    [Header("Model")]
    [Tooltip("Корневой Transform 3D-модели персонажа (для поворота отдельно от корня)")]
    [SerializeField] private Transform model;
    [SerializeField] private Animator animatorOverride;
    #endregion

    #region MovementSettings
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 720f;
    [Tooltip("Время разгона до полной скорости (сек)")]
    [SerializeField] private float accelerationTime = 0.1f;
    [Tooltip("Время торможения до остановки (сек)")]
    [SerializeField] private float decelerationTime = 0.25f;
    #endregion

    #region JumpSettings
    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;
    [Tooltip("Множитель обрезки вертикальной скорости при раннем отпускании (0.1 = короткий прыжок)")]
    [Range(0.05f, 0.9f)]
    [SerializeField] private float jumpCutMultiplier = 0.35f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float jumpCooldown = 0.8f;
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
    private Vector2 _moveInput;
    private Vector3 _moveDirection;
    private Vector3 _currentVelocityXZ;
    private Vector3 _lastNonZeroDirection = Vector3.forward;

    private bool _isGrounded;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _jumpConsumed;
    private float _jumpCDTimer;

    private bool _isRolling;
    private bool _isJumping;
    private bool _jumpButtonHeld;
    private bool _jumpWasCut;
    private bool _jumpReleased;
    private float _rollCDTimer;
    private float _invulTimer;

    private float _defaultColliderHeight;
    private Vector3 _defaultColliderCenter;
    private Vector3 _defaultGroundCheckLocalPos;

    private bool _prevGroundedFixed;
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
    private AnimDirection _currentDirection = AnimDirection.Idle;
    private bool _wasGrounded;
    private bool _jumpExecuted;
    private float _prevCameraYaw;
    #endregion

    #region Lifecycle
    private void Awake()
    {
        Player = this;
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        _cam = Camera.main;
        _input = new InputSystem_Actions();
        CurrentHealth = maxHealth;
        _rb.freezeRotation = true;
        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _animator = animatorOverride != null ? animatorOverride : GetComponentInChildren<Animator>();
        if (_animator != null) _animator.applyRootMotion = false;

        _capsule = GetComponent<CapsuleCollider>();
        if (_capsule != null)
        {
            _defaultColliderHeight = _capsule.height;
            _defaultColliderCenter = _capsule.center;
        }

        if (groundCheck != null)
            _defaultGroundCheckLocalPos = groundCheck.localPosition;
    }

    private void OnEnable()
    {
        _input.Enable();
        _isRolling = false;
    }

    private void OnDisable() => _input.Disable();

    private void OnDestroy()
    {       
        _input?.Disable();
        _input?.Dispose();
    }

    private void Update()
    {
        if (IsDead) return;
        ReadInput();
        TickTimers();
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
        RotateModel();
    }
    #endregion

    #region Input
    private void ReadInput()
    {
        _moveInput = _input.Player.Move.ReadValue<Vector2>();

        Vector3 camForward = _cam != null ? _cam.transform.forward : transform.forward;
        Vector3 camRight = _cam != null ? _cam.transform.right : transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        _moveDirection = (camForward * _moveInput.y) + (camRight * _moveInput.x);
        if (_moveDirection.sqrMagnitude > 1f) _moveDirection.Normalize();
        if (_moveDirection.sqrMagnitude > 0.01f) _lastNonZeroDirection = _moveDirection.normalized;

        

        if (_input.Player.Jump.triggered)
        {
            _jumpBufferTimer = jumpBufferTime;
            _jumpConsumed = false;
        }

        // Фиксируем отпускание кнопки как событие — сохраняется до потребления в FixedUpdate
        bool prevHeld = _jumpButtonHeld;
        _jumpButtonHeld = _input.Player.Jump.IsPressed();
        if (prevHeld && !_jumpButtonHeld) _jumpReleased = true;

        if (_input.Player.Roll.triggered && CanRoll()) StartCoroutine(RollRoutine());
    }
    #endregion

    #region Timers
    private void TickTimers()
    {
        float dt = Time.deltaTime;
        _invulTimer = Mathf.Max(0f, _invulTimer - dt);
        _rollCDTimer = Mathf.Max(0f, _rollCDTimer - dt);
        _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - dt);
        _jumpCDTimer = Mathf.Max(0f, _jumpCDTimer - dt);

    }
    #endregion

    #region GroundCheck
    private void CheckGround()
    {
        _isGrounded = groundCheck != null && Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        

        if (_isGrounded)
        {
            _coyoteTimer = coyoteTime;
            _isJumping = false;
        }
        else if (_prevGroundedFixed)
        {
            // Только один раз при переходе с земли в воздух — запускаем coyote time
            _coyoteTimer = coyoteTime;
        }
        _coyoteTimer = Mathf.Max(0f, _coyoteTimer - Time.fixedDeltaTime);
        _prevGroundedFixed = _isGrounded;
    }
    #endregion

    #region Movement
    private void ApplyMovement()
    {
        if (_isRolling) return;

        Vector3 targetVelocity = new Vector3(_moveDirection.x * moveSpeed, 0f, _moveDirection.z * moveSpeed);
        bool hasInput = _moveDirection.sqrMagnitude > 0.01f;
        float smoothTime = hasInput ? accelerationTime : decelerationTime;

        _currentVelocityXZ = Vector3.MoveTowards(_currentVelocityXZ, targetVelocity, 
            moveSpeed / Mathf.Max(smoothTime, 0.001f) * Time.fixedDeltaTime);

        _rb.linearVelocity = new Vector3(_currentVelocityXZ.x, _rb.linearVelocity.y, _currentVelocityXZ.z);
    }

    private void RotateModel()
    {
        Transform pivot = model != null ? model : transform;

        Vector3 camForward = _cam.transform.forward;
        camForward.y = 0f;        

        Quaternion target = Quaternion.LookRotation(camForward.normalized, Vector3.up);
        pivot.rotation = Quaternion.Slerp(pivot.rotation, target, rotationSpeed * Time.deltaTime);
        

    }
    #endregion

    #region Jump
    private void ApplyJump()
    {
        bool canJump = (_isGrounded || _coyoteTimer > 0f) && !_isRolling && !_isJumping && _jumpCDTimer <= 0f;
        if (_jumpBufferTimer > 0f && canJump && !_jumpConsumed)
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            _jumpConsumed = true;
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            _isJumping = true;
            _jumpWasCut = false;
            _jumpReleased = false;
            _jumpExecuted = true;
            _jumpCDTimer = jumpCooldown;
        }
    }

    private void ApplyJumpCut()
    {
        if (!_isJumping)
        {
            _jumpReleased = false;
            return;
        }

        if (_jumpReleased && !_jumpWasCut && _rb.linearVelocity.y > 0.01f)
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _rb.linearVelocity.y * jumpCutMultiplier, _rb.linearVelocity.z);
            _jumpWasCut = true;
            _jumpReleased = false;
        }
    }

    private void ApplyGravityModifiers()
    {
        float vy = _rb.linearVelocity.y;

        if (vy < -0.01f)
        {
            // Падение — усиленная гравитация для резкого снижения
            _rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;

            // Ограничиваем скорость падения
            if (_rb.linearVelocity.y < -maxFallSpeed)
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, -maxFallSpeed, _rb.linearVelocity.z);
        }
        else if (Mathf.Abs(vy) < apexThreshold && !_isGrounded)
        {
            // Вершина прыжка — лёгкая гравитация для «зависания»
            _rb.linearVelocity += Vector3.up * Physics.gravity.y * (apexGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    private void UpdateCollider()
    {
        if (_capsule == null) return;

        if (!_isGrounded)
        {
            float targetHeight = _defaultColliderHeight * airColliderHeightMul;
            _capsule.height = targetHeight;
            // Сжимаем снизу вверх — верх коллайдера остаётся на месте
            float yShift = (_defaultColliderHeight - targetHeight) * 0.5f;
            _capsule.center = _defaultColliderCenter + Vector3.up * yShift;

            // Перемещаем groundCheck к нижней точке коллайдера
            if (groundCheck != null)
            {
                float bottomY = _capsule.center.y - _capsule.height * 0.5f;
                groundCheck.localPosition = new Vector3(
                    _defaultGroundCheckLocalPos.x + groundCheckAirOffset.x,
                    bottomY + groundCheckAirOffset.y,
                    _defaultGroundCheckLocalPos.z + groundCheckAirOffset.z
                );
            }
        }
        else
        {
            _capsule.height = _defaultColliderHeight;
            _capsule.center = _defaultColliderCenter;

            if (groundCheck != null)
                groundCheck.localPosition = _defaultGroundCheckLocalPos;
        }
    }
    #endregion

    #region Roll
    private bool CanRoll() => !_isRolling && _rollCDTimer <= 0f;

    private IEnumerator RollRoutine()
    {
        _isRolling = true;
        _invulTimer = rollInvulTime;
        _input.Disable();
        TriggerAnimator(RollTriggerHash);

        Vector3 rollDir = _lastNonZeroDirection;
        float elapsed = 0f;

        // try-finally гарантирует, что input.Enable() и isRolling = false
        // выполнятся, даже если корутина прервана (смерть, деактивация, исключение)
        try
        {
            while (elapsed < rollDuration)
            {
                _rb.linearVelocity = new Vector3(rollDir.x * rollSpeed, _rb.linearVelocity.y, rollDir.z * rollSpeed);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        finally
        {
            _input.Enable();
            _isRolling = false;
            _rollCDTimer = rollCooldown;
        }
    }
    #endregion

    #region Health
    public void TakeDamage(int dmg)
    {
        if (_invulTimer > 0f || IsDead) return;
        CurrentHealth -= dmg;
        _invulTimer = invulOnHitTime;
        TriggerAnimator(HurtTriggerHash);
        if (CurrentHealth <= 0) Die();
    }

    private void Die()
    {
        IsDead = true;
        StopAllCoroutines();  // Завершает RollRoutine → finally-блок сбросит _isRolling и включит _input
        _input.Disable();
        _rb.linearVelocity = Vector3.zero;
        _col.enabled = false;
        SetAnimatorBool(IsDeadHash, true);
        gameObject.SetActive(false);
    }
    #endregion

    #region AnimatorHelpers
    private void UpdateAnimator()
    {
        if (_animator == null) return;

        UpdateDirectionState();
        UpdateCameraRotation();

        _animator.SetBool(IsAirborneHash, !_isGrounded);

        if (_jumpExecuted)
        {
            TriggerAnimator(JumpTriggerHash);
            _jumpExecuted = false;
        }

        if (!_wasGrounded && _isGrounded)
        {
            TriggerAnimator(LandTriggerHash);
        }

        _animator.SetBool(IsGroundedHash, _isGrounded);
        _animator.SetBool(IsRollingHash, _isRolling);
        _animator.SetBool(IsDeadHash, IsDead);

        _wasGrounded = _isGrounded;
    }

    private void UpdateCameraRotation()
    {
        if (_cam == null) return;
        float currentYaw = _cam.transform.eulerAngles.y;
        float delta = Mathf.DeltaAngle(_prevCameraYaw, currentYaw);
        _animator.SetFloat(CameraRotationHash, delta / Mathf.Max(Time.deltaTime, 0.0001f));
        _prevCameraYaw = currentYaw;
    }

    private void UpdateDirectionState()
    {
        if (_moveInput.sqrMagnitude < 0.1f)
        {
            _currentDirection = AnimDirection.Idle;
        }
        else
        {
            float angle = Mathf.Atan2(_moveInput.x, _moveInput.y) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            if (angle >= 337.5f || angle < 22.5f) _currentDirection = AnimDirection.Forward;
            else if (angle >= 22.5f && angle < 67.5f) _currentDirection = AnimDirection.ForwardRight;
            else if (angle >= 67.5f && angle < 112.5f) _currentDirection = AnimDirection.Right;
            else if (angle >= 112.5f && angle < 157.5f) _currentDirection = AnimDirection.BackRight;
            else if (angle >= 157.5f && angle < 202.5f) _currentDirection = AnimDirection.Back;
            else if (angle >= 202.5f && angle < 247.5f) _currentDirection = AnimDirection.BackLeft;
            else if (angle >= 247.5f && angle < 292.5f) _currentDirection = AnimDirection.Left;
            else if (angle >= 292.5f && angle < 337.5f) _currentDirection = AnimDirection.ForwardLeft;
        }

        _animator.SetInteger(DirectionHash, (int)_currentDirection);
    }

    private void TriggerAnimator(int triggerHash)
    {
        if (_animator == null) return;
        _animator.ResetTrigger(triggerHash);
        _animator.SetTrigger(triggerHash);
    }

    private void SetAnimatorBool(int boolHash, bool value)
    {
        if (_animator == null) return;
        _animator.SetBool(boolHash, value);
    }
    #endregion

    #region Gizmos
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
#endif
    #endregion
}
