using UnityEngine;
using TMPro; 

public class CarbonManager1 : MonoBehaviour
{
    public static CarbonManager1 Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TreeSpawner treeSpawner;
    [SerializeField] private TextMeshProUGUI carbonText;
    [SerializeField] private TextMeshProUGUI carbonRateText;
    [SerializeField] private TextMeshProUGUI treeCountText;
    [SerializeField] private WinScreen winScreen; 

    [Header("Carbon Settings")]
    [SerializeField] private float maxCarbon = 1000f;
    [SerializeField] private float baseGenerationPower = 0.5f;
    [SerializeField] private float generationPower = 0.5f; 

    [Header("Current Status")]
    [SerializeField] private float currentCarbon = 60f; // Começa com 60 ppm para permitir plantio e experimentação imediata
    private float currentGenerationRate = 0f;

    public float CurrentCarbon => currentCarbon;
    public float MaxCarbon => maxCarbon;
    public float GenerationPower
    {
        get => generationPower;
        set => generationPower = Mathf.Max(0f, value);
    }
    public void SetGenerationMultiplier(float mult)
    {
        generationPower = baseGenerationPower * Mathf.Max(0.01f, mult);
    }

    private void Awake()
    {
        Instance = this;
        Co2.co2Amount = currentCarbon;
    }

    private void Start()
    {
        SetupUIElementsIfNeeded();
    }

    private void SetupUIElementsIfNeeded()
    {
        if (carbonText != null)
        {
            // 1. Configura ou cria texto de taxa de geração logo abaixo do número de carbono
            if (carbonRateText == null)
            {
                Transform existingRate = carbonText.transform.parent.Find("RateText");
                if (existingRate != null)
                {
                    carbonRateText = existingRate.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    var rateGo = new GameObject("RateText");
                    rateGo.transform.SetParent(carbonText.transform.parent, false);
                    var rateRt = rateGo.AddComponent<RectTransform>();
                    rateRt.anchorMin = new Vector2(0.5f, 0.5f);
                    rateRt.anchorMax = new Vector2(0.5f, 0.5f);
                    rateRt.pivot = new Vector2(0.5f, 0.5f);
                    rateRt.anchoredPosition = new Vector2(0f, -34f);
                    rateRt.sizeDelta = new Vector2(290f, 24f);

                    carbonRateText = rateGo.AddComponent<TextMeshProUGUI>();
                    if (carbonText.font != null) carbonRateText.font = carbonText.font;
                    carbonRateText.fontSize = 15f;
                    carbonRateText.color = new Color(0.35f, 0.90f, 0.55f, 1f);
                    carbonRateText.alignment = TextAlignmentOptions.Center;
                    carbonRateText.text = "+0.0 ppm/s";

                    // Ajusta o texto principal de carbono ligeiramente para cima para harmonia perfeita
                    var mainRt = carbonText.GetComponent<RectTransform>();
                    if (mainRt != null)
                    {
                        mainRt.anchoredPosition = new Vector2(0f, -2f);
                        mainRt.sizeDelta = new Vector2(290f, 42f);
                    }
                }
            }
        }

        // 2. Configura ou cria indicador de quantidade de árvores na UI
        if (treeCountText == null)
        {
            var existingTreesWidget = GameObject.Find("UI_Canvas/Game/TreesWidget");
            if (existingTreesWidget != null)
            {
                treeCountText = existingTreesWidget.transform.Find("CountText")?.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                var gameCanvas = GameObject.Find("UI_Canvas/Game");
                if (gameCanvas != null)
                {
                    var widgetGo = new GameObject("TreesWidget");
                    widgetGo.transform.SetParent(gameCanvas.transform, false);

                    var wRt = widgetGo.AddComponent<RectTransform>();
                    wRt.anchorMin = new Vector2(1f, 1f);
                    wRt.anchorMax = new Vector2(1f, 1f);
                    wRt.pivot = new Vector2(0.5f, 0.5f);
                    wRt.anchoredPosition = new Vector2(-315f, -83f);
                    wRt.sizeDelta = new Vector2(230f, 106f);

                    var wImg = widgetGo.AddComponent<UnityEngine.UI.Image>();
                    wImg.color = new Color(0.094f, 0.243f, 0.196f, 0.96f);

                    var outline = widgetGo.AddComponent<UnityEngine.UI.Outline>();
                    outline.effectColor = new Color(0.24f, 0.45f, 0.35f, 0.85f);
                    outline.effectDistance = new Vector2(2f, -2f);

                    // Caption
                    var capGo = new GameObject("Caption");
                    capGo.transform.SetParent(widgetGo.transform, false);
                    var capRt = capGo.AddComponent<RectTransform>();
                    capRt.anchorMin = new Vector2(0.5f, 0.5f);
                    capRt.anchorMax = new Vector2(0.5f, 0.5f);
                    capRt.anchoredPosition = new Vector2(0f, 31f);
                    capRt.sizeDelta = new Vector2(210f, 24f);

                    var capTmp = capGo.AddComponent<TextMeshProUGUI>();
                    if (carbonText != null && carbonText.font != null) capTmp.font = carbonText.font;
                    capTmp.text = "ÁRVORES PLANTADAS";
                    capTmp.fontSize = 13f;
                    capTmp.color = new Color(0.863f, 0.788f, 0.545f, 1f);
                    capTmp.alignment = TextAlignmentOptions.Center;

                    // Count Text
                    var cntGo = new GameObject("CountText");
                    cntGo.transform.SetParent(widgetGo.transform, false);
                    var cntRt = cntGo.AddComponent<RectTransform>();
                    cntRt.anchorMin = new Vector2(0.5f, 0.5f);
                    cntRt.anchorMax = new Vector2(0.5f, 0.5f);
                    cntRt.anchoredPosition = new Vector2(0f, -4f);
                    cntRt.sizeDelta = new Vector2(210f, 42f);

                    treeCountText = cntGo.AddComponent<TextMeshProUGUI>();
                    if (carbonText != null && carbonText.font != null) treeCountText.font = carbonText.font;
                    treeCountText.text = "0";
                    treeCountText.fontSize = 32f;
                    treeCountText.fontStyle = FontStyles.Bold;
                    treeCountText.color = new Color(0.961f, 0.945f, 0.902f, 1f);
                    treeCountText.alignment = TextAlignmentOptions.Center;

                    // SubText
                    var subGo = new GameObject("SubText");
                    subGo.transform.SetParent(widgetGo.transform, false);
                    var subRt = subGo.AddComponent<RectTransform>();
                    subRt.anchorMin = new Vector2(0.5f, 0.5f);
                    subRt.anchorMax = new Vector2(0.5f, 0.5f);
                    subRt.anchoredPosition = new Vector2(0f, -34f);
                    subRt.sizeDelta = new Vector2(210f, 20f);

                    var subTmp = subGo.AddComponent<TextMeshProUGUI>();
                    if (carbonText != null && carbonText.font != null) subTmp.font = carbonText.font;
                    subTmp.text = "Fotossíntese Ativa";
                    subTmp.fontSize = 13f;
                    subTmp.color = new Color(0.45f, 0.85f, 0.65f, 1f);
                    subTmp.alignment = TextAlignmentOptions.Center;
                }
            }
        }
    }

