using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class InventoryMenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Canvas canvas;

    [Header("Prefab Layout")]
    [SerializeField] private RectTransform overlayPrefab;
    [SerializeField] private RectTransform overlayRoot;
    [SerializeField] private RectTransform panel;
    [SerializeField] private RectTransform gridRoot;
    [SerializeField] private RectTransform pendingGridRoot;
    [SerializeField] private RectTransform cursor;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private bool buildMissingLayout = true;

    [Header("Input")]
    [SerializeField] private string toggleActionName = "Inventory";
    [SerializeField] private string selectActionName = "InventorySelect";
    [SerializeField] private string cancelActionName = "InventoryCancel";
    [SerializeField] private string rotateActionName = "InventoryRotate";
    [SerializeField] private string moveActionName = "Move";

    [Header("Layout")]
    [SerializeField] private int tileSize = 56;
    [SerializeField] private int tileGap = 4;
    [SerializeField] private int panelPadding = 28;
    [SerializeField] private Vector2 liftedItemOffset = new Vector2(0f, 0f);
    [SerializeField, Range(1f, 1.2f)] private float liftedItemScale = 1.06f;
    [SerializeField] private Vector2 liftedShadowDistance = new Vector2(8f, -8f);
    [SerializeField] private int pendingGridWidth = 3;
    [SerializeField] private int pendingGridHeight = 6;

    [Header("Colours")]
    [SerializeField] private Color overlayColour = new Color(0f, 0f, 0f, 0.65f);
    [SerializeField] private Color panelColour = new Color(0.08f, 0.085f, 0.075f, 0.96f);
    [SerializeField] private Color tileColour = new Color(0.18f, 0.19f, 0.17f, 0.95f);
    [SerializeField] private Color itemColour = new Color(0.45f, 0.42f, 0.25f, 0.98f);
    [SerializeField] private Color selectedColour = new Color(0.95f, 0.8f, 0.35f, 0.95f);
    [SerializeField] private Color invalidColour = new Color(0.9f, 0.18f, 0.12f, 0.85f);

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private AudioClip placeClip;
    [SerializeField, Range(0f, 1f)] private float uiAudioVolume = 1f;

    private readonly Dictionary<InventoryEntry, InventoryItemUI> itemViews = new Dictionary<InventoryEntry, InventoryItemUI>();
    private readonly Dictionary<InventoryEntry, InventoryItemUI> pendingItemViews = new Dictionary<InventoryEntry, InventoryItemUI>();
    private readonly List<Image> tileImages = new List<Image>();
    private readonly List<Image> pendingTileImages = new List<Image>();
    private readonly List<InventoryEntry> pendingEntries = new List<InventoryEntry>();
    private readonly List<LootableItem> pendingLootSources = new List<LootableItem>();

    private InputAction toggleAction;
    private InputAction selectAction;
    private InputAction cancelAction;
    private InputAction rotateAction;
    private InputAction moveAction;

    private InventoryEntry selectedEntry;
    private Vector2Int cursorPosition;
    private Vector2Int selectedEntryOriginalPosition;
    private bool isOpen;
    private bool wasTogglePressed;
    private bool wasSelectPressed;
    private bool wasCancelPressed;
    private bool wasRotatePressed;
    private float nextControllerMoveTime;
    private RectTransform acceptButtonRect;
    private RectTransform closeButtonRect;
    private RectTransform discardDialog;
    private TMP_Text discardListText;

    public bool IsOpen => isOpen;

    private float TileStride => tileSize + tileGap;
    private bool HasPendingLoot => pendingEntries.Count > 0;

    private void Awake()
    {
        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();

        if (inventory == null)
            inventory = FindAnyObjectByType<PlayerInventory>();

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (playerInput == null)
            playerInput = FindAnyObjectByType<PlayerInput>();

        if (canvas == null)
            canvas = FindAnyObjectByType<Canvas>();

        if (canvas == null)
            canvas = CreateCanvas();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        EnsureEventSystem();
        FindActions();
        ResolvePrefabReferences();
        BuildUI();
        SetOpen(false);
    }

    private void OnEnable()
    {
        if (inventory != null)
            inventory.Changed += HandleInventoryChanged;

        toggleAction?.Enable();
        selectAction?.Enable();
        cancelAction?.Enable();
        rotateAction?.Enable();
        moveAction?.Enable();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.Changed -= HandleInventoryChanged;

        toggleAction?.Disable();
        selectAction?.Disable();
        cancelAction?.Disable();
        rotateAction?.Disable();
        moveAction?.Disable();
    }

    private void Update()
    {
        bool isTogglePressed = IsPressed(toggleAction);

        if (isTogglePressed && !wasTogglePressed)
            SetOpen(!isOpen);

        wasTogglePressed = isTogglePressed;

        if (!isOpen)
            return;

        bool isCancelPressed = IsPressed(cancelAction);

        if (isCancelPressed && !wasCancelPressed)
        {
            if (selectedEntry != null)
            {
                selectedEntry = null;
                Refresh();
            }
            else
            {
                SetOpen(false);
            }
        }

        wasCancelPressed = isCancelPressed;

        HandleControllerSelection();
        HandleControllerRotation();
        HandleControllerMove();
        UpdateCursorVisual();
    }

    public void BeginMouseDrag(InventoryEntry entry)
    {
        selectedEntry = entry;
        selectedEntryOriginalPosition = entry.Position;
        cursorPosition = entry.Position;
        PlayUIClip(pickupClip);
    }

    public void EndMouseDrag(InventoryEntry entry, Vector2 screenPosition)
    {
        if (TryScreenToTile(screenPosition, out Vector2Int tile))
            TryPlaceSelectedEntry(tile);

        selectedEntry = null;
        Refresh();
    }

    public void OpenForLoot(InventoryEntry entry, LootableItem source)
    {
        if (entry == null)
            return;

        pendingEntries.Add(entry.CloneAt(GetNextPendingPosition(entry.Size)));
        pendingLootSources.Add(source);
        SetOpen(true);
        Refresh();
    }

    public Color GetMouseDragColour(Vector2 screenPosition)
    {
        if (!TryScreenToTile(screenPosition, out Vector2Int tile))
            return invalidColour;

        return CanPlaceSelectedAt(tile) ? selectedColour : invalidColour;
    }

    public bool TryScreenToTile(Vector2 screenPosition, out Vector2Int tile)
    {
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRoot, screenPosition, uiCamera, out Vector2 localPoint))
        {
            tile = Vector2Int.zero;
            return false;
        }

        Vector2 topLeftPoint = LocalPointToTopLeftGridPoint(localPoint);
        int x = Mathf.FloorToInt(topLeftPoint.x / TileStride);
        int y = Mathf.FloorToInt(topLeftPoint.y / TileStride);
        tile = new Vector2Int(x, y);

        return x >= 0 && y >= 0 && x < inventory.Width && y < inventory.Height;
    }

    private void FindActions()
    {
        if (playerInput == null)
            return;

        toggleAction = playerInput.actions.FindAction(toggleActionName, false);
        selectAction = playerInput.actions.FindAction(selectActionName, false);
        cancelAction = playerInput.actions.FindAction(cancelActionName, false);
        rotateAction = playerInput.actions.FindAction(rotateActionName, false);
        moveAction = playerInput.actions.FindAction(moveActionName, false);
    }

    private void SetOpen(bool open)
    {
        if (!open && HasPendingLoot)
            CancelPendingLoot();

        isOpen = open;

        if (overlayRoot != null)
            overlayRoot.gameObject.SetActive(isOpen);

        Cursor.visible = isOpen;
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;

        if (isOpen)
        {
            selectedEntry = null;
            cursorPosition = Vector2Int.zero;
            Refresh();
        }
    }

    private void Refresh()
    {
        UpdatePanelLayout();
        UpdateTileViews();
        UpdatePendingTileViews();
        UpdateItemViews();
        UpdatePendingItemViews();
        UpdatePendingControls();
        UpdateCursorVisual();
    }

    private void HandleControllerSelection()
    {
        bool isSelectPressed = IsPressed(selectAction);

        if (isSelectPressed && !wasSelectPressed)
        {
            if (selectedEntry == null)
            {
                selectedEntry = inventory.GetEntryAt(cursorPosition);
                if (selectedEntry != null)
                {
                    selectedEntryOriginalPosition = selectedEntry.Position;
                    PlayUIClip(pickupClip);
                    UpdateItemViews();
                }
            }
            else
            {
                if (TryPlaceSelectedEntry(cursorPosition))
                {
                    selectedEntry = null;
                    Refresh();
                }
            }
        }

        wasSelectPressed = isSelectPressed;
    }

    private void HandleControllerMove()
    {
        if (Time.unscaledTime < nextControllerMoveTime || moveAction == null)
            return;

        Vector2 input = moveAction.ReadValue<Vector2>();

        if (input.magnitude < 0.5f)
            return;

        Vector2Int direction;

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            direction = input.x > 0f ? Vector2Int.right : Vector2Int.left;
        else
            direction = input.y > 0f ? Vector2Int.up : Vector2Int.down;

        cursorPosition = GetCursorPositionAfterMove(direction);
        cursorPosition.x = Mathf.Clamp(cursorPosition.x, 0, inventory.Width - 1);
        cursorPosition.y = Mathf.Clamp(cursorPosition.y, 0, inventory.Height - 1);

        if (selectedEntry != null)
            UpdateItemViews();
        else
            Refresh();

        nextControllerMoveTime = Time.unscaledTime + 0.15f;
    }

    private void HandleControllerRotation()
    {
        bool isRotatePressed = IsPressed(rotateAction);

        if (isRotatePressed && !wasRotatePressed && selectedEntry != null)
        {
            if (inventory.TryRotateEntry(selectedEntry))
                UpdateItemViews();
        }

        wasRotatePressed = isRotatePressed;
    }

    private Vector2Int GetCursorPositionAfterMove(Vector2Int direction)
    {
        InventoryEntry hoveredEntry = selectedEntry == null ? inventory.GetEntryAt(cursorPosition) : null;

        if (hoveredEntry == null)
            return cursorPosition + new Vector2Int(direction.x, -direction.y);

        Vector2Int position = hoveredEntry.Position;
        InventoryItemSize size = hoveredEntry.Size;

        if (direction == Vector2Int.right)
            return new Vector2Int(position.x + size.width, cursorPosition.y);

        if (direction == Vector2Int.left)
            return new Vector2Int(position.x - 1, cursorPosition.y);

        if (direction == Vector2Int.up)
            return new Vector2Int(cursorPosition.x, position.y - 1);

        return new Vector2Int(cursorPosition.x, position.y + size.height);
    }

    private void BuildUI()
    {
        if (overlayRoot == null)
        {
            if (!buildMissingLayout)
                return;

            overlayRoot = CreateRect("Inventory Overlay", canvas.transform);
            AddImage(overlayRoot.gameObject, overlayColour);
        }

        overlayRoot.anchorMin = Vector2.zero;
        overlayRoot.anchorMax = Vector2.one;
        overlayRoot.offsetMin = Vector2.zero;
        overlayRoot.offsetMax = Vector2.zero;

        if (panel == null)
        {
            panel = CreateRect("Inventory Panel", overlayRoot);
            AddImage(panel.gameObject, panelColour);
        }

        float gridWidth = GetGridPixelWidth(inventory.Width);
        float gridHeight = GetGridPixelHeight(inventory.Height);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;

        if (headerText == null)
            headerText = CreateText("Inventory Header", panel, "Inventory");

        RectTransform headerRect = headerText.rectTransform;
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -8f);
        headerRect.sizeDelta = new Vector2(-panelPadding * 2f, 28f);

        if (gridRoot == null)
            gridRoot = CreateRect("Inventory Grid", panel);

        gridRoot.anchorMin = new Vector2(0.5f, 1f);
        gridRoot.anchorMax = new Vector2(0.5f, 1f);
        gridRoot.pivot = new Vector2(0f, 1f);
        gridRoot.sizeDelta = new Vector2(gridWidth, gridHeight);

        if (pendingGridRoot == null)
            pendingGridRoot = CreateRect("Pending Loot Grid", panel);

        ConfigureTopLeftChild(pendingGridRoot);
        pendingGridRoot.sizeDelta = new Vector2(GetGridPixelWidth(pendingGridWidth), GetGridPixelHeight(pendingGridHeight));

        if (cursor == null)
        {
            cursor = CreateRect("Inventory Cursor", gridRoot);
            AddImage(cursor.gameObject, new Color(selectedColour.r, selectedColour.g, selectedColour.b, 0.35f));
        }

        ConfigureTopLeftChild(cursor);
        cursor.sizeDelta = new Vector2(tileSize, tileSize);

        RebuildMainTilesIfNeeded();
        RebuildPendingTilesIfNeeded();

        CreatePendingControls();
        UpdatePanelLayout();
    }

    private void CreateTiles()
    {
        tileImages.Clear();

        for (int y = 0; y < inventory.Height; y++)
        {
            for (int x = 0; x < inventory.Width; x++)
            {
                RectTransform tile = CreateRect($"Tile {x},{y}", gridRoot);
                ConfigureTopLeftChild(tile);
                tile.sizeDelta = new Vector2(tileSize, tileSize);
                tile.anchoredPosition = TileToAnchoredPosition(new Vector2Int(x, y));
                tileImages.Add(AddImage(tile.gameObject, tileColour));
            }
        }
    }

    private void CreatePendingTiles()
    {
        pendingTileImages.Clear();

        for (int y = 0; y < pendingGridHeight; y++)
        {
            for (int x = 0; x < pendingGridWidth; x++)
            {
                RectTransform tile = CreateRect($"Pending Tile {x},{y}", pendingGridRoot);
                ConfigureTopLeftChild(tile);
                tile.sizeDelta = new Vector2(tileSize, tileSize);
                tile.anchoredPosition = TileToAnchoredPosition(new Vector2Int(x, y));
                pendingTileImages.Add(AddImage(tile.gameObject, tileColour));
            }
        }
    }

    private void CreatePendingControls()
    {
        if (acceptButtonRect == null)
            acceptButtonRect = CreateButton("Accept Loot Button", panel, "Accept", AcceptPendingLoot);

        if (closeButtonRect == null)
            closeButtonRect = CreateButton("Close Loot Button", panel, "Close", ClosePendingLoot);

        if (discardDialog == null)
            CreateDiscardDialog();
    }

    private void RebuildMainTilesIfNeeded()
    {
        if (gridRoot == null)
            return;

        CollectExistingTiles();

        int expectedTileCount = inventory.Width * inventory.Height;

        if (CountUsableTileImages(tileImages) == expectedTileCount)
        {
            PositionMainTiles();
            return;
        }

        RemoveGeneratedTiles(gridRoot, "Tile ");

        if (buildMissingLayout)
            CreateTiles();
    }

    private void RebuildPendingTilesIfNeeded()
    {
        if (pendingGridRoot == null)
            return;

        CollectExistingPendingTiles();

        int expectedTileCount = pendingGridWidth * pendingGridHeight;

        if (CountUsableTileImages(pendingTileImages) == expectedTileCount)
        {
            PositionPendingTiles();
            return;
        }

        RemoveGeneratedTiles(pendingGridRoot, "Pending Tile ");

        if (buildMissingLayout)
            CreatePendingTiles();
    }

    private void PositionMainTiles()
    {
        PositionTiles(tileImages, inventory.Width);
    }

    private void PositionPendingTiles()
    {
        PositionTiles(pendingTileImages, pendingGridWidth);
    }

    private void PositionTiles(List<Image> images, int gridWidth)
    {
        for (int i = 0; i < images.Count; i++)
        {
            Image image = images[i];

            if (image == null)
                continue;

            if (!image.TryGetComponent(out RectTransform tileRect))
                continue;

            int x = i % gridWidth;
            int y = i / gridWidth;
            ConfigureTopLeftChild(tileRect);
            tileRect.sizeDelta = new Vector2(tileSize, tileSize);
            tileRect.anchoredPosition = TileToAnchoredPosition(new Vector2Int(x, y));
        }
    }

    private void RemoveGeneratedTiles(RectTransform parent, string namePrefix)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            if (!child.name.StartsWith(namePrefix))
                continue;

            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    private int CountUsableTileImages(List<Image> images)
    {
        int count = 0;

        foreach (Image image in images)
        {
            if (image != null)
                count++;
        }

        return count;
    }

    private void UpdateTileViews()
    {
        foreach (Image tileImage in tileImages)
        {
            if (tileImage != null)
                tileImage.color = tileColour;
        }
    }

    private void UpdatePendingTileViews()
    {
        bool showPending = HasPendingLoot;

        if (pendingGridRoot != null)
            pendingGridRoot.gameObject.SetActive(showPending);

        foreach (Image tileImage in pendingTileImages)
        {
            if (tileImage != null)
                tileImage.color = tileColour;
        }
    }

    private void UpdateItemViews()
    {
        CleanupOrphanedItemViews();

        List<InventoryEntry> missingEntries = new List<InventoryEntry>();

        foreach (InventoryEntry entry in itemViews.Keys)
        {
            if (!InventoryContains(entry))
                missingEntries.Add(entry);
        }

        foreach (InventoryEntry entry in missingEntries)
        {
            if (itemViews.TryGetValue(entry, out InventoryItemUI view) && view != null)
            {
                view.gameObject.SetActive(false);
                Destroy(view.gameObject);
            }

            itemViews.Remove(entry);
        }

        foreach (InventoryEntry entry in inventory.Entries)
        {
            if (!itemViews.TryGetValue(entry, out InventoryItemUI view) || view == null)
                CreateItemView(entry);
            else
                UpdateItemView(entry, view);
        }

        if (selectedEntry != null && itemViews.TryGetValue(selectedEntry, out InventoryItemUI selectedView) && selectedView != null)
            selectedView.transform.SetAsLastSibling();
    }

    private void UpdatePendingItemViews()
    {
        List<InventoryEntry> missingEntries = new List<InventoryEntry>();

        foreach (InventoryEntry entry in pendingItemViews.Keys)
        {
            if (!pendingEntries.Contains(entry))
                missingEntries.Add(entry);
        }

        foreach (InventoryEntry entry in missingEntries)
        {
            if (pendingItemViews.TryGetValue(entry, out InventoryItemUI view) && view != null)
                Destroy(view.gameObject);

            pendingItemViews.Remove(entry);
        }

        foreach (InventoryEntry entry in pendingEntries)
        {
            if (!pendingItemViews.TryGetValue(entry, out InventoryItemUI view) || view == null)
                CreatePendingItemView(entry);
            else
                view.UpdateView(TileToAnchoredPosition(entry.Position), GetItemViewSize(entry.Size), selectedColour, GetEntryLabel(entry), false, liftedItemScale);
        }
    }

    private void CreateItemView(InventoryEntry entry)
    {
        RectTransform itemRect = CreateRect(entry.ItemName, gridRoot);
        ConfigureTopLeftChild(itemRect);
        itemRect.sizeDelta = GetItemViewSize(entry.Size);
        itemRect.anchoredPosition = TileToAnchoredPosition(entry.Position);

        Image image = AddImage(itemRect.gameObject, entry == selectedEntry ? selectedColour : itemColour);
        Shadow shadow = itemRect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = liftedShadowDistance;
        shadow.enabled = false;

        TMP_Text label = CreateText("Label", itemRect, GetEntryLabel(entry));
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 14f;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(4f, 4f);
        label.rectTransform.offsetMax = new Vector2(-4f, -4f);

        InventoryItemUI itemUI = itemRect.gameObject.AddComponent<InventoryItemUI>();
        itemUI.Initialise(this, entry, image, shadow);
        itemViews[entry] = itemUI;
        UpdateItemView(entry, itemUI);
    }

    private void CreatePendingItemView(InventoryEntry entry)
    {
        RectTransform itemRect = CreateRect(entry.ItemName, pendingGridRoot);
        ConfigureTopLeftChild(itemRect);
        itemRect.sizeDelta = GetItemViewSize(entry.Size);
        itemRect.anchoredPosition = TileToAnchoredPosition(entry.Position);

        Image image = AddImage(itemRect.gameObject, selectedColour);
        Shadow shadow = itemRect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = liftedShadowDistance;
        shadow.enabled = false;

        TMP_Text label = CreateText("Label", itemRect, GetEntryLabel(entry));
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 14f;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(4f, 4f);
        label.rectTransform.offsetMax = new Vector2(-4f, -4f);

        InventoryItemUI itemUI = itemRect.gameObject.AddComponent<InventoryItemUI>();
        itemUI.Initialise(this, entry, image, shadow);
        itemUI.SetDragEnabled(false);
        pendingItemViews[entry] = itemUI;
        itemUI.UpdateView(TileToAnchoredPosition(entry.Position), GetItemViewSize(entry.Size), selectedColour, GetEntryLabel(entry), false, liftedItemScale);
    }

    private void UpdateItemView(InventoryEntry entry, InventoryItemUI itemUI)
    {
        Vector2Int viewPosition = entry == selectedEntry ? cursorPosition : entry.Position;
        Color colour = GetItemColour(entry);
        Vector2 anchoredPosition = TileToAnchoredPosition(viewPosition);
        bool isLifted = entry == selectedEntry;

        if (isLifted)
            anchoredPosition += liftedItemOffset;

        itemUI.UpdateView(anchoredPosition, GetItemViewSize(entry.Size), colour, GetEntryLabel(entry), isLifted, liftedItemScale);

        if (isLifted)
            itemUI.transform.SetAsLastSibling();
    }

    private Color GetItemColour(InventoryEntry entry)
    {
        if (entry != selectedEntry)
            return itemColour;

        return CanPlaceSelectedAt(cursorPosition) ? selectedColour : invalidColour;
    }

    private string GetEntryLabel(InventoryEntry entry)
    {
        if (entry.IsAmmo)
            return $"{entry.AmmoType}\n{entry.Quantity}";

        if (entry.IsWeapon)
            return entry.WeaponType.ToString();

        if (entry.IsAttachment)
            return entry.AttachmentType.ToString();

        return $"{entry.ItemName}\n{entry.Quantity}";
    }

    private void HandleInventoryChanged()
    {
        if (isOpen)
            Refresh();
    }

    private bool InventoryContains(InventoryEntry entry)
    {
        foreach (InventoryEntry inventoryEntry in inventory.Entries)
        {
            if (inventoryEntry == entry)
                return true;
        }

        return false;
    }

    private void CleanupOrphanedItemViews()
    {
        if (gridRoot == null)
            return;

        InventoryItemUI[] views = gridRoot.GetComponentsInChildren<InventoryItemUI>(true);

        foreach (InventoryItemUI view in views)
        {
            if (view == null)
                continue;

            if (InventoryContains(view.Entry))
                continue;

            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }
    }

    private void UpdateCursorVisual()
    {
        if (cursor == null)
            return;

        InventoryEntry displayEntry = selectedEntry != null ? selectedEntry : inventory.GetEntryAt(cursorPosition);
        Vector2Int displayPosition = displayEntry != null ? displayEntry.Position : cursorPosition;
        InventoryItemSize displaySize = displayEntry != null ? displayEntry.Size : new InventoryItemSize(1, 1);

        cursor.anchoredPosition = TileToAnchoredPosition(displayPosition);
        cursor.sizeDelta = GetItemViewSize(displaySize);
        cursor.SetAsLastSibling();

        if (cursor.TryGetComponent(out Image cursorImage))
        {
            bool canPlaceSelected = selectedEntry == null || CanPlaceSelectedAt(cursorPosition);
            Color cursorColour = canPlaceSelected ? selectedColour : invalidColour;
            cursorColour.a = 0.35f;
            cursorImage.color = cursorColour;
        }

        if (headerText != null)
        {
            string selectedText = selectedEntry != null
                ? $"Moving: {selectedEntry.ItemName}"
                : displayEntry != null ? displayEntry.ItemName : "Inventory";
            headerText.text = selectedText;
        }
    }

    private bool TryPlaceSelectedEntry(Vector2Int targetPosition)
    {
        if (selectedEntry == null || !inventory.IsWithinBounds(selectedEntry, targetPosition))
            return false;

        int overlapCount = inventory.CountOverlappingEntries(selectedEntry, targetPosition);

        if (overlapCount == 0)
        {
            bool moved = inventory.TryMoveEntry(selectedEntry, targetPosition);

            if (moved)
                PlayUIClip(placeClip);

            return moved;
        }

        if (overlapCount == 1)
        {
            InventoryEntry swappedEntry = inventory.GetSingleOverlappingEntry(selectedEntry, targetPosition);

            if (swappedEntry == null)
                return false;

            Vector2Int oldPosition = selectedEntryOriginalPosition;
            Vector2Int swappedPosition = swappedEntry.Position;

            if (!inventory.TrySwapEntries(selectedEntry, targetPosition, swappedEntry, oldPosition))
                return false;

            selectedEntry = swappedEntry;
            selectedEntryOriginalPosition = swappedEntry.Position;
            cursorPosition = swappedPosition;
            PlayUIClip(placeClip);
            PlayUIClip(pickupClip);
            Refresh();
            return true;
        }

        return false;
    }

    private bool CanPlaceSelectedAt(Vector2Int targetPosition)
    {
        if (selectedEntry == null || !inventory.IsWithinBounds(selectedEntry, targetPosition))
            return false;

        int overlapCount = inventory.CountOverlappingEntries(selectedEntry, targetPosition);

        if (overlapCount == 0)
            return true;

        if (overlapCount > 1)
            return false;

        InventoryEntry swappedEntry = inventory.GetSingleOverlappingEntry(selectedEntry, targetPosition);
        return inventory.CanSwapEntries(selectedEntry, targetPosition, swappedEntry, selectedEntryOriginalPosition);
    }

    private void AcceptPendingLoot()
    {
        for (int i = pendingEntries.Count - 1; i >= 0; i--)
        {
            if (!inventory.TryAddEntryAuto(pendingEntries[i]))
                continue;

            CompletePendingSource(i, true);
            pendingEntries.RemoveAt(i);
        }

        if (!HasPendingLoot)
        {
            SetOpen(false);
            return;
        }

        ShowDiscardDialog();
        Refresh();
    }

    private void CancelPendingLoot()
    {
        CompleteAllPendingSources(false);
        ClearPendingLoot();
        HideDiscardDialog();
    }

    private void ClosePendingLoot()
    {
        CancelPendingLoot();
        SetOpen(false);
    }

    private void ConfirmDiscardPendingLoot()
    {
        CompleteAllPendingSources(true);
        ClearPendingLoot();
        HideDiscardDialog();
        SetOpen(false);
    }

    private void HideDiscardDialog()
    {
        if (discardDialog != null)
            discardDialog.gameObject.SetActive(false);
    }

    private void ShowDiscardDialog()
    {
        if (discardDialog == null)
            return;

        discardDialog.gameObject.SetActive(true);

        if (discardListText == null)
            return;

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.AppendLine("These items do not fit and will be destroyed:");

        foreach (InventoryEntry entry in pendingEntries)
            builder.AppendLine(inventory.GetEntryDisplayName(entry));

        discardListText.text = builder.ToString();
    }

    private void UpdatePendingControls()
    {
        bool showPending = HasPendingLoot;

        if (acceptButtonRect != null)
            acceptButtonRect.gameObject.SetActive(showPending);

        if (closeButtonRect != null)
            closeButtonRect.gameObject.SetActive(showPending);

        if (!showPending)
            HideDiscardDialog();
    }

    private void UpdatePanelLayout()
    {
        if (panel == null || inventory == null)
            return;

        float gridWidth = GetGridPixelWidth(inventory.Width);
        float gridHeight = GetGridPixelHeight(inventory.Height);
        float pendingWidth = GetGridPixelWidth(pendingGridWidth);
        float pendingHeight = GetGridPixelHeight(pendingGridHeight);
        float gap = HasPendingLoot ? panelPadding : 0f;
        float contentWidth = gridWidth + (HasPendingLoot ? pendingWidth + gap : 0f);
        float contentHeight = Mathf.Max(gridHeight, HasPendingLoot ? pendingHeight : 0f);
        float buttonHeight = HasPendingLoot ? 40f : 0f;

        if (gridRoot != null)
            gridRoot.sizeDelta = new Vector2(gridWidth, gridHeight);

        if (pendingGridRoot != null)
            pendingGridRoot.sizeDelta = new Vector2(pendingWidth, pendingHeight);

        panel.sizeDelta = new Vector2(
            contentWidth + panelPadding * 2f,
            contentHeight + panelPadding * 2f + 34f + buttonHeight
        );

        float left = -contentWidth * 0.5f;
        gridRoot.anchoredPosition = new Vector2(left, -panelPadding - 28f);

        if (pendingGridRoot != null)
            pendingGridRoot.anchoredPosition = new Vector2(left + gridWidth + gap, -panelPadding - 28f);

        float controlsY = -panelPadding - 28f - contentHeight - 8f;

        if (acceptButtonRect != null)
            acceptButtonRect.anchoredPosition = new Vector2(left + contentWidth - 184f, controlsY);

        if (closeButtonRect != null)
            closeButtonRect.anchoredPosition = new Vector2(left + contentWidth - 88f, controlsY);
    }

    private Vector2Int GetNextPendingPosition(InventoryItemSize size)
    {
        for (int y = 0; y < pendingGridHeight; y++)
        {
            for (int x = 0; x < pendingGridWidth; x++)
            {
                Vector2Int candidate = new Vector2Int(x, y);

                if (!IsPendingPositionFree(size, candidate))
                    continue;

                return candidate;
            }
        }

        return Vector2Int.zero;
    }

    private bool IsPendingPositionFree(InventoryItemSize size, Vector2Int position)
    {
        if (position.x < 0 ||
            position.y < 0 ||
            position.x + size.width > pendingGridWidth ||
            position.y + size.height > pendingGridHeight)
        {
            return false;
        }

        foreach (InventoryEntry entry in pendingEntries)
        {
            if (position.x < entry.Position.x + entry.Size.width &&
                position.x + size.width > entry.Position.x &&
                position.y < entry.Position.y + entry.Size.height &&
                position.y + size.height > entry.Position.y)
            {
                return false;
            }
        }

        return true;
    }

    private void CompletePendingSource(int index, bool destroySource)
    {
        if (index < 0 || index >= pendingLootSources.Count)
            return;

        if (pendingLootSources[index] != null)
            pendingLootSources[index].CompletePickup(destroySource);

        pendingLootSources.RemoveAt(index);
    }

    private void CompleteAllPendingSources(bool destroySources)
    {
        foreach (LootableItem source in pendingLootSources)
        {
            if (source != null)
                source.CompletePickup(destroySources);
        }
    }

    private void ClearPendingLoot()
    {
        pendingEntries.Clear();
        pendingLootSources.Clear();

        foreach (InventoryItemUI view in pendingItemViews.Values)
        {
            if (view != null)
                Destroy(view.gameObject);
        }

        pendingItemViews.Clear();
        Refresh();
    }

    private void PlayUIClip(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip, uiAudioVolume);
    }

    private Vector2 GetItemViewSize(InventoryItemSize size)
    {
        return new Vector2(
            size.width * tileSize + (size.width - 1) * tileGap,
            size.height * tileSize + (size.height - 1) * tileGap
        );
    }

    private float GetGridPixelWidth(int gridWidth)
    {
        return gridWidth * tileSize + (gridWidth - 1) * tileGap;
    }

    private float GetGridPixelHeight(int gridHeight)
    {
        return gridHeight * tileSize + (gridHeight - 1) * tileGap;
    }

    private Vector2 TileToAnchoredPosition(Vector2Int tile)
    {
        return new Vector2(tile.x * TileStride, -tile.y * TileStride);
    }

    private Vector2 LocalPointToTopLeftGridPoint(Vector2 localPoint)
    {
        Rect rect = gridRoot.rect;
        float x = localPoint.x + rect.width * gridRoot.pivot.x;
        float y = rect.height * (1f - gridRoot.pivot.y) - localPoint.y;
        return new Vector2(x, y);
    }

    private void ConfigureTopLeftChild(RectTransform rectTransform)
    {
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
    }

    private void ResolvePrefabReferences()
    {
        if (overlayRoot == null)
        {
            RectTransform ownRect = GetComponent<RectTransform>();

            if (ownRect != null)
                overlayRoot = ownRect;
        }

        if (overlayRoot == null && overlayPrefab != null)
        {
            overlayRoot = Instantiate(overlayPrefab, canvas.transform);
            overlayRoot.name = overlayPrefab.name;
        }

        if (panel == null)
            panel = FindChildRect("Inventory Panel");

        if (gridRoot == null)
            gridRoot = FindChildRect("Inventory Grid");

        if (pendingGridRoot == null)
            pendingGridRoot = FindChildRect("Pending Loot Grid");

        if (cursor == null)
            cursor = FindChildRect("Inventory Cursor");

        if (headerText == null)
        {
            Transform searchRoot = overlayRoot != null ? overlayRoot : transform;
            headerText = searchRoot.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private RectTransform FindChildRect(string childName)
    {
        Transform searchRoot = overlayRoot != null ? overlayRoot : transform;
        RectTransform[] rectTransforms = searchRoot.GetComponentsInChildren<RectTransform>(true);

        foreach (RectTransform rectTransform in rectTransforms)
        {
            if (rectTransform.name == childName)
                return rectTransform;
        }

        return null;
    }

    private void CollectExistingTiles()
    {
        tileImages.Clear();

        if (gridRoot == null)
            return;

        for (int i = 0; i < gridRoot.childCount; i++)
        {
            Transform child = gridRoot.GetChild(i);

            if (!child.name.StartsWith("Tile "))
                continue;

            if (child.TryGetComponent(out RectTransform tileRect))
                ConfigureTopLeftChild(tileRect);

            if (child.TryGetComponent(out Image image))
                InsertTileImageByName(tileImages, image, child.name, "Tile ", inventory.Width);
        }
    }

    private void CollectExistingPendingTiles()
    {
        pendingTileImages.Clear();

        if (pendingGridRoot == null)
            return;

        for (int i = 0; i < pendingGridRoot.childCount; i++)
        {
            Transform child = pendingGridRoot.GetChild(i);

            if (!child.name.StartsWith("Pending Tile "))
                continue;

            if (child.TryGetComponent(out RectTransform tileRect))
                ConfigureTopLeftChild(tileRect);

            if (child.TryGetComponent(out Image image))
                InsertTileImageByName(pendingTileImages, image, child.name, "Pending Tile ", pendingGridWidth);
        }
    }

    private void InsertTileImageByName(List<Image> images, Image image, string objectName, string prefix, int gridWidth)
    {
        if (!TryGetTileIndex(objectName, prefix, gridWidth, out int tileIndex))
        {
            images.Add(image);
            return;
        }

        while (images.Count <= tileIndex)
            images.Add(null);

        images[tileIndex] = image;
    }

    private bool TryGetTileIndex(string objectName, string prefix, int gridWidth, out int tileIndex)
    {
        tileIndex = -1;

        if (gridWidth <= 0 || !objectName.StartsWith(prefix))
            return false;

        string coordinateText = objectName.Substring(prefix.Length);
        string[] parts = coordinateText.Split(',');

        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out int x) ||
            !int.TryParse(parts[1], out int y) ||
            x < 0 ||
            y < 0)
        {
            return false;
        }

        tileIndex = y * gridWidth + x;
        return true;
    }

    private bool IsPressed(InputAction action)
    {
        return action != null && action.ReadValue<float>() > 0.2f;
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Inventory Canvas");
        Canvas createdCanvas = canvasObject.AddComponent<Canvas>();
        createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        return createdCanvas;
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }

    private Image AddImage(GameObject target, Color colour)
    {
        Image image = target.AddComponent<Image>();
        image.color = colour;
        return image;
    }

    private RectTransform CreateButton(string objectName, Transform parent, string text, UnityEngine.Events.UnityAction action)
    {
        RectTransform rectTransform = CreateRect(objectName, parent);
        ConfigureTopLeftChild(rectTransform);
        rectTransform.sizeDelta = new Vector2(88f, 32f);

        Image image = AddImage(rectTransform.gameObject, selectedColour);
        Button button = rectTransform.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        TMP_Text label = CreateText("Label", rectTransform, text);
        label.color = Color.black;
        label.fontSize = 16f;
        label.raycastTarget = false;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;

        return rectTransform;
    }

    private void CreateDiscardDialog()
    {
        discardDialog = CreateRect("Discard Pending Loot Dialog", overlayRoot != null ? overlayRoot : panel);
        discardDialog.anchorMin = new Vector2(0.5f, 0.5f);
        discardDialog.anchorMax = new Vector2(0.5f, 0.5f);
        discardDialog.pivot = new Vector2(0.5f, 0.5f);
        discardDialog.sizeDelta = new Vector2(420f, 260f);
        discardDialog.anchoredPosition = Vector2.zero;
        AddImage(discardDialog.gameObject, panelColour);

        discardListText = CreateText("Discard List", discardDialog, string.Empty);
        discardListText.alignment = TextAlignmentOptions.TopLeft;
        discardListText.fontSize = 16f;
        discardListText.rectTransform.anchorMin = new Vector2(0f, 0f);
        discardListText.rectTransform.anchorMax = new Vector2(1f, 1f);
        discardListText.rectTransform.offsetMin = new Vector2(18f, 58f);
        discardListText.rectTransform.offsetMax = new Vector2(-18f, -18f);

        RectTransform yesButton = CreateButton("Discard Yes Button", discardDialog, "Yes", ConfirmDiscardPendingLoot);
        yesButton.anchorMin = new Vector2(1f, 0f);
        yesButton.anchorMax = new Vector2(1f, 0f);
        yesButton.pivot = new Vector2(1f, 0f);
        yesButton.anchoredPosition = new Vector2(-110f, 18f);

        RectTransform noButton = CreateButton("Discard No Button", discardDialog, "No", HideDiscardDialog);
        noButton.anchorMin = new Vector2(1f, 0f);
        noButton.anchorMax = new Vector2(1f, 0f);
        noButton.pivot = new Vector2(1f, 0f);
        noButton.anchoredPosition = new Vector2(-18f, 18f);

        discardDialog.gameObject.SetActive(false);
    }

    private TMP_Text CreateText(string objectName, Transform parent, string text)
    {
        RectTransform rectTransform = CreateRect(objectName, parent);
        TMP_Text tmpText = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.color = Color.white;
        tmpText.fontSize = 18f;
        tmpText.alignment = TextAlignmentOptions.Center;
        return tmpText;
    }
}
