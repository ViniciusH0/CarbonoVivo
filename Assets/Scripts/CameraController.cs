using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MobileCameraController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float dragSpeed = 0.5f;
    [SerializeField] private float movementSmoothness = 10f;
    
    [Header("Zoom (Pinch / Scroll)")]
    [SerializeField] private float zoomSpeed = 0.1f;
    [SerializeField] private float mouseScrollSpeed = 2f;
    [SerializeField] private float zoomSmoothness = 10f;
    [SerializeField] private float minOrthographicSize = 2f;
    [SerializeField] private float maxOrthographicSize = 10f;

    private Camera cam;
    private Vector3 targetPosition;
    private float targetZoom;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        targetPosition = transform.position;
        targetZoom = cam.orthographicSize;
    }

    private void Update()
    {
        HandleInput();
        SmoothUpdate();
    }

    private void HandleInput()
    {
        // Suporte para Scroll do Mouse (Unity Editor / PC)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetZoom -= scroll * mouseScrollSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minOrthographicSize, maxOrthographicSize);
        }

        // Touch - Movimentação (1 dedo)
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;
                float speedMultiplier = cam.orthographicSize * 0.002f;
                
                targetPosition -= new Vector3(delta.x, delta.y, 0) * dragSpeed * speedMultiplier;
            }
        }
        // Touch - Zoom por Pinça (2 dedos)
        else if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            targetZoom += deltaMagnitudeDiff * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minOrthographicSize, maxOrthographicSize);
        }
        // Suporte para Mouse Drag (Apenas no Unity Editor)
        else if (Application.isEditor && Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f)
            {
                float speedMultiplier = cam.orthographicSize * 0.05f; // Ajustado para a sensibilidade do mouse
                targetPosition -= new Vector3(mouseX, mouseY, 0) * dragSpeed * speedMultiplier;
            }
        }
    }

    private void SmoothUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * movementSmoothness);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSmoothness);
    }
}