using System;
using UnityEngine;

[Serializable]
public class InventoryEntry
{
    [SerializeField] private string itemName;
    [SerializeField] private Vector2Int position;
    [SerializeField] private InventoryItemSize size = new InventoryItemSize(1, 1);
    [SerializeField] private bool stackable = true;
    [SerializeField] private int quantity = 1;
    [SerializeField] private int maxStack = 10;
    [SerializeField] private InventoryItemKind itemKind = InventoryItemKind.Ammo;
    [SerializeField] private bool isAmmo;
    [SerializeField] private AmmoType ammoType;
    [SerializeField] private WeaponType weaponType;
    [SerializeField] private AttachmentType attachmentType;

    public string ItemName => itemName;
    public Vector2Int Position => position;
    public InventoryItemSize Size => size;
    public bool Stackable => stackable;
    public int Quantity => quantity;
    public int MaxStack => maxStack;
    public InventoryItemKind ItemKind => itemKind;
    public bool IsAmmo => isAmmo;
    public AmmoType AmmoType => ammoType;
    public bool IsWeapon => itemKind == InventoryItemKind.Weapon;
    public WeaponType WeaponType => weaponType;
    public bool IsAttachment => itemKind == InventoryItemKind.Attachment;
    public AttachmentType AttachmentType => attachmentType;
    public int FreeStackSpace => stackable ? Mathf.Max(0, maxStack - quantity) : 0;

    public static InventoryEntry CreateAmmoStack(AmmoType ammoType, int amount, int maxStack, Vector2Int position)
    {
        return new InventoryEntry
        {
            itemName = $"{ammoType} Ammo",
            position = position,
            size = new InventoryItemSize(2, 1),
            stackable = true,
            quantity = Mathf.Max(0, amount),
            maxStack = Mathf.Max(1, maxStack),
            itemKind = InventoryItemKind.Ammo,
            isAmmo = true,
            ammoType = ammoType
        };
    }

    public static InventoryEntry CreateWeapon(WeaponType weaponType, InventoryItemSize size, Vector2Int position)
    {
        return new InventoryEntry
        {
            itemName = weaponType.ToString(),
            position = position,
            size = size,
            stackable = false,
            quantity = 1,
            maxStack = 1,
            itemKind = InventoryItemKind.Weapon,
            weaponType = weaponType
        };
    }

    public static InventoryEntry CreateAttachment(AttachmentType attachmentType, InventoryItemSize size, Vector2Int position)
    {
        return new InventoryEntry
        {
            itemName = attachmentType.ToString(),
            position = position,
            size = size,
            stackable = false,
            quantity = 1,
            maxStack = 1,
            itemKind = InventoryItemKind.Attachment,
            attachmentType = attachmentType
        };
    }

    public static InventoryEntry CreateMisc(string itemName, InventoryItemSize size, int quantity, int maxStack, Vector2Int position)
    {
        int safeMaxStack = Mathf.Max(1, maxStack);

        return new InventoryEntry
        {
            itemName = string.IsNullOrWhiteSpace(itemName) ? "Item" : itemName,
            position = position,
            size = size,
            stackable = safeMaxStack > 1,
            quantity = Mathf.Clamp(quantity, 1, safeMaxStack),
            maxStack = safeMaxStack,
            itemKind = InventoryItemKind.Misc
        };
    }

    public InventoryEntry CloneAt(Vector2Int newPosition)
    {
        return new InventoryEntry
        {
            itemName = itemName,
            position = newPosition,
            size = size,
            stackable = stackable,
            quantity = quantity,
            maxStack = maxStack,
            itemKind = itemKind,
            isAmmo = isAmmo,
            ammoType = ammoType,
            weaponType = weaponType,
            attachmentType = attachmentType
        };
    }

    public int AddToStack(int amount)
    {
        if (!stackable || amount <= 0)
            return amount;

        int amountToAdd = Mathf.Min(amount, FreeStackSpace);
        quantity += amountToAdd;

        return amount - amountToAdd;
    }

    public int RemoveFromStack(int amount)
    {
        if (amount <= 0)
            return 0;

        int amountToRemove = Mathf.Min(amount, quantity);
        quantity -= amountToRemove;

        return amountToRemove;
    }

    public void SetPosition(Vector2Int newPosition)
    {
        position = newPosition;
    }

    public void SetQuantity(int newQuantity)
    {
        quantity = Mathf.Clamp(newQuantity, 0, maxStack);
    }

    public void Rotate()
    {
        size = size.Rotated();
    }
}
