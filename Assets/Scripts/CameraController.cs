using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MobileCameraController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float dragSpeed = 0.5f;
    [SerializeField] private float movementSmoothness = 10f;
    
    // Define os limites X e Y a partir do centro
    [Header("Limits")]
    [SerializeField] private Vector2 movementLimits = new Vector2(10f, 10f);

    [Header("Zoom (Pinch / Scroll)")]
    [SerializeField] private float zoomSpeed = 0.1f;
    [SerializeField] private float mouseScrollSpeed = 2f;
    [SerializeField] private float zoomSmoothness = 10f;
    [SerializeField] private float minOrthographicSize = 2f;
    [SerializeField] private float maxOrthographicSize = 10f;

    private Camera cam;
    private Vector3 targetPosition;
    private float targetZoom;
    private Vector3 startPosition;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        targetPosition = transform.position;
        startPosition = transform.position; // Define o centro da área permitida
        targetZoom = cam.orthographicSize;
    }

    private void Update()
    {
        HandleInput();
        ApplyMovementLimits();
        SmoothUpdate();
    }

    private void HandleInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetZoom -= scroll * mouseScrollSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minOrthographicSize, maxOrthographicSize);
        }

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
        else if (Application.isEditor && Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f)
            {
                float speedMultiplier = cam.orthographicSize * 0.05f;
                targetPosition -= new Vector3(mouseX, mouseY, 0) * dragSpeed * speedMultiplier;
            }
        }
    }

    // Trava a posição alvo dentro da caixa delimitadora
    private void ApplyMovementLimits()
    {
        targetPosition.x = Mathf.Clamp(targetPosition.x, startPosition.x - movementLimits.x, startPosition.x + movementLimits.x);
        targetPosition.y = Mathf.Clamp(targetPosition.y, startPosition.y - movementLimits.y, startPosition.y + movementLimits.y);
    }

    private void SmoothUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * movementSmoothness);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSmoothness);
    }

    // Desenha a área de limite no editor
    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? startPosition : transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, new Vector3(movementLimits.x * 2, movementLimits.y * 2, 0));
    }
}