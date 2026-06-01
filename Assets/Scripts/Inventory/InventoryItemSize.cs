using System;

[Serializable]
public struct InventoryItemSize
{
    public int width;
    public int height;

    public InventoryItemSize(int width, int height)
    {
        this.width = width;
        this.height = height;
    }
}
