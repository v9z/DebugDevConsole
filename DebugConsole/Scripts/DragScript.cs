using UnityEngine;
using UnityEngine.EventSystems;

public class DragScript : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] private RectTransform windowToMove;

    private Vector2 pointerOffset;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (windowToMove == null)
        {
            return;
        }

        RectTransform parentRect = windowToMove.parent as RectTransform;

        if (parentRect == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition
        );

        pointerOffset = localPointerPosition - windowToMove.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (windowToMove == null)
        {
            return;
        }

        RectTransform parentRect = windowToMove.parent as RectTransform;

        if (parentRect == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition
        );

        Vector2 targetPosition = localPointerPosition - pointerOffset;
        windowToMove.anchoredPosition = ClampToParent(targetPosition, parentRect);
    }

    private Vector2 ClampToParent(Vector2 targetPosition, RectTransform parentRect)
    {
        Vector2 parentSize = parentRect.rect.size;

        Vector2 scaledWindowSize = new Vector2(
            windowToMove.rect.width * windowToMove.localScale.x,
            windowToMove.rect.height * windowToMove.localScale.y
        );

        Vector2 pivot = windowToMove.pivot;

        float minX = -parentSize.x * 0.5f + scaledWindowSize.x * pivot.x;
        float maxX = parentSize.x * 0.5f - scaledWindowSize.x * (1f - pivot.x);

        float minY = -parentSize.y * 0.5f + scaledWindowSize.y * pivot.y;
        float maxY = parentSize.y * 0.5f - scaledWindowSize.y * (1f - pivot.y);

        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);

        return targetPosition;
    }
}