using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerInventory))]
public class PlayerController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject crosshair;

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

    [Header("Weapon")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private WeaponBase[] weapons;
    [SerializeField] private int startingWeaponIndex;
    [SerializeField] private WeaponBase equippedWeapon;

    private InputAction fireAction;
    private InputAction reloadAction;
    private InputAction toggleFireModeAction;
    private InputAction nextWeaponAction;
    private InputAction previousWeaponAction;
    private InputAction[] weaponSlotActions;
    private bool wasFirePressed;
    private bool wasReloadPressed;
    private bool wasToggleFireModePressed;
    private bool wasNextWeaponPressed;
    private bool wasPreviousWeaponPressed;
    private bool[] wasWeaponSlotPressed;

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

    public WeaponBase EquippedWeapon => equippedWeapon;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        aimAction = playerInput.actions["Aim"];

        fireAction = playerInput.actions.FindAction("Fire", false);
        reloadAction = playerInput.actions.FindAction("Reload", false);
        toggleFireModeAction = playerInput.actions.FindAction("ToggleFireMode", false);
        nextWeaponAction = playerInput.actions.FindAction("NextWeapon", false);
        previousWeaponAction = playerInput.actions.FindAction("PreviousWeapon", false);
        weaponSlotActions = new InputAction[]
        {
            playerInput.actions.FindAction("WeaponSlot1", false),
            playerInput.actions.FindAction("WeaponSlot2", false),
            playerInput.actions.FindAction("WeaponSlot3", false),
            playerInput.actions.FindAction("WeaponSlot4", false)
        };
        wasWeaponSlotPressed = new bool[weaponSlotActions.Length];

        if (mainCamera != null)
        {
            cam = mainCamera.GetComponent<Camera>();
        }

        if (fireAction == null)
            Debug.LogError("Missing input action: Fire");

        if (reloadAction == null)
            Debug.LogError("Missing input action: Reload");

        if (inventory == null)
        {
            inventory = GetComponent<PlayerInventory>();
        }

        if (inventory == null)
        {
            inventory = gameObject.AddComponent<PlayerInventory>();
        }

        inventory.InitialiseStartingInventory();
        InitialiseWeapons();

    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        aimAction.Enable();

        fireAction?.Enable();
        reloadAction?.Enable();
        toggleFireModeAction?.Enable();
        nextWeaponAction?.Enable();
        previousWeaponAction?.Enable();

        foreach (InputAction weaponSlotAction in weaponSlotActions)
        {
            weaponSlotAction?.Enable();
        }
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        aimAction.Disable();

        fireAction?.Disable();
        reloadAction?.Disable();
        toggleFireModeAction?.Disable();
        nextWeaponAction?.Disable();
        previousWeaponAction?.Disable();

        foreach (InputAction weaponSlotAction in weaponSlotActions)
        {
            weaponSlotAction?.Disable();
        }
    }

    private void Update()
    {
        ReadInput();

        if (crosshair != null)
        {
            crosshair.SetActive(isAiming);
        }

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

        float fireValue = fireAction != null ? fireAction.ReadValue<float>() : 0f;
        bool isFirePressed = fireValue > 0.2f;

        if (isFirePressed && isAiming && equippedWeapon != null)
        {
            if (equippedWeapon.FiresContinuously || !wasFirePressed)
            {
                equippedWeapon.TryFire();
            }
        }

        wasFirePressed = isFirePressed;

        float reloadValue = reloadAction != null ? reloadAction.ReadValue<float>() : 0f;
        bool isReloadPressed = reloadValue > 0.2f;

        if (isReloadPressed && !wasReloadPressed)
        {
            if (equippedWeapon != null)
            {
                equippedWeapon.TryReload();
            }
        }

        wasReloadPressed = isReloadPressed;

        float toggleFireModeValue = toggleFireModeAction != null ? toggleFireModeAction.ReadValue<float>() : 0f;
        bool isToggleFireModePressed = toggleFireModeValue > 0.2f;

        if (isToggleFireModePressed && !wasToggleFireModePressed && equippedWeapon != null)
        {
            equippedWeapon.ToggleFireMode();
        }

        wasToggleFireModePressed = isToggleFireModePressed;

        ReadWeaponSwitchInput();
    }

    private void InitialiseWeapons()
    {
        if (weapons == null || weapons.Length == 0)
        {
            if (equippedWeapon != null)
                weapons = new[] { equippedWeapon };
            else
                weapons = GetComponentsInChildren<WeaponBase>(true);
        }

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] == null)
                continue;

            weapons[i].Initialise(cam, inventory);
            weapons[i].enabled = false;
        }

        if (weapons.Length == 0)
            return;

        int weaponIndex = Mathf.Clamp(startingWeaponIndex, 0, weapons.Length - 1);
        EquipWeapon(weaponIndex);
    }

    private void ReadWeaponSwitchInput()
    {
        bool isNextWeaponPressed = IsPressed(nextWeaponAction);

        if (isNextWeaponPressed && !wasNextWeaponPressed)
            EquipNextWeapon();

        wasNextWeaponPressed = isNextWeaponPressed;

        bool isPreviousWeaponPressed = IsPressed(previousWeaponAction);

        if (isPreviousWeaponPressed && !wasPreviousWeaponPressed)
            EquipPreviousWeapon();

        wasPreviousWeaponPressed = isPreviousWeaponPressed;

        for (int i = 0; i < weaponSlotActions.Length; i++)
        {
            bool isWeaponSlotPressed = IsPressed(weaponSlotActions[i]);

            if (isWeaponSlotPressed && !wasWeaponSlotPressed[i])
                EquipWeapon(i);

            wasWeaponSlotPressed[i] = isWeaponSlotPressed;
        }
    }

    private bool IsPressed(InputAction action)
    {
        return action != null && action.ReadValue<float>() > 0.2f;
    }

    private void EquipNextWeapon()
    {
        if (weapons == null || weapons.Length == 0)
            return;

        int currentIndex = GetEquippedWeaponIndex();
        EquipWeapon((currentIndex + 1) % weapons.Length);
    }

    private void EquipPreviousWeapon()
    {
        if (weapons == null || weapons.Length == 0)
            return;

        int currentIndex = GetEquippedWeaponIndex();
        EquipWeapon((currentIndex - 1 + weapons.Length) % weapons.Length);
    }

    private void EquipWeapon(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Length || weapons[index] == null)
            return;

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
                weapons[i].enabled = i == index;
        }

        equippedWeapon = weapons[index];
        equippedWeapon.Initialise(cam, inventory);

        Debug.Log($"Equipped {equippedWeapon.name}. Ammo: {equippedWeapon.CurrentAmmo}/{equippedWeapon.ReserveAmmo}");
    }

    private int GetEquippedWeaponIndex()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] == equippedWeapon)
                return i;
        }

        return 0;
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
