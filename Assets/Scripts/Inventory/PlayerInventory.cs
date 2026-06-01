using System.Collections.Generic;
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

    [Header("Runtime")]
    [SerializeField] private List<InventoryEntry> entries = new List<InventoryEntry>();

    public IReadOnlyList<InventoryEntry> Entries => entries;

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
                remaining = entry.AddToStack(remaining);

            if (remaining <= 0)
                return 0;
        }

        while (remaining > 0 && TryFindFreeTile(out Vector2Int position))
        {
            int stackAmount = Mathf.Min(remaining, maxStack);
            entries.Add(InventoryEntry.CreateAmmoStack(ammoType, stackAmount, maxStack, position));
            remaining -= stackAmount;
        }

        return remaining;
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

        return consumed;
    }

    private bool TryFindFreeTile(out Vector2Int position)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int candidate = new Vector2Int(x, y);

                if (!IsTileOccupied(candidate))
                {
                    position = candidate;
                    return true;
                }
            }
        }

        position = Vector2Int.zero;
        return false;
    }

    private bool IsTileOccupied(Vector2Int tile)
    {
        foreach (InventoryEntry entry in entries)
        {
            Vector2Int position = entry.Position;

            if (tile.x >= position.x &&
                tile.x < position.x + entry.Size.width &&
                tile.y >= position.y &&
                tile.y < position.y + entry.Size.height)
            {
                return true;
            }
        }

        return false;
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
