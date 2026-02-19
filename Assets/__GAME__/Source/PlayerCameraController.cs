using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("First Person")]
    [SerializeField] private Vector3 cameraLocalOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private float yawSpeed = 180f;
    [SerializeField] private float pitchSpeed = 120f;
    [SerializeField] private float minPitch = -70f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Smoothing")]
    [SerializeField] private float rotationSmoothTime = 0.02f;
    [SerializeField] private float positionSmoothTime = 0.02f;

    [Header("Player Rotation")]
    [SerializeField] private bool rotateTargetWithCamera = true;

    [Header("Cursor")]
    [SerializeField] private bool lockCursor = true;

    private float yaw;
    private float pitch;
    private float smoothYaw;
    private float smoothPitch;
    private float yawVelocity;
    private float pitchVelocity;
    private Vector3 positionVelocity;

    private void OnEnable()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (Mouse.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        yaw += delta.x * yawSpeed * Time.deltaTime;
        pitch -= delta.y * pitchSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        smoothYaw = Mathf.SmoothDampAngle(smoothYaw, yaw, ref yawVelocity, rotationSmoothTime);
        smoothPitch = Mathf.SmoothDampAngle(smoothPitch, pitch, ref pitchVelocity, rotationSmoothTime);

        Quaternion cameraRot = Quaternion.Euler(smoothPitch, smoothYaw, 0f);
        Vector3 desiredPos = target.TransformPoint(cameraLocalOffset);

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref positionVelocity, positionSmoothTime);
        transform.rotation = cameraRot;

        if (rotateTargetWithCamera)
        {
            Quaternion targetRot = Quaternion.Euler(0f, smoothYaw, 0f);
            target.rotation = targetRot;
        }
    }
}
