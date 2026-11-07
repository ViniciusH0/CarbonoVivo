using UnityEngine;
using TMPro; 

public class CarbonManager1 : MonoBehaviour
{
    public static CarbonManager1 Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TreeSpawner treeSpawner;
    [SerializeField] private TextMeshProUGUI carbonText;
    [SerializeField] private WinScreen winScreen; 

    [Header("Carbon Settings")]
    [SerializeField] private float maxCarbon = 1000f;
    [SerializeField] private float baseGenerationPower = 0.5f;
    [SerializeField] private float generationPower = 0.5f; 

    [Header("Current Status")]
    [SerializeField] private float currentCarbon = 60f; // Começa com 60 ppm para permitir plantio e experimentação imediata

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

        float delta = treeCount * generationPower * bioMultiplier * waterFactor * weatherFactor * Time.deltaTime;
        AddCarbon(delta);

        UpdateCarbonDisplay();

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
        UpdateCarbonDisplay();
    }

    public bool SpendCarbon(float amount)
    {
        if (currentCarbon >= amount)
        {
            currentCarbon -= amount;
            Co2.co2Amount = currentCarbon;
            UpdateCarbonDisplay();
            return true;
        }
        return false;
    }

    private void UpdateCarbonDisplay()
    {
        if (carbonText != null)
        {
            // Formato digital minimalista conforme GDD: "15.420 ppm"
            carbonText.text = $"{Mathf.RoundToInt(currentCarbon):N0} ppm";
        }
    }
}
