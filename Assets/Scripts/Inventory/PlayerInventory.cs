using System.Collections.Generic;
using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Case Size")]
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 6;

    [Header("Starting Ammo")]
    [SerializeField] private List<StartingAmmoStack> startingAmmo = new List<StartingAmmoStack>
    {
        new StartingAmmoStack(AmmoType.Handgun, 30, 50),
        new StartingAmmoStack(AmmoType.Shotgun, 12, 15),
        new StartingAmmoStack(AmmoType.MachineGun, 60, 100),
        new StartingAmmoStack(AmmoType.Rifle, 10, 10)
    };

    [Header("Starting Weapons")]
    [SerializeField] private List<StartingWeaponItem> startingWeapons = new List<StartingWeaponItem>
    {
        new StartingWeaponItem(WeaponType.Handgun, 3, 2),
        new StartingWeaponItem(WeaponType.Shotgun, 5, 2),
        new StartingWeaponItem(WeaponType.MachineGun, 3, 2),
        new StartingWeaponItem(WeaponType.SniperRifle, 6, 2)
    };

    [Header("Starting Attachments")]
    [SerializeField] private List<StartingAttachmentItem> startingAttachments = new List<StartingAttachmentItem>
    {
        new StartingAttachmentItem(AttachmentType.SniperScope, 1, 1)
    };

    [Header("Runtime")]
    [SerializeField] private List<InventoryEntry> entries = new List<InventoryEntry>();

    public IReadOnlyList<InventoryEntry> Entries => entries;
    public int Width => width;
    public int Height => height;
    public event Action Changed;

    private bool hasInitialised;

    private void Awake()
    {
        InitialiseStartingInventory();
    }

    public void InitialiseStartingInventory()
    {
        if (entries.Count > 0)
        {
            hasInitialised = true;
            return;
        }

        if (hasInitialised)
            return;

        for (int i = 0; i < startingAmmo.Count; i++)
        {
            StartingAmmoStack stack = startingAmmo[i];
            AddAmmo(stack.ammoType, stack.amount, stack.maxStack);
        }

        for (int i = 0; i < startingWeapons.Count; i++)
        {
            StartingWeaponItem weapon = startingWeapons[i];
            AddWeapon(weapon.weaponType, new InventoryItemSize(weapon.width, weapon.height));
        }

        for (int i = 0; i < startingAttachments.Count; i++)
        {
            StartingAttachmentItem attachment = startingAttachments[i];
            AddAttachment(attachment.attachmentType, new InventoryItemSize(attachment.width, attachment.height));
        }

        hasInitialised = true;
    }

    public int GetAmmoCount(AmmoType ammoType)
    {
        int total = 0;

        foreach (InventoryEntry entry in entries)
        {
            if (entry.IsAmmo && entry.AmmoType == ammoType)
                total += entry.Quantity;
        }

        return total;
    }

    public int AddAmmo(AmmoType ammoType, int amount, int maxStack)
    {
        int remaining = amount;

        foreach (InventoryEntry entry in entries)
        {
            if (entry.IsAmmo && entry.AmmoType == ammoType)
            {
                remaining = entry.AddToStack(remaining);
                NotifyChanged();
            }

            if (remaining <= 0)
                return 0;
        }

        while (remaining > 0 && TryFindFreeTile(out Vector2Int position))
        {
            int stackAmount = Mathf.Min(remaining, maxStack);
            entries.Add(InventoryEntry.CreateAmmoStack(ammoType, stackAmount, maxStack, position));
            remaining -= stackAmount;
            NotifyChanged();
        }

        return remaining;
    }

    public bool TryAddEntryAuto(InventoryEntry entry)
    {
        if (entry == null)
            return false;

        if (entry.IsAmmo)
        {
            return TryAddAmmoEntryAuto(entry);
        }

        if (!TryFindFreeArea(entry.Size, out Vector2Int position))
            return false;

        entries.Add(entry.CloneAt(position));
        NotifyChanged();
        return true;
    }

    public bool TryAddEntryAt(InventoryEntry entry, Vector2Int position)
    {
        if (entry == null || !CanPlaceSize(entry.Size, position))
            return false;

        entries.Add(entry.CloneAt(position));
        NotifyChanged();
        return true;
    }

    public bool AddWeapon(WeaponType weaponType, InventoryItemSize size)
    {
        if (HasWeapon(weaponType) || !TryFindFreeArea(size, out Vector2Int position))
            return false;

        entries.Add(InventoryEntry.CreateWeapon(weaponType, size, position));
        NotifyChanged();
        return true;
    }

    public bool AddAttachment(AttachmentType attachmentType, InventoryItemSize size)
    {
        if (HasAttachment(attachmentType) || !TryFindFreeArea(size, out Vector2Int position))
            return false;

        entries.Add(InventoryEntry.CreateAttachment(attachmentType, size, position));
        NotifyChanged();
        return true;
    }

    public bool HasWeapon(WeaponType weaponType)
    {
        foreach (InventoryEntry entry in entries)
        {
            if (entry.IsWeapon && entry.WeaponType == weaponType)
                return true;
        }

        return false;
    }

    public string GetEntryDisplayName(InventoryEntry entry)
    {
        if (entry == null)
            return string.Empty;

        if (entry.IsAmmo)
            return $"{entry.AmmoType} Ammo x{entry.Quantity}";

        if (entry.IsWeapon)
            return entry.WeaponType.ToString();

        if (entry.IsAttachment)
            return entry.AttachmentType.ToString();

        return entry.Quantity > 1 ? $"{entry.ItemName} x{entry.Quantity}" : entry.ItemName;
    }

    public bool HasAttachment(AttachmentType attachmentType)
    {
        foreach (InventoryEntry entry in entries)
        {
            if (entry.IsAttachment && entry.AttachmentType == attachmentType)
                return true;
        }

        return false;
    }

    public int ConsumeAmmo(AmmoType ammoType, int amount)
    {
        int remaining = amount;
        int consumed = 0;

        for (int i = entries.Count - 1; i >= 0 && remaining > 0; i--)
        {
            InventoryEntry entry = entries[i];

            if (!entry.IsAmmo || entry.AmmoType != ammoType)
                continue;

            int removed = entry.RemoveFromStack(remaining);
            consumed += removed;
            remaining -= removed;

            if (entry.Quantity <= 0)
                entries.RemoveAt(i);
        }

        if (consumed > 0)
            NotifyChanged();

        return consumed;
    }

    public InventoryEntry GetEntryAt(Vector2Int tile)
    {
        foreach (InventoryEntry entry in entries)
        {
            if (DoesEntryOccupyTile(entry, tile))
                return entry;
        }

        return null;
    }

    public int CountOverlappingEntries(InventoryEntry movingEntry, Vector2Int position)
    {
        int count = 0;

        foreach (InventoryEntry entry in entries)
        {
            if (entry == movingEntry)
                continue;

            if (DoEntriesOverlap(position, movingEntry.Size, entry.Position, entry.Size))
                count++;
        }

        return count;
    }

    public InventoryEntry GetSingleOverlappingEntry(InventoryEntry movingEntry, Vector2Int position)
    {
        InventoryEntry overlappingEntry = null;

        foreach (InventoryEntry entry in entries)
        {
            if (entry == movingEntry)
                continue;

            if (!DoEntriesOverlap(position, movingEntry.Size, entry.Position, entry.Size))
                continue;

            if (overlappingEntry != null)
                return null;

            overlappingEntry = entry;
        }

        return overlappingEntry;
    }

    public bool IsWithinBounds(InventoryEntry entry, Vector2Int position)
    {
        if (entry == null)
            return false;

        return position.x >= 0 &&
            position.y >= 0 &&
            position.x + entry.Size.width <= width &&
            position.y + entry.Size.height <= height;
    }

    public bool TryMoveEntry(InventoryEntry entry, Vector2Int newPosition)
    {
        if (entry == null || !CanPlaceEntry(entry, newPosition))
            return false;

        entry.SetPosition(newPosition);
        NotifyChanged();
        return true;
    }

    public bool TrySwapEntries(
        InventoryEntry firstEntry,
        Vector2Int firstPosition,
        InventoryEntry secondEntry,
        Vector2Int secondPosition)
    {
        if (firstEntry == null || secondEntry == null || firstEntry == secondEntry)
            return false;

        if (!CanPlacePair(firstEntry, firstPosition, secondEntry, secondPosition))
            return false;

        firstEntry.SetPosition(firstPosition);
        secondEntry.SetPosition(secondPosition);
        NotifyChanged();
        return true;
    }

    public bool CanSwapEntries(
        InventoryEntry firstEntry,
        Vector2Int firstPosition,
        InventoryEntry secondEntry,
        Vector2Int secondPosition)
    {
        if (firstEntry == null || secondEntry == null || firstEntry == secondEntry)
            return false;

        return CanPlacePair(firstEntry, firstPosition, secondEntry, secondPosition);
    }

    public bool TryRotateEntry(InventoryEntry entry)
    {
        if (entry == null)
            return false;

        InventoryItemSize rotatedSize = entry.Size.Rotated();

        if (!CanPlaceEntry(entry, entry.Position, rotatedSize))
            return false;

        entry.Rotate();
        NotifyChanged();
        return true;
    }

    public bool CanPlaceEntry(InventoryEntry movingEntry, Vector2Int position)
    {
        return CanPlaceEntry(movingEntry, position, movingEntry != null ? movingEntry.Size : default);
    }

    private bool CanPlaceEntry(InventoryEntry movingEntry, Vector2Int position, InventoryItemSize size)
    {
        if (movingEntry == null)
            return false;

        if (position.x < 0 ||
            position.y < 0 ||
            position.x + size.width > width ||
            position.y + size.height > height)
        {
            return false;
        }

        foreach (InventoryEntry entry in entries)
        {
            if (entry == movingEntry)
                continue;

            if (DoEntriesOverlap(position, size, entry.Position, entry.Size))
                return false;
        }

        return true;
    }

    private bool CanPlacePair(
        InventoryEntry firstEntry,
        Vector2Int firstPosition,
        InventoryEntry secondEntry,
        Vector2Int secondPosition)
    {
        if (!IsWithinBounds(firstEntry, firstPosition) || !IsWithinBounds(secondEntry, secondPosition))
            return false;

        if (DoEntriesOverlap(firstPosition, firstEntry.Size, secondPosition, secondEntry.Size))
            return false;

        foreach (InventoryEntry entry in entries)
        {
            if (entry == firstEntry || entry == secondEntry)
                continue;

            if (DoEntriesOverlap(firstPosition, firstEntry.Size, entry.Position, entry.Size))
                return false;

            if (DoEntriesOverlap(secondPosition, secondEntry.Size, entry.Position, entry.Size))
                return false;
        }

        return true;
    }

    private bool TryFindFreeTile(out Vector2Int position)
    {
        return TryFindFreeArea(new InventoryItemSize(2, 1), out position);
    }

    private bool TryAddAmmoEntryAuto(InventoryEntry entry)
    {
        int remaining = entry.Quantity;
        List<InventoryEntry> changedStacks = new List<InventoryEntry>();
        List<int> previousQuantities = new List<int>();
        List<InventoryEntry> newStacks = new List<InventoryEntry>();

        foreach (InventoryEntry existingEntry in entries)
        {
            if (!existingEntry.IsAmmo || existingEntry.AmmoType != entry.AmmoType || existingEntry.FreeStackSpace <= 0)
                continue;

            changedStacks.Add(existingEntry);
            previousQuantities.Add(existingEntry.Quantity);
            remaining = existingEntry.AddToStack(remaining);

            if (remaining <= 0)
            {
                NotifyChanged();
                return true;
            }
        }

        while (remaining > 0 && TryFindFreeArea(entry.Size, out Vector2Int position))
        {
            int stackAmount = Mathf.Min(remaining, entry.MaxStack);
            InventoryEntry newStack = InventoryEntry.CreateAmmoStack(entry.AmmoType, stackAmount, entry.MaxStack, position);
            entries.Add(newStack);
            newStacks.Add(newStack);
            remaining -= stackAmount;
        }

        if (remaining <= 0)
        {
            NotifyChanged();
            return true;
        }

        for (int i = 0; i < changedStacks.Count; i++)
            changedStacks[i].SetQuantity(previousQuantities[i]);

        foreach (InventoryEntry newStack in newStacks)
            entries.Remove(newStack);

        return false;
    }

    private bool TryFindFreeArea(InventoryItemSize size, out Vector2Int position)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int candidate = new Vector2Int(x, y);

                if (CanPlaceSize(size, candidate))
                {
                    position = candidate;
                    return true;
                }
            }
        }

        position = Vector2Int.zero;
        return false;
    }

    private bool CanPlaceSize(InventoryItemSize size, Vector2Int position)
    {
        if (position.x < 0 ||
            position.y < 0 ||
            position.x + size.width > width ||
            position.y + size.height > height)
        {
            return false;
        }

        foreach (InventoryEntry entry in entries)
        {
            if (DoEntriesOverlap(position, size, entry.Position, entry.Size))
                return false;
        }

        return true;
    }

    private bool DoesEntryOccupyTile(InventoryEntry entry, Vector2Int tile)
    {
        Vector2Int position = entry.Position;

        return tile.x >= position.x &&
            tile.x < position.x + entry.Size.width &&
            tile.y >= position.y &&
            tile.y < position.y + entry.Size.height;
    }

    private bool DoEntriesOverlap(
        Vector2Int firstPosition,
        InventoryItemSize firstSize,
        Vector2Int secondPosition,
        InventoryItemSize secondSize)
    {
        return firstPosition.x < secondPosition.x + secondSize.width &&
            firstPosition.x + firstSize.width > secondPosition.x &&
            firstPosition.y < secondPosition.y + secondSize.height &&
            firstPosition.y + firstSize.height > secondPosition.y;
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }
}

[System.Serializable]
public struct StartingAmmoStack
{
    public AmmoType ammoType;
    public int amount;
    public int maxStack;

    public StartingAmmoStack(AmmoType ammoType, int amount, int maxStack)
    {
        this.ammoType = ammoType;
        this.amount = amount;
        this.maxStack = maxStack;
    }
}

[System.Serializable]
public struct StartingWeaponItem
{
    public WeaponType weaponType;
    public int width;
    public int height;

    public StartingWeaponItem(WeaponType weaponType, int width, int height)
    {
        this.weaponType = weaponType;
        this.width = width;
        this.height = height;
    }
}

[System.Serializable]
public struct StartingAttachmentItem
{
    public AttachmentType attachmentType;
    public int width;
    public int height;

    public StartingAttachmentItem(AttachmentType attachmentType, int width, int height)
    {
        this.attachmentType = attachmentType;
        this.width = width;
        this.height = height;
    }
}
