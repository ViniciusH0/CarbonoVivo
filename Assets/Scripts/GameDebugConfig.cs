using System;
using System.IO;
using UnityEngine;

[System.Serializable]
public class GameDebugConfigData
{
    [Header("Game Design Settings")]
    public float decisionInterval = 22f;          // Intervalo entre dilemas/perguntas (s)
    public float carbonGenerationMultiplier = 1.0f; // Multiplicador de geração/sequestro de carbono
    public float treePlantCost = 10f;             // Custo em ppm para plantar muda
    public float indicatorDecayMultiplier = 1.0f; // Multiplicador de decaimento dos indicadores
    public float lumberjackSpawnInterval = 25f;   // Intervalo médio entre invasões de lenhadores (s)
    public int lumberjackHitsToDefeat = 3;         // Golpes necessários para derrotar lenhador
    public float lumberjackCutTimeLimit = 15f;    // Tempo limite de corte antes da árvore cair (s)

    [Header("Visual & Atmosphere Settings")]
    public float timeOfDay = 8.5f;                // Horário do dia (0h a 24h)
    public float dayCycleDuration = 120f;         // Duração do ciclo completo de 24h em segundos reais
    public bool pauseDayCycle = false;            // Pausar ciclo no horário selecionado
    public float sunIntensityMultiplier = 1.0f;   // Multiplicador de intensidade de luz solar/lunar
    public float fogDensityMultiplier = 1.0f;     // Opacidade/densidade da névoa da floresta
    public float rainInterval = 80f;              // Intervalo entre tempestades de chuva (s)
    public float rainDuration = 28f;              // Duração da chuva (s)

    private const string PREFS_KEY = "GameDebugSettings_V1";
    private const string FILE_NAME = "GameDebugSettings.json";

    public static string GetPersistentPath()
    {
        return Path.Combine(Application.persistentDataPath, FILE_NAME);
    }

    public static string GetProjectPath()
    {
        return Path.Combine(Application.dataPath, "Settings", FILE_NAME);
    }

    public static GameDebugConfigData Load()
    {
        GameDebugConfigData data = null;

        // 1. Tenta carregar do persistentDataPath
        try
        {
            string pPath = GetPersistentPath();
            if (File.Exists(pPath))
            {
                string json = File.ReadAllText(pPath);
                if (!string.IsNullOrEmpty(json))
                {
                    data = JsonUtility.FromJson<GameDebugConfigData>(json);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GameDebugConfig] Falha ao ler de persistentDataPath: " + ex.Message);
        }

        // 2. Tenta carregar do pasta de Settings do projeto (Editor)
        if (data == null)
        {
            try
            {
                string prjPath = GetProjectPath();
                if (File.Exists(prjPath))
                {
                    string json = File.ReadAllText(prjPath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        data = JsonUtility.FromJson<GameDebugConfigData>(json);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameDebugConfig] Falha ao ler de projectPath: " + ex.Message);
            }
        }

        // 3. Tenta carregar de PlayerPrefs
        if (data == null && PlayerPrefs.HasKey(PREFS_KEY))
        {
            try
            {
                string json = PlayerPrefs.GetString(PREFS_KEY, "");
                if (!string.IsNullOrEmpty(json))
                {
                    data = JsonUtility.FromJson<GameDebugConfigData>(json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameDebugConfig] Falha ao ler de PlayerPrefs: " + ex.Message);
            }
        }

        if (data == null)
        {
            data = new GameDebugConfigData();
            data.Save();
        }

        return data;
    }

    public void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(this, true);

            // 1. Salva no persistentDataPath
            string pPath = GetPersistentPath();
            string pDir = Path.GetDirectoryName(pPath);
            if (!Directory.Exists(pDir)) Directory.CreateDirectory(pDir);
            File.WriteAllText(pPath, json);

            // 2. Salva no projectPath (Assets/Settings)
            string prjPath = GetProjectPath();
            string prjDir = Path.GetDirectoryName(prjPath);
            if (!Directory.Exists(prjDir)) Directory.CreateDirectory(prjDir);
            File.WriteAllText(prjPath, json);

            // 3. Salva em PlayerPrefs
            PlayerPrefs.SetString(PREFS_KEY, json);
            PlayerPrefs.Save();

            Debug.Log($"[GameDebugConfig] Configurações salvas com sucesso em disco e persistidas.");
        }
        catch (Exception ex)
        {
            Debug.LogError("[GameDebugConfig] Erro ao salvar configurações: " + ex.Message);
        }
    }

    public void ResetDefaults()
    {
        decisionInterval = 22f;
        carbonGenerationMultiplier = 1.0f;
        treePlantCost = 10f;
        indicatorDecayMultiplier = 1.0f;
        lumberjackSpawnInterval = 25f;
        lumberjackHitsToDefeat = 3;
        lumberjackCutTimeLimit = 15f;

        timeOfDay = 8.5f;
        dayCycleDuration = 120f;
        pauseDayCycle = false;
        sunIntensityMultiplier = 1.0f;
        fogDensityMultiplier = 1.0f;
        rainInterval = 80f;
        rainDuration = 28f;

        Save();
    }
}

