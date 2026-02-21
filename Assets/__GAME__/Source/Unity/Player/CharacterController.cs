using UnityEngine;

/// <summary>
/// Двигательный интерфейс персонажа.
/// Капсула коллизии, проверка земли, применение перемещения через Rigidbody.
/// Не знает об input, состояниях, анимациях, прыжках.
/// </summary>
public class CharacterController : MonoBehaviour
{
    #region Settings

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Collider In Air")]
    [Tooltip("Множитель высоты коллайдера в воздухе (0.7 = сжимается на 30%)")]
    [Range(0.3f, 1f)]
    [SerializeField] private float airColliderHeightMul = 0.7f;
    [Tooltip("Дополнительное смещение groundCheck относительно дна коллайдера в воздухе")]
    [SerializeField] private Vector3 groundCheckAirOffset = new Vector3(0f, -0.05f, 0f);

    #endregion

    #region Cached

    private Rigidbody _rb;
    private CapsuleCollider _capsule;
    private Collider _col;

    #endregion

    #region ColliderDefaults

    private float _defaultColliderHeight;
    private Vector3 _defaultColliderCenter;
    private Vector3 _defaultGroundCheckLocalPos;

    #endregion

    #region PublicAPI

    /// <summary>Персонаж стоит на земле.</summary>
    public bool IsGrounded { get; private set; }

    /// <summary>Текущая скорость Rigidbody.</summary>
    public Vector3 Velocity
    {
        get => _rb.linearVelocity;
        set => _rb.linearVelocity = value;
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Инициализация компонента. Вызывается из PlayerController.Awake().
    /// </summary>
    public void Initialize()
    {
        _rb  = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        _capsule = GetComponent<CapsuleCollider>();

        if (_capsule != null)
        {
            _defaultColliderHeight = _capsule.height;
            _defaultColliderCenter = _capsule.center;
        }

        if (groundCheck != null)
            _defaultGroundCheckLocalPos = groundCheck.localPosition;
    }

    #endregion

    #region GroundCheck

    /// <summary>
    /// Обновляет флаг IsGrounded. Вызывается в FixedUpdate.
    /// </summary>
    public void UpdateGroundState()
    {
        IsGrounded = groundCheck != null
                     && Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }

    #endregion

    #region Movement

    /// <summary>
    /// Устанавливает горизонтальную скорость, сохраняя вертикальную.
    /// </summary>
    public void SetHorizontalVelocity(Vector3 horizontal)
    {
        _rb.linearVelocity = new Vector3(horizontal.x, _rb.linearVelocity.y, horizontal.z);
    }

    /// <summary>
    /// Сбрасывает вертикальную скорость и добавляет импульс вверх.
    /// </summary>
    public void AddVerticalImpulse(float force)
    {
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        _rb.AddForce(Vector3.up * force, ForceMode.Impulse);
    }

    /// <summary>
    /// Умножает вертикальную скорость на множитель (для обрезки прыжка).
    /// </summary>
    public void MultiplyVerticalVelocity(float multiplier)
    {
        _rb.linearVelocity = new Vector3(
            _rb.linearVelocity.x,
            _rb.linearVelocity.y * multiplier,
            _rb.linearVelocity.z);
    }

    /// <summary>
    /// Добавляет дельту к скорости (для модификаторов гравитации).
    /// </summary>
    public void AddVelocity(Vector3 delta)
    {
        _rb.linearVelocity += delta;
    }

    /// <summary>
    /// Ограничивает скорость падения.
    /// </summary>
    public void ClampFallSpeed(float maxFallSpeed)
    {
        if (_rb.linearVelocity.y < -maxFallSpeed)
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, -maxFallSpeed, _rb.linearVelocity.z);
    }

    /// <summary>Полная остановка.</summary>
    public void Stop()
    {
        _rb.linearVelocity = Vector3.zero;
    }

    #endregion

    #region Collider

    /// <summary>
    /// Обновляет высоту коллайдера: сжатие в воздухе, восстановление на земле.
    /// </summary>
    public void UpdateCollider(bool inAir)
    {
        if (_capsule == null) return;

        if (inAir)
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
                    _defaultGroundCheckLocalPos.z + groundCheckAirOffset.z);
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

    /// <summary>Включить/выключить коллайдер.</summary>
    public void SetColliderEnabled(bool enabled)
    {
        if (_col != null) _col.enabled = enabled;
    }

    #endregion

    #region Gizmos
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
#endif
    #endregion
}