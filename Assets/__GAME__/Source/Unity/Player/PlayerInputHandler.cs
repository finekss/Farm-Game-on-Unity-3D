using UnityEngine;

/// <summary>
/// Слой абстракции ввода.
/// Читает Input System, конвертирует в команды.
/// Не знает о состояниях, стамине, возможности действий.
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    private InputSystem_Actions _input;
    private Camera _cam;

    #region PublicAPI

    /// <summary>Сырой 2D-вектор ввода движения.</summary>
    public Vector2 RawMoveInput { get; private set; }

    /// <summary>Направление движения в мировых координатах (с учётом камеры).</summary>
    public Vector3 MoveDirection { get; private set; }

    /// <summary>Последнее ненулевое направление (для рывка и т.п.).</summary>
    public Vector3 LastNonZeroDirection { get; private set; } = Vector3.forward;

    /// <summary>Кнопка прыжка нажата в этом кадре.</summary>
    public bool JumpPressed { get; private set; }

    /// <summary>Кнопка прыжка отпущена в этом кадре.</summary>
    public bool JumpReleased { get; private set; }

    /// <summary>Кнопка прыжка удерживается.</summary>
    public bool JumpHeld { get; private set; }

    /// <summary>Кнопка подката нажата в этом кадре.</summary>
    public bool RollPressed { get; private set; }

    #endregion

    #region Lifecycle

    public void Initialize(Camera cam)
    {
        _cam = cam;
        _input = new InputSystem_Actions();
    }

    public void Enable()  => _input?.Enable();
    public void Disable() => _input?.Disable();

    public void DisposeInput()
    {
        _input?.Disable();
        _input?.Dispose();
    }

    #endregion

    #region ReadInput

    /// <summary>
    /// Чтение ввода. Вызывается каждый Update из PlayerController.
    /// </summary>
    public void ReadInput()
    {
        // ── Movement ──
        RawMoveInput = _input.Player.Move.ReadValue<Vector2>();

        Vector3 camForward = _cam != null ? _cam.transform.forward : transform.forward;
        Vector3 camRight   = _cam != null ? _cam.transform.right   : transform.right;
        camForward.y = 0f;
        camRight.y   = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 dir = camForward * RawMoveInput.y + camRight * RawMoveInput.x;
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        MoveDirection = dir;

        if (dir.sqrMagnitude > 0.01f)
            LastNonZeroDirection = dir.normalized;

        // ── Jump ──
        JumpPressed = _input.Player.Jump.triggered;

        bool prevHeld = JumpHeld;
        JumpHeld     = _input.Player.Jump.IsPressed();
        JumpReleased = prevHeld && !JumpHeld;

        // ── Roll ──
        RollPressed = _input.Player.Roll.triggered;
    }

    #endregion
}