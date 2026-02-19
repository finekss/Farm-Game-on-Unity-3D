using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Character : MonoBehaviour
{
    public static Character player;

    #region CachedComponents
    private Rigidbody rb;
    private Collider col;
    private Camera cam;
    private InputSystem_Actions input;
    #endregion

    #region Model
    [Header("Model")]
    [Tooltip("Корневой Transform 3D-модели персонажа (для поворота отдельно от корня)")]
    [SerializeField] private Transform model;
    #endregion

    #region MovementSettings
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 720f;
    #endregion

    #region JumpSettings
    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
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

    #region AttackSettings
    [Header("Attack")]
    [SerializeField] private int damage = 2;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackRadius = 2f;
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private LayerMask enemyLayer;
    #endregion

    #region RuntimeState
    private Vector2 moveInput;
    private Vector3 moveDirection;
    private Vector3 lastNonZeroDirection = Vector3.forward;

    private bool isGrounded;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool jumpConsumed;

    private bool isRolling;
    private float rollCDTimer;
    private float invulTimer;
    private float attackCDTimer;

    private readonly Plane aimPlane = new(Vector3.up, Vector3.zero);
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
    }

    private void OnEnable() => input.Enable();
    private void OnDisable() => input.Disable();

    private void Update()
    {
        if (IsDead) return;
        ReadInput();
        TickTimers();
        RotateModel();
    }

    private void FixedUpdate()
    {
        if (IsDead) return;
        CheckGround();
        ApplyMovement();
        ApplyJump();
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

        if (input.Player.Roll.triggered && CanRoll()) StartCoroutine(RollRoutine());
    }
    #endregion

    #region Timers
    private void TickTimers()
    {
        float dt = Time.deltaTime;
        invulTimer = Mathf.Max(0f, invulTimer - dt);
        rollCDTimer = Mathf.Max(0f, rollCDTimer - dt);
        attackCDTimer = Mathf.Max(0f, attackCDTimer - dt);
        jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - dt);
    }
    #endregion

    #region GroundCheck
    private void CheckGround()
    {
        bool wasGrounded = isGrounded;
        isGrounded = groundCheck != null && Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded) coyoteTimer = coyoteTime;
        else if (wasGrounded) coyoteTimer = coyoteTime;

        coyoteTimer = Mathf.Max(0f, coyoteTimer - Time.fixedDeltaTime);
    }
    #endregion

    #region Movement
    private void ApplyMovement()
    {
        if (isRolling) return;
        rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, rb.linearVelocity.y, moveDirection.z * moveSpeed);
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
        bool canJump = (isGrounded || coyoteTimer > 0f) && !isRolling;
        if (jumpBufferTimer > 0f && canJump && !jumpConsumed)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpConsumed = true;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
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
        if (CurrentHealth <= 0) Die();
    }

    private void Die()
    {
        IsDead = true;
        input.Disable();
        rb.linearVelocity = Vector3.zero;
        col.enabled = false;
        gameObject.SetActive(false);
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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
#endif
    #endregion
}
