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
    [SerializeField] private bool isAmmo;
    [SerializeField] private AmmoType ammoType;

    public string ItemName => itemName;
    public Vector2Int Position => position;
    public InventoryItemSize Size => size;
    public bool Stackable => stackable;
    public int Quantity => quantity;
    public int MaxStack => maxStack;
    public bool IsAmmo => isAmmo;
    public AmmoType AmmoType => ammoType;
    public int FreeStackSpace => stackable ? Mathf.Max(0, maxStack - quantity) : 0;

    public static InventoryEntry CreateAmmoStack(AmmoType ammoType, int amount, int maxStack, Vector2Int position)
    {
        return new InventoryEntry
        {
            itemName = $"{ammoType} Ammo",
            position = position,
            size = new InventoryItemSize(1, 1),
            stackable = true,
            quantity = Mathf.Max(0, amount),
            maxStack = Mathf.Max(1, maxStack),
            isAmmo = true,
            ammoType = ammoType
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
}
