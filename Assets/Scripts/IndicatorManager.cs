using System;
using UnityEngine;
using TMPro;

public class IndicatorManager : MonoBehaviour
{
    public static IndicatorManager Instance { get; private set; }

    [Header("Values (0 to 10)")]
    [Range(0, 10)] public float forestHealth = 4.0f; // Começa em 40% (patamar estável inicial)
    [Range(0, 10)] public float water = 5.0f;        // Começa em 50%
    [Range(0, 10)] public float community = 5.0f;    // Começa em 50%
    [Range(0, 10)] public float resources = 5.0f;    // Começa em 50%

    [Header("UI Text References")]
    public TextMeshProUGUI forestHealthText;
    public TextMeshProUGUI waterText;
    public TextMeshProUGUI communityText;
    public TextMeshProUGUI resourcesText;

    [Header("Conectividade Matemática (GDD 3.2 & Casual Zen)")]
    [Tooltip("Período de carência inicial (segundos) sem degradação para o jogador se ambientar")]
    [SerializeField] private float initialGracePeriod = 60f;
    [Tooltip("Taxa suave de degradação da água se a floresta for muito negligenciada")]
    [SerializeField] private float waterDecayRate = 0.012f;
    [SerializeField] private float decayMultiplier = 1.0f;
    [Tooltip("Taxa de recuperação hídrica se a mata ciliar estiver saudável")]
    [SerializeField] private float waterRegenRate = 0.04f;

    public float DecayMultiplier
    {
        get => decayMultiplier;
        set => decayMultiplier = Mathf.Max(0f, value);
    }

    public bool IsGameOver { get; private set; } = false;
    public event Action<string> OnGameOverTriggered;

    private float gameTimer = 0f;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateUI();
    }

    void Update()
    {
        if (IsGameOver) return;
        if (WinScreen.Instance != null && WinScreen.Instance.IsGameWon) return;

        gameTimer += Time.deltaTime;

        // Não aplica decaimento durante o tutorial ou período de carência inicial
        if (gameTimer > initialGracePeriod)
        {
            ApplyInterdependence();
        }

        UpdateUI();
        CheckDefeatConditions();
    }

    private void ApplyInterdependence()
    {
        float bonusRetention = 0f;
        if (BiodiversityManager.Instance != null)
        {
            bonusRetention = BiodiversityManager.Instance.GetWaterRetentionBonus();
        }

        // Negligência severa com a mata ciliar (< 2.0) reduz a água suavemente, mas com piso de segurança
        if (forestHealth < 2.0f)
        {
            float decay = (waterDecayRate * decayMultiplier * (1f - bonusRetention)) * Time.deltaTime;
            // Piso de segurança de 1.5f para que o decaimento passivo sozinho nunca mate o jogador no idle
            water = Mathf.Clamp(water - decay, 1.5f, 10f);
        }
        else if (forestHealth >= 3.0f)
        {
            // Mata ciliar saudável recupera os aquíferos e reservatórios
            float regen = (waterRegenRate * (1f + bonusRetention) * (forestHealth / 10f)) * Time.deltaTime;
            water = Mathf.Clamp(water + regen, 0f, 10f);
        }

        // Clima/Recursos se beneficiam da sinergia entre floresta e água
        if (forestHealth > 4.5f && water > 4.5f)
        {
            resources = Mathf.Clamp(resources + 0.015f * Time.deltaTime, 0f, 10f);
            community = Mathf.Clamp(community + 0.015f * Time.deltaTime, 0f, 10f);
        }
    }

    private void CheckDefeatConditions()
    {
        if (IsGameOver) return;

        // O jogo só termina se um indicador for ativamente zerado por danos/decisões
        if (forestHealth <= 0f)
        {
            TriggerDefeat("A Mata Atlântica foi completamente desmatada.");
        }
        else if (water <= 0f)
        {
            TriggerDefeat("Crise hídrica extrema: os reservatórios secaram completamente.");
        }
        else if (community <= 0f)
        {
            TriggerDefeat("A comunidade perdeu as condições de sobrevivência na região.");
        }
        else if (resources <= 0f)
        {
            TriggerDefeat("Esgotamento crítico de recursos naturais essenciais.");
        }
    }

    private void TriggerDefeat(string reason)
    {
        IsGameOver = true;
        OnGameOverTriggered?.Invoke(reason);
        Debug.LogWarning("[Colapso Ambiental] " + reason);

        var goScreen = UnityEngine.Object.FindAnyObjectByType<GameOverScreen>();
        if (goScreen != null)
        {
            goScreen.Show(reason);
        }
    }

    public void ModifyForestHealth(float amount)
    {
        forestHealth = Mathf.Clamp(forestHealth + amount, 0f, 10f);
        UpdateUI();
    }

    public void ModifyWater(float amount)
    {
        water = Mathf.Clamp(water + amount, 0f, 10f);
        UpdateUI();
    }

    public void ModifyCommunity(float amount)
    {
        community = Mathf.Clamp(community + amount, 0f, 10f);
        UpdateUI();
    }

    public void ModifyResources(float amount)
    {
        resources = Mathf.Clamp(resources + amount, 0f, 10f);
        UpdateUI();
    }

    public void RestoreAll(float value = 10f)
    {
        forestHealth = Mathf.Clamp(value, 0f, 10f);
        water = Mathf.Clamp(value, 0f, 10f);
        community = Mathf.Clamp(value, 0f, 10f);
        resources = Mathf.Clamp(value, 0f, 10f);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (forestHealthText) forestHealthText.text = $"{Mathf.RoundToInt((forestHealth / 10f) * 100f)}%";
        if (waterText) waterText.text = $"{Mathf.RoundToInt((water / 10f) * 100f)}%";
        if (communityText) communityText.text = $"{Mathf.RoundToInt((community / 10f) * 100f)}%";
        if (resourcesText) resourcesText.text = $"{Mathf.RoundToInt((resources / 10f) * 100f)}%";
    }
}
