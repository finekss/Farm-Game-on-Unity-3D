using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/// Главная логика поведения игрока.
/// Хранит состояние, рассчитывает движение, применяет гравитацию,
/// управляет прыжком, подкатом, здоровьем и анимациями.
/// Получает команды от PlayerInputHandler, передаёт движение в CharacterController.

public class PlayerController : MonoBehaviour
{
    public static PlayerController Player;

    #region References

    private CharacterController _motor;
    private PlayerInputHandler _inputHandler;
    private PlayerHealth _health;
    private Camera _cam;
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
    [Tooltip("Множитель обрезки вертикальной скорости при раннем отпускании")]
    [Range(0.05f, 0.9f)]
    [SerializeField] private float jumpCutMultiplier = 0.35f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float jumpCooldown = 0.8f;

    #endregion

    #region GravitySettings

    [Header("Gravity")]
    [Tooltip("Множитель гравитации при падении (>1 = резче падение)")]
    [SerializeField] private float fallGravityMultiplier = 2.5f;
    [Tooltip("Множитель гравитации на вершине прыжка (делает дугу плавнее)")]
    [SerializeField] private float apexGravityMultiplier = 1.2f;
    [Tooltip("Порог скорости Y для зоны вершины прыжка")]
    [SerializeField] private float apexThreshold = 1.5f;
    [Tooltip("Максимальная скорость падения")]
    [SerializeField] private float maxFallSpeed = 25f;

    #endregion

    #region RollSettings

    [Header("Roll")]
    [SerializeField] private float rollSpeed = 14f;
    [SerializeField] private float rollDuration = 0.35f;
    [SerializeField] private float rollCooldown = 0.8f;
    [SerializeField] private float rollInvulTime = 0.25f;

    #endregion

    #region Health

    public int CurrentHealth => _health != null ? _health.CurrentHealth : 0;
    public bool IsDead => _health != null && _health.IsDead;

    #endregion

    #region RuntimeState

    private Vector3 _currentVelocityXZ;

    private bool _isGrounded;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _jumpConsumed;
    private float _jumpCdTimer;

    private bool _isRolling;
    private bool _isJumping;
    private bool _jumpWasCut;
    private bool _jumpReleased;
    private float _rollCdTimer;

    private bool _prevGroundedFixed;

    #endregion

    #region AnimatorHashes

    private static readonly int DirectionHash      = Animator.StringToHash("Direction");
    private static readonly int IsGroundedHash      = Animator.StringToHash("IsGrounded");
    private static readonly int IsRollingHash       = Animator.StringToHash("IsRolling");
    private static readonly int IsDeadHash          = Animator.StringToHash("IsDead");
    private static readonly int IsAirborneHash      = Animator.StringToHash("IsAirborne");
    private static readonly int CameraRotationHash  = Animator.StringToHash("CameraRotation");
    private static readonly int JumpTriggerHash     = Animator.StringToHash("Jump");
    private static readonly int RollTriggerHash     = Animator.StringToHash("Roll");
    private static readonly int HurtTriggerHash     = Animator.StringToHash("Hurt");
    private static readonly int LandTriggerHash     = Animator.StringToHash("Land");

    private enum AnimDirection
    {
        Idle = 0, Forward = 1, Back = 2, Left = 3, Right = 4,
        ForwardLeft = 5, ForwardRight = 6, BackLeft = 7, BackRight = 8
    }

    private AnimDirection _currentDirection = AnimDirection.Idle;
    private bool _wasGrounded;
    private bool _jumpExecuted;
    private float _prevCameraYaw;

    #endregion

    #region Lifecycle

    private void Awake()
    {
        Player = this;

        _motor        = GetComponent<CharacterController>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        _health       = GetComponent<PlayerHealth>();
        _cam          = Camera.main;

        _motor.Initialize();
        _inputHandler.Initialize(_cam);
        _health.Initialize();

        _health.OnDamaged += OnDamaged;
        _health.OnDied    += OnDied;
        _animator = animatorOverride != null ? animatorOverride : GetComponentInChildren<Animator>();
        if (_animator != null) _animator.applyRootMotion = false;
    }

    private void OnEnable()
    {
        _inputHandler.Enable();
        _isRolling = false;
    }

    private void OnDisable() => _inputHandler.Disable();

    private void OnDestroy()
    {
        _inputHandler.DisposeInput();

        if (_health != null)
        {
            _health.OnDamaged -= OnDamaged;
            _health.OnDied    -= OnDied;
        }
    }

    private void Update()
    {
        if (IsDead) return;
        _inputHandler.ReadInput();
        ProcessInputCommands();
        TickTimers();
    }

    private void FixedUpdate()
    {
        if (IsDead) return;
        CheckGround();
        ApplyMovement();
        ApplyJump();
        ApplyJumpCut();
        ApplyGravityModifiers();
        _motor.UpdateCollider(!_isGrounded);
        RotateModel();
        UpdateAnimator();
    }

    #endregion

    #region InputProcessing

    private void ProcessInputCommands()
    {
        // Буфер прыжка — запоминаем нажатие для применения в FixedUpdate
        if (_inputHandler.JumpPressed)
        {
            _jumpBufferTimer = jumpBufferTime;
            _jumpConsumed = false;
        }

        // Фиксируем отпускание кнопки как событие — сохраняется до потребления в FixedUpdate
        if (_inputHandler.JumpReleased)
            _jumpReleased = true;

        if (_inputHandler.RollPressed && CanRoll())
            StartCoroutine(RollRoutine());
    }

    #endregion

    #region Timers

