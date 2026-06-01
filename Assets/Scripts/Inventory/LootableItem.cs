using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Collider))]
public class LootableItem : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private InventoryItemKind itemKind = InventoryItemKind.Ammo;
    [SerializeField] private string itemName = "Item";
    [SerializeField] private InventoryItemSize size = new InventoryItemSize(2, 1);
    [SerializeField] private int quantity = 1;
    [SerializeField] private int maxStack = 10;
    [SerializeField] private AmmoType ammoType;
    [SerializeField] private WeaponType weaponType;
    [SerializeField] private AttachmentType attachmentType;

    [Header("Pickup")]
    [SerializeField] private Key pickupKey = Key.E;
    [SerializeField] private bool pickUpOnContact;
    [SerializeField] private bool hideWhilePending = true;
    [SerializeField] private LayerMask playerLayers = ~0;
    [SerializeField] private string promptText;
    [SerializeField] private Vector2 promptScreenPosition = new Vector2(0f, 120f);
    [SerializeField] private string gamepadPrompt = "A";
    [SerializeField] private string keyboardPrompt = "E";

    private PlayerInventory playerInventory;
    private InventoryMenuUI inventoryMenu;
    private Collider[] itemColliders;
    private bool playerInRange;
    private bool isPending;
    private Canvas promptCanvas;
    private RectTransform promptRoot;
    private TMP_Text promptLabel;

    private void Awake()
    {
        itemColliders = GetComponentsInChildren<Collider>();
    }

    private void Reset()
    {
        Collider itemCollider = GetComponent<Collider>();
        itemCollider.isTrigger = true;
    }

    private void Update()
    {
        if (isPending)
        {
            SetPromptVisible(false);
            return;
        }

        RefreshPlayerInPickupVolume();
        SetPromptVisible(playerInRange && !pickUpOnContact);

        if (!playerInRange)
            return;

        if (pickUpOnContact)
        {
            TryPickup();
            return;
        }

        bool keyboardPressed = Keyboard.current != null && Keyboard.current[pickupKey].wasPressedThisFrame;
        bool gamepadPressed = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        if (keyboardPressed || gamepadPressed)
            TryPickup();
    }

    private void OnDisable()
    {
        SetPromptVisible(false);
    }

    private void OnDestroy()
    {
        if (promptCanvas != null)
            Destroy(promptCanvas.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!TryGetPlayerInventory(other, out PlayerInventory foundInventory, out InventoryMenuUI foundMenu))
            return;

        playerInventory = foundInventory;
        inventoryMenu = foundMenu;
        playerInRange = true;

        if (pickUpOnContact && !isPending)
            TryPickup();
    }

    private void OnTriggerExit(Collider other)
    {
        if (playerInventory == null || !TryGetPlayerInventory(other, out PlayerInventory foundInventory, out _))
            return;

        if (foundInventory != playerInventory)
            return;

        playerInRange = false;
        playerInventory = null;
        inventoryMenu = null;
        SetPromptVisible(false);
    }

    public void CompletePickup(bool destroyItem)
    {
        isPending = false;

        if (destroyItem)
        {
            SetPromptVisible(false);
            Destroy(gameObject);
            return;
        }

        if (hideWhilePending)
            gameObject.SetActive(true);
    }

    private void TryPickup()
    {
        if (playerInventory == null)
            return;

        InventoryEntry entry = CreateEntry();

        if (playerInventory.TryAddEntryAuto(entry))
        {
            SetPromptVisible(false);
            Destroy(gameObject);
            return;
        }

        if (inventoryMenu == null)
            inventoryMenu = playerInventory.GetComponent<InventoryMenuUI>();

        if (inventoryMenu == null)
            return;

        isPending = true;
        SetPromptVisible(false);
        inventoryMenu.OpenForLoot(entry, this);

        if (hideWhilePending)
            gameObject.SetActive(false);
    }

    private void RefreshPlayerInPickupVolume()
    {
        if (itemColliders == null || itemColliders.Length == 0)
            itemColliders = GetComponentsInChildren<Collider>();

        bool foundPlayer = false;

        foreach (Collider itemCollider in itemColliders)
        {
            if (itemCollider == null || !itemCollider.enabled)
                continue;

            Bounds bounds = itemCollider.bounds;
            Collider[] overlaps = Physics.OverlapBox(
                bounds.center,
                bounds.extents,
                Quaternion.identity,
                playerLayers,
                QueryTriggerInteraction.Collide
            );

            foreach (Collider overlap in overlaps)
            {
                if (overlap == itemCollider)
                    continue;

                if (!TryGetPlayerInventory(overlap, out PlayerInventory foundInventory, out InventoryMenuUI foundMenu))
                    continue;

                playerInventory = foundInventory;
                inventoryMenu = foundMenu;
                foundPlayer = true;
                break;
            }

            if (foundPlayer)
                break;
        }

        if (!foundPlayer)
        {
            playerInventory = null;
            inventoryMenu = null;
        }

        playerInRange = foundPlayer;
    }

    private bool TryGetPlayerInventory(Collider other, out PlayerInventory foundInventory, out InventoryMenuUI foundMenu)
    {
        foundInventory = other.GetComponentInParent<PlayerInventory>();
        foundMenu = foundInventory != null ? foundInventory.GetComponent<InventoryMenuUI>() : null;
        return foundInventory != null;
    }

    private void SetPromptVisible(bool visible)
    {
        if (visible)
            EnsurePromptUI();

        if (promptRoot == null)
            return;

        promptRoot.gameObject.SetActive(visible);

        if (visible && promptLabel != null)
            promptLabel.text = GetPromptText();
    }

    private void EnsurePromptUI()
    {
        if (promptRoot != null)
            return;

        GameObject canvasObject = new GameObject($"{name} Pickup Prompt Canvas");
        promptCanvas = canvasObject.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        promptCanvas.sortingOrder = 50;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject promptObject = new GameObject("Pickup Prompt", typeof(RectTransform));
        promptRoot = promptObject.GetComponent<RectTransform>();
        promptRoot.SetParent(promptCanvas.transform, false);
        promptRoot.anchorMin = new Vector2(0.5f, 0f);
        promptRoot.anchorMax = new Vector2(0.5f, 0f);
        promptRoot.pivot = new Vector2(0.5f, 0.5f);
        promptRoot.anchoredPosition = promptScreenPosition;
        promptRoot.sizeDelta = new Vector2(360f, 72f);

        Image background = promptObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject buttonObject = new GameObject("Button Prompt", typeof(RectTransform));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(promptRoot, false);
        buttonRect.anchorMin = new Vector2(0f, 0.5f);
        buttonRect.anchorMax = new Vector2(0f, 0.5f);
        buttonRect.pivot = new Vector2(0f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(16f, 0f);
        buttonRect.sizeDelta = new Vector2(48f, 48f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.95f, 0.8f, 0.35f, 0.96f);

        TMP_Text buttonText = CreatePromptText("Button Label", buttonRect, gamepadPrompt);
        buttonText.color = Color.black;
        buttonText.fontSize = 28f;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.rectTransform.anchorMin = Vector2.zero;
        buttonText.rectTransform.anchorMax = Vector2.one;
        buttonText.rectTransform.offsetMin = Vector2.zero;
        buttonText.rectTransform.offsetMax = Vector2.zero;

        promptLabel = CreatePromptText("Prompt Label", promptRoot, GetPromptText());
        promptLabel.fontSize = 18f;
        promptLabel.alignment = TextAlignmentOptions.MidlineLeft;
        promptLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
        promptLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
        promptLabel.rectTransform.offsetMin = new Vector2(78f, 8f);
        promptLabel.rectTransform.offsetMax = new Vector2(-16f, -8f);

        promptRoot.gameObject.SetActive(false);
    }

    private TMP_Text CreatePromptText(string objectName, Transform parent, string text)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);

        TMP_Text tmpText = textObject.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.color = Color.white;
        tmpText.raycastTarget = false;
        return tmpText;
    }

    private string GetPromptText()
    {
        string itemDisplayName = GetPromptItemName();
        string actionText = string.IsNullOrWhiteSpace(promptText) ? "Pick up" : promptText;
        return $"{itemDisplayName}\n{gamepadPrompt} / {keyboardPrompt} {actionText}";
    }

    private string GetPromptItemName()
    {
        if (!string.IsNullOrWhiteSpace(itemName))
            return quantity > 1 ? $"{itemName} x{quantity}" : itemName;

        if (itemKind == InventoryItemKind.Ammo)
            return quantity > 1 ? $"{ammoType} Ammo x{quantity}" : $"{ammoType} Ammo";

        if (itemKind == InventoryItemKind.Weapon)
            return weaponType.ToString();

        if (itemKind == InventoryItemKind.Attachment)
            return attachmentType.ToString();

        return "Item";
    }

    private InventoryEntry CreateEntry()
    {
        switch (itemKind)
        {
            case InventoryItemKind.Weapon:
                return InventoryEntry.CreateWeapon(weaponType, size, Vector2Int.zero);
            case InventoryItemKind.Attachment:
                return InventoryEntry.CreateAttachment(attachmentType, size, Vector2Int.zero);
            case InventoryItemKind.Misc:
                return InventoryEntry.CreateMisc(itemName, size, quantity, maxStack, Vector2Int.zero);
            default:
                return InventoryEntry.CreateAmmoStack(ammoType, quantity, maxStack, Vector2Int.zero);
        }
    }
}
