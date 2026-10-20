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

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            SetCursorLocked(false);
        }
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame &&
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

        transform.SetPositionAndRotation(
            focusPoint - rotation * Vector3.forward * distance,
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