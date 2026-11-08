using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Ground-plane navigation with terrain-aware pan and zoom limits.</summary>
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public sealed class MobileCameraController : MonoBehaviour
{
    [SerializeField] private MeshRenderer ground;
    [SerializeField] private float minOrthographicSize = 28;
    [SerializeField] private float maxOrthographicSize = 68;
    [SerializeField] private float scrollSensitivity = 1.6f;
    [SerializeField] private float zoomSmoothness = 12;
    [SerializeField] private float dragThreshold = 20;

    private Camera view;
    private Bounds terrainBounds;
    private Bounds focusBounds;
    private Vector3 cameraOffset;
    private Vector3 focus;
    private Vector3 targetFocus;
    private float targetZoom;
    private Vector2 pressPosition;
    private Vector2 lastPosition;
    private bool tracking;
    private bool dragging;

    public Vector3 GroundFocus => focus;
    public float MinimumZoom => minOrthographicSize;
    public float MaximumZoom => EffectiveMaximumZoom();

    private void Awake()
    {
        view = GetComponent<Camera>();
        var spawner = FindAnyObjectByType<TreeSpawner>();
        if (ground == null && spawner != null) ground = spawner.plane;
        if (ground == null || !view.orthographic || Mathf.Abs(transform.forward.y) < .01f)
        {
            enabled = false;
            return;
        }
        terrainBounds = ground.bounds;
        focusBounds = terrainBounds;
        // Large border blockers identify the scenery surrounding the usable terrain.
        if (spawner != null && spawner.spawnBlockers != null)
        foreach (var blocker in spawner.spawnBlockers)
        {
            if (blocker == null) continue;
            var b = blocker.bounds;
            var min = focusBounds.min; var max = focusBounds.max;
            if (b.size.x > terrainBounds.size.x * .85f)
            {
                if (b.center.z < terrainBounds.center.z - terrainBounds.size.z * .25f)
                    min.z = Mathf.Max(min.z, b.max.z);
                else if (b.center.z > terrainBounds.center.z + terrainBounds.size.z * .25f)
                    max.z = Mathf.Min(max.z, b.min.z);
            }
            if (b.size.z > terrainBounds.size.z * .85f)
            {
                if (b.center.x < terrainBounds.center.x - terrainBounds.size.x * .25f)
                    min.x = Mathf.Max(min.x, b.max.x);
                else if (b.center.x > terrainBounds.center.x + terrainBounds.size.x * .25f)
                    max.x = Mathf.Min(max.x, b.min.x);
            }
            focusBounds.SetMinMax(min, max);
        }
        GroundPoint(new Vector2(Screen.width * .5f, Screen.height * .5f), out focus);
        cameraOffset = transform.position - focus;
        targetFocus = focus;
        targetZoom = Mathf.Clamp(view.orthographicSize, Mathf.Min(minOrthographicSize, MaximumZoom), MaximumZoom);
        view.orthographicSize = targetZoom;
        focus = targetFocus = ClampFocus(focus, targetZoom);
        transform.position = focus + cameraOffset;
    }

    private void OnEnable() { tracking = dragging = false; }

    private void Update()
    {
        if (view == null || StepController.WorldInputBlocked) return;
        HandleInput();
        targetZoom = Mathf.Clamp(targetZoom, Mathf.Min(minOrthographicSize, MaximumZoom), MaximumZoom);
        targetFocus = ClampFocus(targetFocus, targetZoom);
        float blend = 1 - Mathf.Exp(-zoomSmoothness * Time.deltaTime);
        view.orthographicSize = Mathf.Lerp(view.orthographicSize, targetZoom, blend);
        focus = ClampFocus(Vector3.Lerp(focus, targetFocus, blend), view.orthographicSize);
        transform.position = focus + cameraOffset;
    }

    private void HandleInput()
    {
        if (Input.touchCount > 1)
        {
            var a = Input.GetTouch(0); var b = Input.GetTouch(1);
            tracking = dragging = false;
            if (OverUI(a.fingerId) || OverUI(b.fingerId)) return;
            float previous = ((a.position - a.deltaPosition) - (b.position - b.deltaPosition)).magnitude;
            float current = (a.position - b.position).magnitude;
            if (current > 1 && previous > 1) SetZoom(targetZoom * previous / current);
            return;
        }
        if (Input.touchCount == 1)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) BeginDrag(touch.position, OverUI(touch.fingerId));
            else if (touch.phase == TouchPhase.Moved) Drag(touch.position);
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                tracking = dragging = false;
            return;
        }
        if (!OverUI(-1))
        {
            float wheel = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(wheel) > .001f) SetZoom(targetZoom * Mathf.Exp(-wheel * scrollSensitivity));
        }
        if (Input.GetMouseButtonDown(0)) BeginDrag(Input.mousePosition, OverUI(-1));
        if (Input.GetMouseButton(0)) Drag(Input.mousePosition);
        if (Input.GetMouseButtonUp(0)) tracking = dragging = false;
    }

    private static bool OverUI(int finger)
    {
        if (EventSystem.current == null) return false;
        return finger < 0 ? EventSystem.current.IsPointerOverGameObject() :
            EventSystem.current.IsPointerOverGameObject(finger);
    }

    private void BeginDrag(Vector2 position, bool overUI)
    {
        tracking = !overUI; dragging = false;
        pressPosition = lastPosition = position;
    }

    private void Drag(Vector2 position)
    {
        if (!tracking) return;
        if (!dragging && Vector2.Distance(position, pressPosition) < dragThreshold) return;
        dragging = true;
        if (GroundPoint(lastPosition, out var from) && GroundPoint(position, out var to))
            targetFocus += from - to;
        lastPosition = position;
    }

    private bool GroundPoint(Vector2 screen, out Vector3 point)
    {
        var plane = new Plane(Vector3.up, terrainBounds.center);
        var ray = view.ScreenPointToRay(screen);
        if (plane.Raycast(ray, out float distance)) { point = ray.GetPoint(distance); return true; }
        point = terrainBounds.center; return false;
    }

    private Vector3 GroundProjection(Vector3 offset)
        => offset - transform.forward * (offset.y / transform.forward.y);

    private Vector2 ViewExtents(float zoom)
    {
        var horizontal = GroundProjection(transform.right * (zoom * view.aspect));
        var vertical = GroundProjection(transform.up * zoom);
        return new Vector2(Mathf.Abs(horizontal.x) + Mathf.Abs(vertical.x),
            Mathf.Abs(horizontal.z) + Mathf.Abs(vertical.z));
    }

    private float EffectiveMaximumZoom()
    {
        if (view == null) return maxOrthographicSize;
        var unit = ViewExtents(1);
        return Mathf.Min(maxOrthographicSize, terrainBounds.extents.x / Mathf.Max(.001f, unit.x),
            terrainBounds.extents.z / Mathf.Max(.001f, unit.y));
    }

    private Vector3 ClampFocus(Vector3 point, float zoom)
    {
        var extent = ViewExtents(zoom);
        float minX = Mathf.Max(focusBounds.min.x, terrainBounds.min.x + extent.x);
        float maxX = Mathf.Min(focusBounds.max.x, terrainBounds.max.x - extent.x);
        float minZ = Mathf.Max(focusBounds.min.z, terrainBounds.min.z + extent.y);
        float maxZ = Mathf.Min(focusBounds.max.z, terrainBounds.max.z - extent.y);
        point.x = minX <= maxX ? Mathf.Clamp(point.x, minX, maxX) : terrainBounds.center.x;
        point.z = minZ <= maxZ ? Mathf.Clamp(point.z, minZ, maxZ) : terrainBounds.center.z;
        point.y = terrainBounds.center.y;
        return point;
    }

    public void SetZoom(float size)
        => targetZoom = Mathf.Clamp(size, Mathf.Min(minOrthographicSize, MaximumZoom), MaximumZoom);

    public void FocusOn(Vector3 point, bool immediate = false)
    {
        targetFocus = ClampFocus(point, targetZoom);
        if (!immediate) return;
        view.orthographicSize = targetZoom;
        focus = targetFocus;
        transform.position = focus + cameraOffset;
    }
}
