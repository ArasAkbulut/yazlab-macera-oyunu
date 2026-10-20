using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private float verticalSpeed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null)
        {
            Debug.LogError("PlayerMovement icin kamera atanmalidir.", this);
            enabled = false;
        }
    }
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Vector2 input = Vector2.zero;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
        }

        // Capraz hareketin daha hizli olmasini engeller.
        input = Vector2.ClampMagnitude(input, 1f);

        if (controller.isGrounded && verticalSpeed < 0f)
        {
            verticalSpeed = -2f;
        }

        if (keyboard != null &&
            keyboard.spaceKey.wasPressedThisFrame &&
            controller.isGrounded)
        {
            verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalSpeed += gravity * Time.deltaTime;

        // Kameranin sadece yatay yonunu kullanir.
        Quaternion cameraYaw = Quaternion.Euler(
            0f,
            cameraTransform.eulerAngles.y,
            0f
        );

        Vector3 moveDirection =
            cameraYaw * new Vector3(input.x, 0f, input.y);

        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = verticalSpeed;

        // Karakteri yurudugu yone dondurur.
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                540f * Time.deltaTime
            );
        }

        CollisionFlags collisions =
            controller.Move(velocity * Time.deltaTime);

        // Tavana carptiginda yukari hareketi durdurur.
        if ((collisions & CollisionFlags.Above) != 0 &&
            verticalSpeed > 0f)
        {
            verticalSpeed = 0f;
        }
    }
}