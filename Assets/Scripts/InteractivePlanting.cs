using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InteractivePlanting : MonoBehaviour
{
    public static InteractivePlanting Instance { get; private set; }

    [Header("Planting Cost")]
    [SerializeField] private float treeCost = 10f;
    public float TreeCost
    {
        get => treeCost;
        set => treeCost = Mathf.Max(0f, value);
    }
    private float lastPlantTime = -1f;

    [Header("UI Reference")]
    [SerializeField] private Button hudPlantButton;

    [Header("Input Filtering")]
    [SerializeField] private float maxTapMovement = 20f;
    private Vector2 touchStartPos;
    private bool isPointerDown = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (hudPlantButton == null)
        {
            var btnGo = GameObject.Find("UI_Canvas/Game/BottomActionsBar/PlantTreeButton");
            if (btnGo != null) hudPlantButton = btnGo.GetComponent<Button>();
        }

        if (hudPlantButton != null)
        {
            hudPlantButton.onClick.RemoveAllListeners();
            hudPlantButton.onClick.AddListener(PlantTreeRandomly);
        }
    }

    private void Update()
    {
        HandleTouchInput();
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    return;

                touchStartPos = touch.position;
                isPointerDown = true;
            }
            else if (touch.phase == TouchPhase.Ended && isPointerDown)
            {
                isPointerDown = false;
                if (Vector2.Distance(touch.position, touchStartPos) < maxTapMovement)
                {
                    PerformWorldRaycast(touch.position);
                }
            }
            else if (touch.phase == TouchPhase.Canceled)
            {
                isPointerDown = false;
            }
        }
        else if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                touchStartPos = Input.mousePosition;
                isPointerDown = true;
            }
            else if (Input.GetMouseButtonUp(0) && isPointerDown)
            {
                isPointerDown = false;
                if (Vector2.Distance(Input.mousePosition, touchStartPos) < maxTapMovement)
                {
                    PerformWorldRaycast(Input.mousePosition);
                }
            }
        }
    }

    private void PerformWorldRaycast(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f);

        // 1. Prioridade máxima: Checa se qualquer colisor atingido pertence a um lenhador ativo
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            var lj = hit.collider.GetComponentInParent<LumberjackActive>();
            if (lj != null && !lj.IsDefeated)
            {
                lj.OnPlayerTapped();
                return;
            }
        }

        // 2. Se não clicou em lenhador, avalia o primeiro hit válido
        if (hits.Length > 0)
        {
            var firstHit = hits[0];

            // Árvore existente
            var tree = firstHit.collider.GetComponentInParent<TreeClickable>();
            if (tree != null)
            {
                tree.OnPlayerTapped();
                return;
            }

            // Terreno (Plane) para plantar
            if (TreeSpawner.Instance != null && firstHit.collider.gameObject == TreeSpawner.Instance.plane.gameObject)
            {
                TryPlantAt(firstHit.point);
            }
        }
    }

    public bool TryPlantAt(Vector3 worldPos)
    {
        if (TreeSpawner.Instance == null) return false;
        if (TreeSpawner.Instance.IsInsideAnyBlocker(worldPos))
        {
            if (LumberjackManager.Instance != null)
                LumberjackManager.Instance.ShowAlert("Solo bloqueado por construções. Escolha uma área aberta!", 2f, new Color(1f, 0.7f, 0.3f));
            return false;
        }

        if (CarbonManager1.Instance != null && CarbonManager1.Instance.CurrentCarbon >= treeCost)
        {
            CarbonManager1.Instance.SpendCarbon(treeCost);
            GameObject newTree = TreeSpawner.Instance.ForceSpawnAt(worldPos, true);

            if (newTree != null)
            {
                var im = UnityEngine.Object.FindAnyObjectByType<IndicatorManager>();
                if (im != null) im.ModifyForestHealth(0.25f);

                if (BioacousticAudioManager.Instance != null)
                {
                    BioacousticAudioManager.Instance.PlayPlantSound();
                }

                if (LumberjackManager.Instance != null)
                {
                    LumberjackManager.Instance.ShowAlert("Muda nativa plantada! A floresta se expande.", 2f, new Color(0.35f, 0.95f, 0.5f));
                }
                return true;
            }
        }
        else
        {
            if (LumberjackManager.Instance != null)
            {
                LumberjackManager.Instance.ShowAlert($"CO2 insuficiente ({treeCost} ppm necessários para plantar)", 2f, new Color(1f, 0.8f, 0.3f));
            }
        }

        return false;
    }

    public void PlantTreeRandomly()
    {
        if (Time.unscaledTime - lastPlantTime < 0.15f && lastPlantTime >= 0f) return;
        lastPlantTime = Time.unscaledTime;

        if (TreeSpawner.Instance == null) return;

        if (CarbonManager1.Instance != null && CarbonManager1.Instance.CurrentCarbon >= treeCost)
        {
            CarbonManager1.Instance.SpendCarbon(treeCost);
            GameObject newTree = TreeSpawner.Instance.ForceSpawnSingleTree(true);
            if (newTree != null)
            {
                var im = UnityEngine.Object.FindAnyObjectByType<IndicatorManager>();
                if (im != null) im.ModifyForestHealth(0.25f);

                if (BioacousticAudioManager.Instance != null)
                {
                    BioacousticAudioManager.Instance.PlayPlantSound();
                }

                if (LumberjackManager.Instance != null)
                {
                    LumberjackManager.Instance.ShowAlert("Muda nativa plantada à sua frente!", 2f, new Color(0.35f, 0.95f, 0.5f));
                }
            }
        }
        else
        {
            if (LumberjackManager.Instance != null)
            {
                LumberjackManager.Instance.ShowAlert($"CO2 insuficiente ({treeCost} ppm necessários)", 2f, new Color(1f, 0.8f, 0.3f));
            }
        }
    }
}
