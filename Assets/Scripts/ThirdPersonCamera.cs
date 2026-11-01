using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Drag your Player GameObject here")]
    public Transform target;

    [Header("Camera Distance & Positioning")]
    [Tooltip("Distance behind the player")]
    public float distance = 6.5f;
    [Tooltip("Minimum distance when compressed against a wall")]
    public float minDistance = 1.2f;
    [Tooltip("Height above the player's feet")]
    public float heightOffset = 3.2f;
    [Tooltip("Right shoulder offset (PUBG style)")]
    public float shoulderOffset = 1.3f;

    [Header("Mouse Orbit Settings")]
    public float mouseSensitivity = 2.5f;
    public float minPitch = -40f;
    public float maxPitch = 65f;

    [Header("Smoothing")]
    public float positionSmoothSpeed = 20f;
    public float rotationSmoothSpeed = 25f;

    [Header("Wall Collision")]
    [Tooltip("Prevents camera from clipping into walls and corners")]
    public LayerMask collisionLayers = ~0; // Everything
    public float wallBuffer = 0.3f;

    [HideInInspector]
    public float currentYaw = 0f;
    [HideInInspector]
    public float currentPitch = 15f;

    private float currentDistance;

    private void Start()
    {
        currentDistance = distance;

        // Initialize angles
        if (target != null)
        {
            currentYaw = target.eulerAngles.y;
        }

        // Lock cursor only if game is already active
        if (GameManager.Instance == null || GameManager.Instance.isGameActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        // If in menu or game over, keep cursor free for UI interaction
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive)
        {
            return;
        }

        // Toggle cursor with Escape
        #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        #else
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        #endif
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive) return;

        // 1. Read Mouse Orbit (Free camera look like PUBG)
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 mouseDelta = Vector2.zero;
            #if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                mouseDelta = UnityEngine.InputSystem.Mouse.current.delta.ReadValue() * 0.1f * mouseSensitivity;
            }
            #else
            mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * mouseSensitivity;
            #endif

            currentYaw += mouseDelta.x;
            currentPitch -= mouseDelta.y;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }

        Quaternion orbitRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

        // 2. Focus point (at the player's chest/head)
        Vector3 focusPoint = target.position + Vector3.up * heightOffset;

        // 3. Ideal camera position with shoulder offset
        Vector3 shoulderVector = orbitRotation * Vector3.right * shoulderOffset;
        Vector3 backVector = orbitRotation * Vector3.back;
        Vector3 targetCameraPos = focusPoint + shoulderVector + (backVector * distance);

        // 4. Spring-Arm Wall Collision:
        // Cast from focus point toward camera. If a wall is hit, pull closer along the view line.
        // It NEVER trips to the floor because it stays on the orbit line!
        Vector3 rayDirection = targetCameraPos - (focusPoint + shoulderVector);
        float rayDistance = rayDirection.magnitude;

        float targetDist = distance;
        if (Physics.SphereCast(focusPoint + shoulderVector, 0.2f, rayDirection.normalized, out RaycastHit hit, rayDistance, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            // Ignore player's own colliders
            if (hit.transform.root != target.root)
            {
                targetDist = Mathf.Clamp(hit.distance - wallBuffer, minDistance, distance);
            }
        }

        currentDistance = Mathf.Lerp(currentDistance, targetDist, positionSmoothSpeed * Time.deltaTime);
        Vector3 finalPos = focusPoint + shoulderVector + (backVector * currentDistance);

        // Smooth position and rotation
        transform.position = Vector3.Lerp(transform.position, finalPos, positionSmoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, orbitRotation, rotationSmoothSpeed * Time.deltaTime);
    }
}
