using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private InventoryMenuUI menu;
    private InventoryEntry entry;
    private Image image;
    private Shadow shadow;
    private TMP_Text label;
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 originalAnchoredPosition;
    private bool canDrag = true;

    public InventoryEntry Entry => entry;

    public void Initialise(InventoryMenuUI inventoryMenu, InventoryEntry inventoryEntry, Image itemImage, Shadow itemShadow)
    {
        menu = inventoryMenu;
        entry = inventoryEntry;
        image = itemImage;
        shadow = itemShadow;
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        label = GetComponentInChildren<TMP_Text>(true);
    }

    public void SetDragEnabled(bool enabled)
    {
        canDrag = enabled;

        if (image != null)
            image.raycastTarget = enabled;
    }

    public void UpdateView(Vector2 anchoredPosition, Vector2 size, Color colour, string labelText, bool isLifted, float liftedScale)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (label == null)
            label = GetComponentInChildren<TMP_Text>(true);

        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
        rectTransform.localScale = isLifted ? Vector3.one * liftedScale : Vector3.one;

        if (shadow != null)
            shadow.enabled = isLifted;

        if (image != null)
            image.color = colour;

        if (label != null)
            label.text = labelText;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canDrag)
            return;

        originalAnchoredPosition = rectTransform.anchoredPosition;
        transform.SetAsLastSibling();
        rectTransform.localScale = Vector3.one * 1.06f;
        if (shadow != null)
            shadow.enabled = true;
        image.raycastTarget = false;
        menu.BeginMouseDrag(entry);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag)
            return;

        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
        rectTransform.anchoredPosition += eventData.delta / scaleFactor;

        if (image != null)
            image.color = menu.GetMouseDragColour(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canDrag)
            return;

        image.raycastTarget = true;
        rectTransform.anchoredPosition = originalAnchoredPosition;
        rectTransform.localScale = Vector3.one;
        if (shadow != null)
            shadow.enabled = false;
        menu.EndMouseDrag(entry, eventData.position);
    }
}