    private void TickTimers()
    {
        float dt = Time.deltaTime;
        _rollCdTimer     = Mathf.Max(0f, _rollCdTimer     - dt);
        _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - dt);
        _jumpCdTimer     = Mathf.Max(0f, _jumpCdTimer     - dt);
    }

    #endregion

    #region GroundCheck

    private void CheckGround()
    {
        _motor.UpdateGroundState();
        _isGrounded = _motor.IsGrounded;

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

        Vector3 moveDir = _inputHandler.MoveDirection;
        Vector3 targetVelocity = new Vector3(moveDir.x * moveSpeed, 0f, moveDir.z * moveSpeed);
        bool hasInput = moveDir.sqrMagnitude > 0.01f;
        float smoothTime = hasInput ? accelerationTime : decelerationTime;

        _currentVelocityXZ = Vector3.MoveTowards(
            _currentVelocityXZ, targetVelocity,
            moveSpeed / Mathf.Max(smoothTime, 0.001f) * Time.fixedDeltaTime);

        _motor.SetHorizontalVelocity(_currentVelocityXZ);
    }

    private void RotateModel()
    {
        Transform pivot = model != null ? model : transform;

        Vector3 camForward = _cam.transform.forward;
        camForward.y = 0f;

        Quaternion target = Quaternion.LookRotation(camForward.normalized, Vector3.up);
        pivot.rotation = Quaternion.Slerp(pivot.rotation, target, rotationSpeed * Time.fixedDeltaTime);
    }

    #endregion

    #region Jump

    private void ApplyJump()
    {
        bool canJump = (_isGrounded || _coyoteTimer > 0f) && !_isRolling && !_isJumping && _jumpCdTimer <= 0f;
        if (_jumpBufferTimer > 0f && canJump && !_jumpConsumed)
        {
            _motor.AddVerticalImpulse(jumpForce);
            _jumpConsumed    = true;
            _jumpBufferTimer = 0f;
            _coyoteTimer     = 0f;
            _isJumping       = true;
            _jumpWasCut      = false;
            _jumpReleased    = false;
            _jumpExecuted    = true;
            _jumpCdTimer     = jumpCooldown;
        }
    }

    private void ApplyJumpCut()
    {
        if (!_isJumping)
        {
            _jumpReleased = false;
            return;
        }

        if (_jumpReleased && !_jumpWasCut && _motor.Velocity.y > 0.01f)
        {
            _motor.MultiplyVerticalVelocity(jumpCutMultiplier);
            _jumpWasCut   = true;
            _jumpReleased = false;
        }
    }

    private void ApplyGravityModifiers()
    {
        float vy = _motor.Velocity.y;

        if (vy < -0.01f)
        {
            // Падение — усиленная гравитация для резкого снижения
            _motor.AddVelocity(Vector3.up * Physics.gravity.y * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime);
            _motor.ClampFallSpeed(maxFallSpeed);
        }
        else if (Mathf.Abs(vy) < apexThreshold && !_isGrounded)
        {
            // Вершина прыжка — лёгкая гравитация для «зависания»
            _motor.AddVelocity(Vector3.up * Physics.gravity.y * (apexGravityMultiplier - 1f) * Time.fixedDeltaTime);
        }
    }

    #endregion

    #region Roll

    private bool CanRoll() => !_isRolling && _rollCdTimer <= 0f;

    private IEnumerator RollRoutine()
    {
        _isRolling = true;
        _health.SetInvulnerable(rollInvulTime);
        _inputHandler.Disable();
        TriggerAnimator(RollTriggerHash);

        Vector3 rollDir = _inputHandler.LastNonZeroDirection;
        float elapsed = 0f;

        // try-finally гарантирует, что input.Enable() и isRolling = false
        // выполнятся, даже если корутина прервана (смерть, деактивация, исключение)
        try
        {
            while (elapsed < rollDuration)
            {
                _motor.Velocity = new Vector3(
                    rollDir.x * rollSpeed, _motor.Velocity.y, rollDir.z * rollSpeed);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        finally
        {
            _inputHandler.Enable();
            _isRolling = false;
            _rollCdTimer = rollCooldown;
        }
    }

    #endregion

    #region HealthCallbacks

    private void OnDamaged(int dmg)
    {
        TriggerAnimator(HurtTriggerHash);
    }

    private void OnDied()
    {
        StopAllCoroutines(); // Завершает RollRoutine → finally-блок сбросит _isRolling и включит input
        _inputHandler.Disable();
        _motor.Stop();
        _motor.SetColliderEnabled(false);
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
            TriggerAnimator(LandTriggerHash);

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
        Vector2 moveInput = _inputHandler.RawMoveInput;

        if (moveInput.sqrMagnitude < 0.1f)
        {
            _currentDirection = AnimDirection.Idle;
        }
        else
        {
            float angle = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            if      (angle >= 337.5f || angle < 22.5f)   _currentDirection = AnimDirection.Forward;
            else if (angle >= 22.5f  && angle < 67.5f)   _currentDirection = AnimDirection.ForwardRight;
            else if (angle >= 67.5f  && angle < 112.5f)  _currentDirection = AnimDirection.Right;
            else if (angle >= 112.5f && angle < 157.5f)  _currentDirection = AnimDirection.BackRight;
            else if (angle >= 157.5f && angle < 202.5f)  _currentDirection = AnimDirection.Back;
            else if (angle >= 202.5f && angle < 247.5f)  _currentDirection = AnimDirection.BackLeft;
            else if (angle >= 247.5f && angle < 292.5f)  _currentDirection = AnimDirection.Left;
            else if (angle >= 292.5f && angle < 337.5f)  _currentDirection = AnimDirection.ForwardLeft;
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
}
