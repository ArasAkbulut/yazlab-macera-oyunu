using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Hareket")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private Transform cameraTransform;

    [Header("Egim ve kayma")]
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private float slideSpeed = 6f;
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private LayerMask groundLayers = ~0;

    private CharacterController controller;
    private Animator animator;
    private PlayerHealth playerHealth;

    private float verticalSpeed;
    private float originalStepOffset;

    private Vector3 steepContactNormal;
    private bool hasSteepContact;

    private static readonly int IsRunning =
        Animator.StringToHash("IsRunning");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        playerHealth = GetComponent<PlayerHealth>();

        controller.slopeLimit = maxSlopeAngle;
        originalStepOffset = controller.stepOffset;

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
        bool isDead = playerHealth != null && playerHealth.IsDead;

        Keyboard keyboard = Keyboard.current;
        Vector2 input = Vector2.zero;
        bool sprinting = false;

        // Oyuncu öldüyse hareket tuþlarýný okuma.
        if (!isDead && keyboard != null)
        {
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;

            sprinting = keyboard.leftShiftKey.isPressed ||
                        keyboard.rightShiftKey.isPressed;
        }

        input = Vector2.ClampMagnitude(input, 1f);

        bool foundGround = FindGround(out RaycastHit groundHit);

        bool walkableGround =
            foundGround &&
            Vector3.Angle(groundHit.normal, Vector3.up)
                <= maxSlopeAngle &&
            verticalSpeed <= 0f;

        bool steepGround =
            foundGround &&
            Vector3.Angle(groundHit.normal, Vector3.up)
                > maxSlopeAngle;

        // Yalnýzca yürünebilir zeminde düþüþü sýfýrla.
        if (walkableGround)
        {
            verticalSpeed = -2f;
        }

        // Ölü oyuncu zýplayamaz.
        // Duvar veya dik eðim yeni bir zýplama hakký vermez.
        if (!isDead &&
            keyboard != null &&
            keyboard.spaceKey.wasPressedThisFrame &&
            walkableGround)
        {
            verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
            walkableGround = false;
        }

        // Ölümden sonra da yerçekimi çalýþmaya devam eder.
        verticalSpeed += gravity * Time.deltaTime;
        verticalSpeed = Mathf.Max(verticalSpeed, -40f);

        // Havadayken basamak týrmanmasýný engelle.
        controller.stepOffset =
            walkableGround ? originalStepOffset : 0f;

        Quaternion cameraYaw = Quaternion.Euler(
            0f, cameraTransform.eulerAngles.y, 0f
        );

        Vector3 moveDirection =
            cameraYaw * new Vector3(input.x, 0f, input.y);

        float speed = sprinting ? sprintSpeed : moveSpeed;
        Vector3 horizontalVelocity = moveDirection * speed;
        Vector3 slideVelocity = Vector3.zero;

        Vector3 surfaceNormal = Vector3.up;
        bool touchingSteepSurface = false;

        if (steepGround)
        {
            surfaceNormal = groundHit.normal;
            touchingSteepSurface = true;
        }
        else if (!walkableGround && hasSteepContact)
        {
            surfaceNormal = steepContactNormal;
            touchingSteepSurface = true;
        }

        if (touchingSteepSurface)
        {
            // Duvara veya dik yamaca doðru bastýrmayý kaldýr.
            Vector3 outward = new Vector3(
                surfaceNormal.x, 0f, surfaceNormal.z
            ).normalized;

            float intoSurface =
                Vector3.Dot(horizontalVelocity, outward);

            if (intoSurface < 0f)
            {
                horizontalVelocity -= outward * intoSurface;
            }

            // Düþüþ sýrasýnda yamaç boyunca aþaðý kay.
            if (verticalSpeed <= 0f)
            {
                Vector3 downhill =
                    Vector3.ProjectOnPlane(
                        Vector3.down, surfaceNormal
                    ).normalized;

                slideVelocity = downhill * slideSpeed;
            }
        }

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                540f * Time.deltaTime
            );
        }

        Vector3 velocity =
            horizontalVelocity +
            Vector3.up * verticalSpeed +
            slideVelocity;

        // Move sýrasýnda yeni temas bilgileri toplanýr.
        hasSteepContact = false;

        CollisionFlags collisions =
            controller.Move(velocity * Time.deltaTime);

        if ((collisions & CollisionFlags.Above) != 0 &&
            verticalSpeed > 0f)
        {
            verticalSpeed = 0f;
        }

        if (animator != null)
        {
            animator.SetBool(
                IsRunning,
                !isDead && input.sqrMagnitude > 0.001f
            );
        }
    }

    private bool FindGround(out RaycastHit closestHit)
    {
        Bounds bounds = controller.bounds;
        float radius = Mathf.Min(
            bounds.extents.x, bounds.extents.z
        ) * 0.9f;

        Vector3 bottom = new Vector3(
            bounds.center.x,
            bounds.min.y,
            bounds.center.z
        );

        const float lift = 0.1f;
        Vector3 origin = bottom + Vector3.up * (radius + lift);

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            radius,
            Vector3.down,
            lift + groundCheckDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );

        closestHit = default;
        float closestDistance = float.PositiveInfinity;
        bool found = false;

        foreach (RaycastHit hit in hits)
        {
            // Kendi collider'larýmýzý zemin sayma.
            if (hit.collider == controller ||
                hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            // Baþlangýçta iç içe olan veya duvar olan temaslarý atla.
            if (hit.distance <= 0f || hit.normal.y <= 0.01f)
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                found = true;
            }
        }

        return found;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        float angle = Vector3.Angle(hit.normal, Vector3.up);

        // Tavanlarý deðil, dik yamaç ve duvarlarý kaydet.
        if (angle > maxSlopeAngle && hit.normal.y >= -0.01f)
        {
            steepContactNormal = hit.normal;
            hasSteepContact = true;
        }
    }
}