    void Update()
    {
        if (treeSpawner == null || (winScreen != null && winScreen.IsGameWon)) return;
        if (IndicatorManager.Instance != null && IndicatorManager.Instance.IsGameOver) return;

        int treeCount = treeSpawner.GetTreeCount();

        // Multiplicador de biodiversidade (Araucária, Besouro Rola-Bosta, Guapuruvu)
        float bioMultiplier = 1.0f;
        if (BiodiversityManager.Instance != null)
        {
            bioMultiplier = BiodiversityManager.Instance.GetCarbonSequestrationMultiplier();
        }

        // Fator ecológico de saúde hídrica (a água da região acelera a capacidade fotossintética)
        float waterFactor = 1.0f;
        if (IndicatorManager.Instance != null)
        {
            waterFactor = 0.5f + 0.5f * (IndicatorManager.Instance.water / 10f);
        }

        // Bônus climático: chuva tropical acelera fotossíntese e sequestro de carbono em +35%
        float weatherFactor = (WeatherManager.Instance != null && WeatherManager.Instance.IsRaining) ? 1.35f : 1.0f;

        currentGenerationRate = treeCount * generationPower * bioMultiplier * waterFactor * weatherFactor;
        float delta = currentGenerationRate * Time.deltaTime;
        AddCarbon(delta);

        UpdateCarbonDisplay(treeCount);

        // Checa condição de vitória
        if (currentCarbon >= maxCarbon && winScreen != null && !winScreen.IsGameWon)
        {
            winScreen.Show();
        }
    }

    public void AddCarbon(float amount)
    {
        currentCarbon += amount;
        currentCarbon = Mathf.Clamp(currentCarbon, 0f, maxCarbon);
        Co2.co2Amount = currentCarbon;
        UpdateCarbonDisplay(treeSpawner != null ? treeSpawner.GetTreeCount() : 0);
    }

    public bool SpendCarbon(float amount)
    {
        if (currentCarbon >= amount)
        {
            currentCarbon -= amount;
            Co2.co2Amount = currentCarbon;
            UpdateCarbonDisplay(treeSpawner != null ? treeSpawner.GetTreeCount() : 0);
            return true;
        }
        return false;
    }

    private void UpdateCarbonDisplay(int treeCount = -1)
    {
        if (carbonText != null)
        {
            // Formato digital conforme GDD: "60 ppm"
            carbonText.text = $"{Mathf.RoundToInt(currentCarbon):N0} ppm";
        }

        if (carbonRateText != null)
        {
            // Formato menor abaixo do número: "+X.X ppm/s"
            carbonRateText.text = $"+{currentGenerationRate:F1} ppm/s";
        }

        if (treeCountText != null)
        {
            if (treeCount < 0 && treeSpawner != null) treeCount = treeSpawner.GetTreeCount();
            treeCountText.text = $"{Mathf.Max(0, treeCount):N0}";
        }
    }
}
