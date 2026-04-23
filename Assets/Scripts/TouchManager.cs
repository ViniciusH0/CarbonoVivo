using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.StandaloneInputModule;
public interface IClickable { void OnClicked(); }
public class TouchManager : MonoBehaviour
{
    public LayerMask soilLayer;
    public LayerMask treeLayer;
    public ScrollCam scrollCam;
    public float dragThreshold = 10f;
    private Vector2 startPos;
    private bool isDragging = false;
    private bool touchStartedOverUI = false;
    void Update()
    {
        if (Input.touchCount <= 0) return;

        Touch touch = Input.GetTouch(0);
        switch (touch.phase)
        {
            case TouchPhase.Began:
                startPos = touch.position;
                isDragging = false;

                touchStartedOverUI = EventSystem.current.IsPointerOverGameObject(touch.fingerId);
                break;
            case TouchPhase.Moved:
                if (!isDragging && Vector2.Distance(touch.position, startPos) > dragThreshold)
                    isDragging = true;

                if (isDragging)
                    scrollCam.MoveCamera(touch.deltaPosition.y * 1.5f);
                break;

            case TouchPhase.Ended:
                if (!isDragging && !touchStartedOverUI)
                    HandleWorldClick(touch.position);

                isDragging = false;
                touchStartedOverUI = false;
                break;
            case TouchPhase.Canceled:
                isDragging = false;
                touchStartedOverUI = false;
                break;
        }

    }

    void HandleWorldClick(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        LayerMask layerToUse = LayerMask.GetMask(""); // default vazio

        switch (GridManager.Instance.currentMode)
        {
            case InputMode.Planting:
                layerToUse = soilLayer;
                break;

            case InputMode.Interacting:
                layerToUse = treeLayer;
                break;

            default:
                return;
        }

        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, layerToUse);

        if (hit.collider != null)
        {
            hit.collider.GetComponent<IClickable>()?.OnClicked();
        }
    }


}
