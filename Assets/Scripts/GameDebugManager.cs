using System;
using UnityEngine;

public class GameDebugManager : MonoBehaviour
{
    public static GameDebugManager Instance { get; private set; }

    public GameDebugConfigData Config { get; private set; }

    [Header("UI Reference")]
    [SerializeField] private DebugPanelUI debugPanelUI;

    public event Action OnConfigChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Carrega configurações persistidas de disco/PlayerPrefs
        Config = GameDebugConfigData.Load();
    }

    private void Start()
    {
        ApplyAll();

        if (debugPanelUI == null)
        {
            debugPanelUI = FindAnyObjectByType<DebugPanelUI>();
        }
    }

    private void Update()
    {
        // Teclas de atalho para abrir/fechar o painel de debug: F1 ou Til/Crase ou F12
        if (Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.F12))
        {
            ToggleDebugPanel();
        }
    }

    public void ToggleDebugPanel()
    {
        if (debugPanelUI != null)
        {
            debugPanelUI.TogglePanel();
        }
    }

    public void ApplyAll()
    {
        if (Config == null) return;

        // 1. Carbono
        if (CarbonManager1.Instance != null)
        {
            CarbonManager1.Instance.SetGenerationMultiplier(Config.carbonGenerationMultiplier);
        }

        // 2. Plantio de Árvores
        if (InteractivePlanting.Instance != null)
        {
            InteractivePlanting.Instance.TreeCost = Config.treePlantCost;
        }

        // 3. Dilemas / Perguntas
        if (QuestionManager.Instance != null)
        {
            QuestionManager.Instance.QuestionInterval = Config.decisionInterval;
        }

        // 4. Decaimento dos Indicadores
        if (IndicatorManager.Instance != null)
        {
            IndicatorManager.Instance.DecayMultiplier = Config.indicatorDecayMultiplier;
        }

        // 5. Lenhadores
        if (LumberjackManager.Instance != null)
        {
            LumberjackManager.Instance.SetSpawnInterval(Config.lumberjackSpawnInterval);
            LumberjackManager.Instance.HitsToDefeat = Config.lumberjackHitsToDefeat;
            LumberjackManager.Instance.CutTimeLimit = Config.lumberjackCutTimeLimit;
        }

        // 6. Ciclo Dia/Noite e Iluminação
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.DayDurationSeconds = Config.dayCycleDuration;
            DayNightCycle.Instance.PauseCycle = Config.pauseDayCycle;
            DayNightCycle.Instance.SunIntensityMultiplier = Config.sunIntensityMultiplier;
            DayNightCycle.Instance.FogDensityMultiplier = Config.fogDensityMultiplier;
            if (Config.pauseDayCycle)
            {
                DayNightCycle.Instance.SetTime(Config.timeOfDay);
            }
        }

        // 7. Clima e Chuva
        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.SetRainInterval(Config.rainInterval);
            WeatherManager.Instance.DefaultRainDuration = Config.rainDuration;
        }

        OnConfigChanged?.Invoke();
    }

    // --- Setters de Game Design ---

    public void SetDecisionInterval(float seconds)
    {
        Config.decisionInterval = Mathf.Clamp(seconds, 3f, 120f);
        if (QuestionManager.Instance != null)
        {
            QuestionManager.Instance.QuestionInterval = Config.decisionInterval;
        }
        Config.Save();
        OnConfigChanged?.Invoke();
    }

    public void SetCarbonMultiplier(float mult)
    {
        Config.carbonGenerationMultiplier = Mathf.Clamp(mult, 0.1f, 10f);
        if (CarbonManager1.Instance != null)
        {
            CarbonManager1.Instance.SetGenerationMultiplier(Config.carbonGenerationMultiplier);
        }
        Config.Save();
        OnConfigChanged?.Invoke();
    }

    public void SetTreePlantCost(float cost)
    {
        Config.treePlantCost = Mathf.Clamp(cost, 0f, 100f);
        if (InteractivePlanting.Instance != null)
        {
            InteractivePlanting.Instance.TreeCost = Config.treePlantCost;
        }
        Config.Save();
        OnConfigChanged?.Invoke();
    }

    public void SetIndicatorDecayMultiplier(float mult)
    {
        Config.indicatorDecayMultiplier = Mathf.Clamp(mult, 0f, 5f);
        if (IndicatorManager.Instance != null)
        {
            IndicatorManager.Instance.DecayMultiplier = Config.indicatorDecayMultiplier;
        }
        Config.Save();
        OnConfigChanged?.Invoke();
    }

    public void SetLumberjackSpawnInterval(float interval)
    {
        Config.lumberjackSpawnInterval = Mathf.Clamp(interval, 5f, 120f);
        if (LumberjackManager.Instance != null)
        {
            LumberjackManager.Instance.SetSpawnInterval(Config.lumberjackSpawnInterval);
        }
        Config.Save();
        OnConfigChanged?.Invoke();
    }

    public void SetLumberjackHitsToDefeat(int hits)
    {
        Config.lumberjackHitsToDefeat = Mathf.Clamp(hits, 1, 10);
        if (LumberjackManager.Instance != null)
        {
            LumberjackManager.Instance.HitsToDefeat = Config.lumberjackHitsToDefeat;
        }
        Config.Save();
        OnConfigChanged?.Invoke();
    }

    public void SetLumberjackCutTimeLimit(float limit)
    {
        Config.lumberjackCutTimeLimit = Mathf.Clamp(limit, 4f, 60f);
        if (LumberjackManager.Instance != null)
        {
            LumberjackManager.Instance.CutTimeLimit = Config.lumberjackCutTimeLimit;
        }
        Config.Save();
        OnConfigChanged?.Invoke();
    }

    // --- Setters de Visual e Clima ---

    public void SetTimeOfDay(float hour)
    {
        Config.timeOfDay = Mathf.Repeat(hour, 24f);
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.SetTime(Config.timeOfDay, true);
        }
        Config.Save();
        OnConfigChanged?.Invoke();
    }

    public void SetDayCycleDuration(float duration)
    {
        Config.dayCycleDuration = Mathf.Clamp(duration, 10f, 600f);
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.DayDurationSeconds = Config.dayCycleDuration;
        }
        Config.Save();
        OnConfigChanged?.Invoke();
    }

    public void SetPauseDayCycle(bool pause)
    {
        Config.pauseDayCycle = pause;
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.PauseCycle = pause;
            if (pause)
            {
                DayNightCycle.Instance.SetTime(Config.timeOfDay, true);
            }
        }
        Config.Save();
        OnConfigChanged?.Invoke();
    }

    public void SetSunIntensityMultiplier(float mult)
    {
        Config.sunIntensityMultiplier = Mathf.Clamp(mult, 0.1f, 4f);
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.SunIntensityMultiplier = Config.sunIntensityMultiplier;
        }
        Config.Save();
        OnConfigChanged?.Invoke();
    }

    public void SetFogDensityMultiplier(float mult)
    {
        Config.fogDensityMultiplier = Mathf.Clamp(mult, 0f, 5f);
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.FogDensityMultiplier = Config.fogDensityMultiplier;
        }
        Config.Save();
        OnConfigChanged?.Invoke();
    }

    public void SetRainInterval(float interval)
    {
        Config.rainInterval = Mathf.Clamp(interval, 10f, 300f);
        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.SetRainInterval(Config.rainInterval);
        }
        Config.Save();
        OnConfigChanged?.Invoke();
    }

    public void SetRainDuration(float duration)
    {
        Config.rainDuration = Mathf.Clamp(duration, 5f, 120f);
        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.DefaultRainDuration = Config.rainDuration;
        }
        Config.Save();
        OnConfigChanged?.Invoke();
    }

    // --- Ações Imediatas / Cheats ---

    public void TriggerRainNow()
    {
        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.TriggerRain(Config.rainDuration);
        }
    }

    public void StopRainNow()
    {
        if (WeatherManager.Instance != null)
        {
            WeatherManager.Instance.StopRain();
        }
    }

    public void AddQuickCarbon(float amount = 500f)
    {
        if (CarbonManager1.Instance != null)
        {
            CarbonManager1.Instance.AddCarbon(amount);
        }
    }

    public void RestoreIndicatorsNow()
    {
        if (IndicatorManager.Instance != null)
        {
            IndicatorManager.Instance.RestoreAll(10f);
        }
    }

    public void SpawnLumberjackNow()
    {
        if (LumberjackManager.Instance != null)
        {
            LumberjackManager.Instance.ForceSpawnLumberjack();
        }
    }

    public void TriggerQuestionNow()
    {
        if (QuestionManager.Instance != null)
        {
            QuestionManager.Instance.ShowRandomQuestion();
        }
    }

    public void ResetToDefaults()
    {
        Config.ResetDefaults();
        ApplyAll();
    }

    public void ForceSave()
    {
        Config.Save();
    }
}

