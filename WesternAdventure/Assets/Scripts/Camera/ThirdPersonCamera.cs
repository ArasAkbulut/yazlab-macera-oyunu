using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 6f;
    [SerializeField] private float targetHeight = 1f;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private float collisionPadding = 0.05f;

    public bool externalCursorControl;
    private float yaw;
    private float pitch = 20f;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("Kamera icin Target alanina Player atanmalidir.", this);
            enabled = false;
            return;
        }

        yaw = transform.eulerAngles.y;
        SetCursorLocked(true);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (!externalCursorControl && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            SetCursorLocked(false);
        }
        else if (!externalCursorControl && mouse != null && mouse.leftButton.wasPressedThisFrame &&
                 Cursor.lockState != CursorLockMode.Locked)
        {
            SetCursorLocked(true);
        }

        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 look = mouse.delta.ReadValue();

            yaw += look.x * mouseSensitivity;
            pitch -= look.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focusPoint = target.position + Vector3.up * targetHeight;

        Vector3 backward = rotation * Vector3.back;
        float actualDistance = distance;

        // Oyuncuyla kamera arasindaki engelleri kontrol eder.
        if (Physics.SphereCast(
            focusPoint,
            collisionRadius,
            backward,
            out RaycastHit hit,
            distance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore))
        {
            actualDistance = Mathf.Max(0f, hit.distance - collisionPadding);
        }

        transform.SetPositionAndRotation(
            focusPoint + backward * actualDistance,
            rotation
        );
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !locked;
    }
}