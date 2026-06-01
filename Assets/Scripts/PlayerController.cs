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
    [SerializeField] private InventoryMenuUI inventoryMenu;
    [SerializeField] private WeaponBase[] weapons;
    [SerializeField] private int startingWeaponIndex;
    [SerializeField] private WeaponBase equippedWeapon;

    private InputAction fireAction;
    private InputAction reloadAction;
    private InputAction toggleFireModeAction;
    private InputAction toggleScopeAction;
    private InputAction nextWeaponAction;
    private InputAction previousWeaponAction;
    private InputAction[] weaponSlotActions;
    private bool wasFirePressed;
    private bool wasReloadPressed;
    private bool wasToggleFireModePressed;
    private bool wasToggleScopePressed;
    private bool wasNextWeaponPressed;
    private bool wasPreviousWeaponPressed;
    private bool[] wasWeaponSlotPressed;

    private Camera cam;
    private CameraShake cameraShake;
    private Vector3 previousCameraShakeOffset;

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
        toggleScopeAction = playerInput.actions.FindAction("ToggleScope", false);
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
            cameraShake = mainCamera.GetComponent<CameraShake>();

            if (cameraShake == null)
                cameraShake = mainCamera.gameObject.AddComponent<CameraShake>();
        }

        if (fireAction == null)
            Debug.LogError("Missing input action: Fire");

        if (reloadAction == null)
            Debug.LogError("Missing input action: Reload");

        if (inventory == null)
        {
            inventory = GetComponent<PlayerInventory>();
        }

        if (inventoryMenu == null)
        {
            inventoryMenu = GetComponent<InventoryMenuUI>();
        }

        if (inventory == null)
        {
            inventory = gameObject.AddComponent<PlayerInventory>();
        }

        inventory.InitialiseStartingInventory();
        inventory.Changed += HandleInventoryChanged;
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
        toggleScopeAction?.Enable();
        nextWeaponAction?.Enable();
        previousWeaponAction?.Enable();

        foreach (InputAction weaponSlotAction in weaponSlotActions)
        {
            weaponSlotAction?.Enable();
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.Changed -= HandleInventoryChanged;

        moveAction.Disable();
        lookAction.Disable();
        aimAction.Disable();

        fireAction?.Disable();
        reloadAction?.Disable();
        toggleFireModeAction?.Disable();
        toggleScopeAction?.Disable();
        nextWeaponAction?.Disable();
        previousWeaponAction?.Disable();

        foreach (InputAction weaponSlotAction in weaponSlotActions)
        {
            weaponSlotAction?.Disable();
        }
    }

    private void Update()
    {
        if (inventoryMenu != null && inventoryMenu.IsOpen)
        {
            equippedWeapon?.SetAiming(false);
            return;
        }

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
        equippedWeapon?.SetAiming(isAiming);

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

        float toggleScopeValue = toggleScopeAction != null ? toggleScopeAction.ReadValue<float>() : 0f;
        bool isToggleScopePressed = toggleScopeValue > 0.2f;

        if (isToggleScopePressed && !wasToggleScopePressed && equippedWeapon != null)
        {
            equippedWeapon.ToggleScope();
        }

        wasToggleScopePressed = isToggleScopePressed;

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

        int weaponIndex = GetFirstAvailableWeaponIndex();

        if (IsWeaponAvailable(startingWeaponIndex))
            weaponIndex = Mathf.Clamp(startingWeaponIndex, 0, weapons.Length - 1);

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
        EquipWeapon(GetNextAvailableWeaponIndex(currentIndex, 1));
    }

    private void EquipPreviousWeapon()
    {
        if (weapons == null || weapons.Length == 0)
            return;

        int currentIndex = GetEquippedWeaponIndex();
        EquipWeapon(GetNextAvailableWeaponIndex(currentIndex, -1));
    }

    private void EquipWeapon(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Length || weapons[index] == null || !IsWeaponAvailable(index))
            return;

        if (equippedWeapon != null)
            equippedWeapon.OnUnequipped();

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
                weapons[i].enabled = i == index;
        }

        equippedWeapon = weapons[index];
        equippedWeapon.Initialise(cam, inventory);
        equippedWeapon.OnEquipped();
        equippedWeapon.SetAiming(isAiming);

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

    private bool IsWeaponAvailable(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Length || weapons[index] == null)
            return false;

        return inventory == null || inventory.HasWeapon(weapons[index].WeaponType);
    }

    private int GetFirstAvailableWeaponIndex()
    {
        if (weapons == null)
            return -1;

        for (int i = 0; i < weapons.Length; i++)
        {
            if (IsWeaponAvailable(i))
                return i;
        }

        return -1;
    }

    private int GetNextAvailableWeaponIndex(int startIndex, int direction)
    {
        if (weapons == null || weapons.Length == 0)
            return -1;

        for (int offset = 1; offset <= weapons.Length; offset++)
        {
            int index = (startIndex + offset * direction + weapons.Length) % weapons.Length;

            if (IsWeaponAvailable(index))
                return index;
        }

        return startIndex;
    }

    private void HandleInventoryChanged()
    {
        if (equippedWeapon == null || !inventory.HasWeapon(equippedWeapon.WeaponType))
        {
            EquipWeapon(GetFirstAvailableWeaponIndex());
            return;
        }

        equippedWeapon.SetAiming(isAiming);
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

        Vector3 baseCameraPosition = mainCamera.localPosition - previousCameraShakeOffset;

        baseCameraPosition = Vector3.Lerp(
            baseCameraPosition,
            targetPosition,
            cameraAimSmoothSpeed * Time.deltaTime
        );

        previousCameraShakeOffset = cameraShake != null ? cameraShake.CurrentOffset : Vector3.zero;
        mainCamera.localPosition = baseCameraPosition + previousCameraShakeOffset;

        if (cam != null)
        {
            float targetFOV = isAiming && equippedWeapon != null
                ? equippedWeapon.GetAimFOV(aimFOV)
                : normalFOV;

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
