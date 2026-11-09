using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InteractivePlanting : MonoBehaviour
{
    public static InteractivePlanting Instance { get; private set; }
    public static event System.Action<GameObject> OnTreePlanted;

    [Header("Planting Cost")]
    [SerializeField] private float treeCost = 10f;
    public float TreeCost
    {
        get => GetCurrentCost();
        set => treeCost = Mathf.Max(0f, value);
    }

    public float GetCurrentCost()
    {
        int count = TreeSpawner.Instance != null ? TreeSpawner.Instance.GetTreeCount() : 0;
        if (EcologySimulation.Instance != null)
        {
            var config = EcologySimulation.Config;
            return (treeCost + Mathf.Min(config.plantingCostCap, Mathf.Floor(count / 3f) * config.plantingCostStep)) *
                config.Tree(EcologySimulation.Instance.SelectedTreeId).cost;
        }
        // As primeiras 3 mudas custam 10 ppm (fácil inicialização). 
        // Depois o custo sobe suavemente +2 ppm a cada 3 árvores até o teto de 22 ppm.
        return treeCost + Mathf.Min(12f, Mathf.Floor(count / 3f) * 2f);
    }

    public void UpdateButtonUI()
    {
        if (hudPlantButton != null)
        {
            var txt = hudPlantButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = $"<b>Plantar Muda</b> ({GetCurrentCost():0} ppm)";
            }
        }
    }
    private float lastPlantTime = -1f;

    [Header("UI Reference")]
    [SerializeField] private Button hudPlantButton;

    [Header("Input Filtering")]
    [SerializeField] private float maxTapMovement = 20f;
    private Vector2 touchStartPos;
    private bool isPointerDown = false;
    private int activeFingerId = -1;
    private Component pressedThreat;
    private bool pressedOnThreat;
    private float lastCombatTapTime = float.NegativeInfinity;
    [SerializeField] private float combatClickProtection = 0.5f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (gameObject.scene.name == "GameScene_Test" && EcologySimulation.Instance == null)
            gameObject.AddComponent<EcologySimulation>();
        if (gameObject.scene.name == "GameScene_Test" && ForestStoryDirector.Instance == null)
            gameObject.AddComponent<ForestStoryDirector>();
        if (GetComponent<ForestOperationsManager>() == null && ForestOperationsManager.Instance == null)
        {
            gameObject.AddComponent<ForestOperationsManager>();
        }
        if (GetComponent<ForestFireManager>() == null && ForestFireManager.Instance == null)
        {
            gameObject.AddComponent<ForestFireManager>();
        }

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
        if (StepController.WorldInputBlocked) { isPointerDown = false; return; }
        HandleTouchInput();
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount > 1) { isPointerDown = false; return; }
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                isPointerDown = false;
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    return;

                touchStartPos = touch.position;
                isPointerDown = true;
                activeFingerId = touch.fingerId;
                CapturePressedThreat(touch.position);
            }
            else if (touch.fingerId != activeFingerId)
            {
                isPointerDown = false;
            }
            else if (touch.phase == TouchPhase.Moved && Vector2.Distance(touch.position, touchStartPos) >= maxTapMovement)
            {
                isPointerDown = false;
            }
            else if (touch.phase == TouchPhase.Ended && isPointerDown)
            {
                isPointerDown = false;
                if (Vector2.Distance(touch.position, touchStartPos) < maxTapMovement &&
                    (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(touch.fingerId)))
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
                isPointerDown = false;
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                touchStartPos = Input.mousePosition;
                isPointerDown = true;
                CapturePressedThreat(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0) && Vector2.Distance(Input.mousePosition, touchStartPos) >= maxTapMovement)
            {
                isPointerDown = false;
            }
            else if (Input.GetMouseButtonUp(0) && isPointerDown)
            {
                isPointerDown = false;
                if (Vector2.Distance(Input.mousePosition, touchStartPos) < maxTapMovement &&
                    (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                {
                    PerformWorldRaycast(Input.mousePosition);
                }
            }
        }
    }

    private void CapturePressedThreat(Vector2 screenPos)
    {
        pressedThreat = null;
        pressedOnThreat = false;
        if (Camera.main == null) return;
        var hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(screenPos), 500f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            var lumberjack = hit.collider.GetComponentInParent<LumberjackActive>();
            if (lumberjack != null && !lumberjack.IsDefeated) { pressedThreat = lumberjack; break; }
        }
        if (pressedThreat == null)
        foreach (var hit in hits)
        {
            var fire = hit.collider.GetComponentInParent<BurningTreeEffect>();
            if (fire != null && fire.IsBurning) { pressedThreat = fire; break; }
        }
        pressedOnThreat = pressedThreat != null;
    }

    private void PerformWorldRaycast(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f);

        // 1. Prioridade máxima: Checa se qualquer colisor atingido pertence a um lenhador ativo
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        // A combat gesture stays consumed even when its target disappears before release.
        if (pressedOnThreat)
        {
            lastCombatTapTime = Time.unscaledTime;
            if (pressedThreat is LumberjackActive lumberjack && lumberjack != null && !lumberjack.IsDefeated &&
                System.Array.Exists(hits, h => h.collider.GetComponentInParent<LumberjackActive>() == lumberjack))
                lumberjack.OnPlayerTapped();
            else if (pressedThreat is BurningTreeEffect fire && fire != null && fire.IsBurning &&
                System.Array.Exists(hits, h => h.collider.GetComponentInParent<BurningTreeEffect>() == fire))
                fire.OnPlayerExtinguishClick();
            return;
        }
        foreach (var hit in hits)
        {
            var lj = hit.collider.GetComponentInParent<LumberjackActive>();
            if (lj != null && !lj.IsDefeated)
            {
                lastCombatTapTime = Time.unscaledTime;
                lj.OnPlayerTapped();
                return;
            }
        }

        // 2. Segunda prioridade: Checa se clicou em uma árvore pegando fogo (Incêndio Florestal)
        foreach (var hit in hits)
        {
            var burning = hit.collider.GetComponentInParent<BurningTreeEffect>();
            if (burning != null && burning.IsBurning)
            {
                lastCombatTapTime = Time.unscaledTime;
                burning.OnPlayerExtinguishClick();
                return;
            }
        }

        // 3. Se não clicou em lenhador nem fogo, avalia o primeiro hit válido
        if (Time.unscaledTime - lastCombatTapTime < combatClickProtection) return;
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
        if (StepController.WorldInputBlocked) return false;
        if (TreeSpawner.Instance == null) return false;
        if (TreeSpawner.Instance.IsInsideAnyBlocker(worldPos))
        {
            if (LumberjackManager.Instance != null)
                LumberjackManager.Instance.ShowAlert("Solo bloqueado por construções. Escolha uma área aberta!", 2f, new Color(1f, 0.7f, 0.3f));
            return false;
        }

        string speciesId = EcologySimulation.Instance != null ? EcologySimulation.Instance.SelectedTreeId : "guapuruvu";
        if (!TreeSpawner.Instance.IsPlantingPositionAvailable(worldPos))
        {
            ShowNoPlantingSpace();
            return false;
        }
        float cost = GetCurrentCost();
        var paidCarbon = CarbonManager1.Instance;
        if (paidCarbon != null && paidCarbon.CurrentCarbon >= cost)
        {
            CarbonManager1.Instance.SpendCarbon(cost);
            UpdateButtonUI();

            var im = UnityEngine.Object.FindAnyObjectByType<IndicatorManager>();

            if (ForestOperationsManager.Instance != null)
            {
                if (LumberjackManager.Instance != null)
                {
                    LumberjackManager.Instance.ShowAlert(PlantingMessage(speciesId), 2.4f, new Color(0.35f, 0.95f, 0.5f));
                }

                bool dispatched = ForestOperationsManager.Instance.DispatchPlantingDrone(worldPos, (plantedTree) =>
                {
                    if (plantedTree != null)
                    {
                        if (im != null) im.ModifyForestHealth(0.25f);
                        OnTreePlanted?.Invoke(plantedTree);
                        UpdateButtonUI();
                    }
                    else if (paidCarbon != null)
                    {
                        paidCarbon.AddCarbon(cost);
                        if (this != null) UpdateButtonUI();
                    }
                }, speciesId);
                if (!dispatched)
                {
                    paidCarbon.AddCarbon(cost);
                    UpdateButtonUI();
                    ShowNoPlantingSpace();
                }
                return dispatched;
            }
            else
            {
                GameObject newTree = TreeSpawner.Instance.ForceSpawnAt(worldPos, true, speciesId);
                if (newTree != null)
                {
                    if (im != null) im.ModifyForestHealth(0.25f);
                    if (BioacousticAudioManager.Instance != null)
                    {
                        BioacousticAudioManager.Instance.PlayPlantSound();
                    }

                    if (LumberjackManager.Instance != null)
                    {
                        LumberjackManager.Instance.ShowAlert("Muda nativa plantada! A floresta se expande.", 2f, new Color(0.35f, 0.95f, 0.5f));
                    }

                    OnTreePlanted?.Invoke(newTree);
                    UpdateButtonUI();
                    return true;
                }
                paidCarbon.AddCarbon(cost);
                UpdateButtonUI();
            }
        }
        else
        {
            if (LumberjackManager.Instance != null)
            {
                LumberjackManager.Instance.ShowAlert($"CO2 insuficiente ({cost:0} ppm necessários para plantar)", 2f, new Color(1f, 0.8f, 0.3f));
            }
        }

        return false;
    }

    public void PlantTreeRandomly()
    {
        if (StepController.WorldInputBlocked) return;
        if (Time.unscaledTime - lastPlantTime < 0.15f && lastPlantTime >= 0f) return;
        lastPlantTime = Time.unscaledTime;

        if (TreeSpawner.Instance == null) return;

        string speciesId = EcologySimulation.Instance != null ? EcologySimulation.Instance.SelectedTreeId : "guapuruvu";
        float cost = GetCurrentCost();
        var paidCarbon = CarbonManager1.Instance;
        if (paidCarbon != null && paidCarbon.CurrentCarbon >= cost)
        {
            Vector3 targetPos;
            if (!TreeSpawner.Instance.TryFindValidSpawnPosition(true, out targetPos))
            {
                ShowNoPlantingSpace();
                return;
            }

            CarbonManager1.Instance.SpendCarbon(cost);
            UpdateButtonUI();

            var im = UnityEngine.Object.FindAnyObjectByType<IndicatorManager>();

            if (ForestOperationsManager.Instance != null)
            {
                if (LumberjackManager.Instance != null)
                {
                    LumberjackManager.Instance.ShowAlert(PlantingMessage(speciesId), 2.4f, new Color(0.35f, 0.95f, 0.5f));
                }

                bool dispatched = ForestOperationsManager.Instance.DispatchPlantingDrone(targetPos, (plantedTree) =>
                {
                    if (plantedTree != null)
                    {
                        if (im != null) im.ModifyForestHealth(0.25f);
                        OnTreePlanted?.Invoke(plantedTree);
                        UpdateButtonUI();
                    }
                    else if (paidCarbon != null)
                    {
                        paidCarbon.AddCarbon(cost);
                        if (this != null) UpdateButtonUI();
                    }
                }, speciesId);
                if (!dispatched)
                {
                    paidCarbon.AddCarbon(cost);
                    UpdateButtonUI();
                    ShowNoPlantingSpace();
                }
            }
            else
            {
                GameObject newTree = TreeSpawner.Instance.ForceSpawnAt(targetPos, true, speciesId);
                if (newTree != null)
                {
                    if (im != null) im.ModifyForestHealth(0.25f);
                    if (BioacousticAudioManager.Instance != null)
                    {
                        BioacousticAudioManager.Instance.PlayPlantSound();
                    }

                    if (LumberjackManager.Instance != null)
                    {
                        LumberjackManager.Instance.ShowAlert("Muda nativa plantada à sua frente!", 2f, new Color(0.35f, 0.95f, 0.5f));
                    }

                    OnTreePlanted?.Invoke(newTree);
                    UpdateButtonUI();
                }
                else
                {
                    paidCarbon.AddCarbon(cost);
                    UpdateButtonUI();
                }
            }
        }
        else
        {
            if (LumberjackManager.Instance != null)
            {
                LumberjackManager.Instance.ShowAlert($"CO2 insuficiente ({cost:0} ppm necessários)", 2f, new Color(1f, 0.8f, 0.3f));
            }
        }
    }

    private string PlantingMessage(string speciesId)
    {
        var tree = EcologySimulation.Config.Tree(speciesId);
        return EcologySimulation.Instance != null
            ? $"Drone enviado! {tree.name}: crescimento em {tree.growth:0.#} s após o plantio."
            : "Drone de reflorestamento enviado da base! A árvore crescerá em 10s.";
    }

    private void ShowNoPlantingSpace()
    {
        LumberjackManager.Instance?.ShowAlert("Sem espaço livre para esta muda. Escolha um ponto mais afastado das árvores e dos plantios em andamento.", 2.5f, new Color(1f, .8f, .3f));
    }
}
