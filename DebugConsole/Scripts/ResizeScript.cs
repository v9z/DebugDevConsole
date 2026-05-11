using UnityEngine;
using UnityEngine.EventSystems;

public class ResizeScript : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] private RectTransform windowToResize;
    [SerializeField] private float maxWidth = 3032;
    private float minWidth;

    private Vector2 startPointerPosition;
    private Vector2 startSize;
    private Vector2 startAnchoredPosition;
    private float aspectRatio;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (windowToResize == null)
        {
            return;
        }

        RectTransform parentRect = windowToResize.parent as RectTransform;

        if (parentRect == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out startPointerPosition
        );

        startSize = windowToResize.rect.size;
        startAnchoredPosition = windowToResize.anchoredPosition;

        aspectRatio = startSize.x / startSize.y;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (windowToResize == null)
        {
            return;
        }

        RectTransform parentRect = windowToResize.parent as RectTransform;

        if (parentRect == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 currentPointerPosition
        );

        Vector2 dragDelta = currentPointerPosition - startPointerPosition;

        minWidth = maxWidth / 2;

        float newWidth = Mathf.Clamp(startSize.x + dragDelta.x, minWidth, maxWidth);
        float newHeight = newWidth / aspectRatio;

        Vector2 newSize = new Vector2(newWidth, newHeight);

        Vector2 sizeDifference = newSize - startSize;

        windowToResize.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newSize.x);
        windowToResize.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newSize.y);

        windowToResize.anchoredPosition = startAnchoredPosition + new Vector2(
            sizeDifference.x * 0.5f,
            -sizeDifference.y * 0.5f
        );
    }
}