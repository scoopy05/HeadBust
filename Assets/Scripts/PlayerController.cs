using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed")]
    public float moveSpeed = 10f;
    [Tooltip("How fast the character turns to face the movement direction")]
    public float rotationSpeed = 750f;
    [Tooltip("Jump velocity")]
    public float jumpHeight = 2.5f;
    [Tooltip("Gravity force")]
    public float gravity = -25f;

    [Header("Camera & Aim References")]
    [Tooltip("Drag the Main Camera here (or leave empty to auto-detect Camera.main)")]
    public Transform cameraTransform;

    [Tooltip("Drag your Head_Mount GameObject here")]
    public Transform headMount;

    [Header("Aim Settings")]
    [Tooltip("Speed at which the gun snaps to the aim direction when firing")]
    public float aimAlignSpeed = 25f;

    private CharacterController controller;
    private ThirdPersonCamera thirdPersonCamera;
    private Vector3 verticalVelocity;
    private float targetPitch = 0f;
    private float currentGunPitch = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            thirdPersonCamera = cameraTransform.GetComponent<ThirdPersonCamera>();
        }
    }

    private void Update()
    {
        HandleMovementAndAim();
    }

    private void HandleMovementAndAim()
    {
        // 1. Ground Check
        bool isGrounded = controller.isGrounded;
        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        // 2. Read Inputs
        Vector2 inputVector = Vector2.zero;
        bool jumpPressed = false;
        bool isFiringOrAiming = false;

        #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) inputVector.y += 1f;
            if (Keyboard.current.sKey.isPressed) inputVector.y -= 1f;
            if (Keyboard.current.dKey.isPressed) inputVector.x += 1f;
            if (Keyboard.current.aKey.isPressed) inputVector.x -= 1f;

            jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        }
        if (Mouse.current != null)
        {
            // Left Click = Shoot, Right Click = Aim
            isFiringOrAiming = Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed;
        }
        #else
        inputVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        jumpPressed = Input.GetKeyDown(KeyCode.Space);
        isFiringOrAiming = Input.GetMouseButton(0) || Input.GetMouseButton(1);
        #endif

        inputVector = inputVector.normalized;

        // 3. Movement Direction (Camera-Relative)
        Vector3 moveDirection = Vector3.zero;
        if (cameraTransform != null)
        {
            Vector3 camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camRight = Vector3.Scale(cameraTransform.right, new Vector3(1, 0, 1)).normalized;
            moveDirection = (camForward * inputVector.y + camRight * inputVector.x).normalized;
        }
        else
        {
            moveDirection = (transform.forward * inputVector.y + transform.right * inputVector.x).normalized;
        }

        // 4. Character Rotation & Gun Pitch Logic
        if (isFiringOrAiming && cameraTransform != null)
        {
            // --- WHEN FIRING / AIMING ---
            // 1. Body snaps to face the camera crosshair direction
            Vector3 camFacing = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
            if (camFacing.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(camFacing);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * 2f * Time.deltaTime);
            }

            // 2. Gun pitches up/down to match camera angle
            float camPitch = thirdPersonCamera != null ? thirdPersonCamera.currentPitch : cameraTransform.eulerAngles.x;
            if (camPitch > 180f) camPitch -= 360f;
            targetPitch = camPitch;
        }
        else
        {
            // --- WHEN NORMAL RUNNING / WALKING ---
            // 1. Body faces the walking direction
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            // 2. Gun rests LEVEL (0 pitch) pointing straight forward with the body!
            // Even if the camera looks down or up, gun stays steady until you shoot.
            targetPitch = 0f;
        }

        // Smoothly adjust gun pitch
        if (headMount != null)
        {
            currentGunPitch = Mathf.Lerp(currentGunPitch, targetPitch, aimAlignSpeed * Time.deltaTime);
            headMount.localEulerAngles = new Vector3(currentGunPitch, 0f, 0f);
        }

        // 5. Move Character
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        // 6. Jump & Gravity
        if (jumpPressed && isGrounded)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }
}
