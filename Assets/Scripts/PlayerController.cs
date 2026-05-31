using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float aimMoveSpeed = 1.75f;

    [Header("Camera")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float lookSensitivity = 120f;
    [SerializeField] private float minPitch = -25f;
    [SerializeField] private float maxPitch = 55f;

    [Header("Camera Positions")]
    [SerializeField] private Transform mainCamera;

    [SerializeField] private Vector3 normalCameraLocalPosition = new Vector3(0f, 0.3f, -4f);
    [SerializeField] private Vector3 aimCameraLocalPosition = new Vector3(0.65f, 0.15f, -2.2f);

    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float aimFOV = 45f;
    [SerializeField] private float cameraAimSmoothSpeed = 10f;

    private Camera cam;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGravity = -2f;

    private CharacterController controller;
    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction aimAction;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isAiming;

    private float verticalVelocity;
    private float pitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        aimAction = playerInput.actions["Aim"];

        if (mainCamera != null)
        {
            cam = mainCamera.GetComponent<Camera>();
        }
    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        aimAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        aimAction.Disable();
    }

    private void Update()
    {

        ReadInput();

        HandleMovement();
        HandleCamera();
        HandleAimCamera();
    }

    private void ReadInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        lookInput = lookAction.ReadValue<Vector2>();

        // Works for trigger axes and normal buttons.
        float aimValue = aimAction.ReadValue<float>();
        isAiming = aimValue > 0.2f;

        // Temporary debug. Remove later.
        if (aimValue > 0.01f)
        {
            Debug.Log("Aim value: " + aimValue + " | Aiming: " + isAiming);
        }
    }

    private void HandleMovement()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (inputDirection.magnitude > 1f)
        {
            inputDirection.Normalize();
        }

        float currentSpeed = isAiming ? aimMoveSpeed : moveSpeed;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = forward * inputDirection.z + right * inputDirection.x;

        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        Vector3 move = moveDirection * currentSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedGravity;
        }

        verticalVelocity += gravity * Time.deltaTime;

        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    private void HandleAimCamera()
    {
        if (mainCamera == null)
            return;

        Vector3 targetPosition = isAiming ? aimCameraLocalPosition : normalCameraLocalPosition;

        mainCamera.localPosition = Vector3.Lerp(
            mainCamera.localPosition,
            targetPosition,
            cameraAimSmoothSpeed * Time.deltaTime
        );

        if (cam != null)
        {
            float targetFOV = isAiming ? aimFOV : normalFOV;

            cam.fieldOfView = Mathf.Lerp(
                cam.fieldOfView,
                targetFOV,
                cameraAimSmoothSpeed * Time.deltaTime
            );
        }
    }

    private void HandleCamera()
    {
        if (cameraPivot == null)
        {
            return;
        }

        float yawAmount = lookInput.x * lookSensitivity * Time.deltaTime;
        float pitchAmount = lookInput.y * lookSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * yawAmount);

        pitch -= pitchAmount;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}