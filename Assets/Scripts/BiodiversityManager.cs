using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpeciesData
{
    public string id;
    public string commonName;
    public string scientificName;
    [TextArea(2, 4)] public string ecologicalRole;
    [TextArea(2, 4)] public string gameplayEffect;
    public float costInPpm;
    public bool isUnlocked;
    public int level;
}

public class BiodiversityManager : MonoBehaviour
{
    public static BiodiversityManager Instance { get; private set; }

    [Header("Espécies Nativas da Mata Atlântica (GDD 3.1)")]
    public List<SpeciesData> speciesCatalog = new List<SpeciesData>();

    [Header("Auto Seed Dispersal Settings")]
    [SerializeField] private float seedDispersalInterval = 16f;
    private float seedTimer = 0f;

    public event Action<SpeciesData> OnSpeciesUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeCatalog();
    }

    private void OnValidate()
    {
        if (speciesCatalog == null || speciesCatalog.Count == 0)
        {
            InitializeCatalog();
        }
    }

    private void InitializeCatalog()
    {
        if (speciesCatalog == null || speciesCatalog.Count == 0)
        {
            speciesCatalog = new List<SpeciesData>
            {
                new SpeciesData
                {
                    id = "guapuruvu",
                    commonName = "Guapuruvu",
                    scientificName = "Schizolobium parahyba",
                    ecologicalRole = "Árvore pioneira de rápido crescimento que recupera a sombra e umidade do solo.",
                    gameplayEffect = "+50% na velocidade de crescimento e acelera a regeneração da saúde florestal.",
                    costInPpm = 25f,
                    isUnlocked = false,
                    level = 0
                },
                new SpeciesData
                {
                    id = "araucaria",
                    commonName = "Araucária",
                    scientificName = "Araucaria angustifolia",
                    ecologicalRole = "Gigante da floresta com grande capacidade de absorção e retenção hídrica.",
                    gameplayEffect = "Amplia a retenção hídrica do bioma em +40% e dobra o sequestro de carbono por árvore.",
                    costInPpm = 60f,
                    isUnlocked = false,
                    level = 0
                },
                new SpeciesData
                {
                    id = "gralha_mico",
                    commonName = "Gralha-azul & Mico-leão-dourado",
                    scientificName = "Cyanocorax caeruleus & Leontopithecus rosalia",
                    ecologicalRole = "Mestres da dispersão zoológica de sementes nativas por toda a floresta.",
                    gameplayEffect = "Dispersão automática: brota uma nova árvore nativa a cada 16 segundos sem gastar CO2.",
                    costInPpm = 100f,
                    isUnlocked = false,
                    level = 0
                },
                new SpeciesData
                {
                    id = "besouro_rola_bosta",
                    commonName = "Besouro Rola-Bosta",
                    scientificName = "Canthon sp. (Scarabaeinae)",
                    ecologicalRole = "Engenheiro ecológico do solo, recicla matéria orgânica e cria galerias de aeração.",
                    gameplayEffect = "Aumenta os índices de fertilidade e permeabilidade do solo (+35% sequestro geral).",
                    costInPpm = 75f,
                    isUnlocked = false,
                    level = 0
                },
                new SpeciesData
                {
                    id = "tamandua_mirim",
                    commonName = "Tamanduá-mirim",
                    scientificName = "Tamandua tetradactyla",
                    ecologicalRole = "Predador natural que atua no controle biológico de pragas e proteção de mudas.",
                    gameplayEffect = "Controle biológico: reduz invasões e tem 50% de chance de expulsar lenhadores automaticamente.",
                    costInPpm = 120f,
                    isUnlocked = false,
                    level = 0
                }
            };
        }
    }

    private void Update()
    {
        // Gralha-azul e Mico-leão-dourado: brotamento natural sem toque manual
        if (EcologySimulation.Instance != null ? IsUnlocked("mico") : IsUnlocked("gralha_mico"))
        {
            seedTimer += Time.deltaTime;
            float interval = EcologySimulation.Instance != null ? EcologySimulation.Instance.MicoInterval : seedDispersalInterval;
            if (seedTimer >= interval)
            {
                seedTimer = 0f;
                PerformAutoSeedDispersal();
            }
        }
    }

    public bool TryUnlock(string speciesId)
    {
        if (EcologySimulation.Instance != null && StepController.WorldInputBlocked) return false;
        SpeciesData species = speciesCatalog.Find(s => s.id == speciesId);
        if (species == null || species.isUnlocked) return false;

        if (CarbonManager1.Instance != null && CarbonManager1.Instance.CurrentCarbon >= species.costInPpm)
        {
            CarbonManager1.Instance.SpendCarbon(species.costInPpm);
            species.isUnlocked = true;
            species.level = 1;

            // Aplica bônus imediatos
            ApplyImmediateBonus(species);

            OnSpeciesUnlocked?.Invoke(species);
            Debug.Log($"[Biodiversity] Despertou: {species.commonName}!");
            return true;
        }

        return false;
    }

    private void ApplyImmediateBonus(SpeciesData species)
    {
        var im = UnityEngine.Object.FindAnyObjectByType<IndicatorManager>();
        if (im != null)
        {
            if (species.id == "guapuruvu")
            {
                im.ModifyForestHealth(1.0f);
            }
            else if (species.id == "araucaria")
            {
                im.ModifyWater(1.2f);
                im.ModifyForestHealth(0.8f);
            }
            else if (species.id == "besouro_rola_bosta")
            {
                im.ModifyResources(1.0f);
                im.ModifyWater(0.5f);
            }
            else if (species.id == "tamandua_mirim")
            {
                im.ModifyCommunity(1.0f);
                im.ModifyForestHealth(0.5f);
            }
        }
    }

    private void PerformAutoSeedDispersal()
    {
        var spawner = UnityEngine.Object.FindAnyObjectByType<TreeSpawner>();
        if (spawner != null)
        {
            if (spawner.ForceSpawnSingleTree() == null) return;
            var im = UnityEngine.Object.FindAnyObjectByType<IndicatorManager>();
            if (im != null) im.ModifyForestHealth(EcologySimulation.Instance != null ? EcologySimulation.Config.seedForestGain / 10f : 0.2f);

            Debug.Log("[Biodiversity] Gralha-azul/Mico dispersou uma nova semente nativa!");
        }
    }

    public bool IsUnlocked(string speciesId)
    {
        var s = speciesCatalog.Find(x => x.id == speciesId);
        return s != null && s.isUnlocked;
    }

    public float GetCarbonSequestrationMultiplier()
    {
        if (EcologySimulation.Instance != null) return EcologySimulation.Instance.DiversityMultiplier;
        float mult = 1.0f;
        if (IsUnlocked("araucaria")) mult += 0.5f;
        if (IsUnlocked("besouro_rola_bosta")) mult += 0.35f;
        if (IsUnlocked("guapuruvu")) mult += 0.2f;
        return mult;
    }

    public float GetWaterRetentionBonus()
    {
        if (EcologySimulation.Instance != null) return Mathf.Min(EcologySimulation.Config.passiveCap, EcologySimulation.Instance.SpeciesBonus("araucaria")) / 100f;
        float bonus = 0f;
        if (IsUnlocked("araucaria")) bonus += 0.4f;
        if (IsUnlocked("besouro_rola_bosta")) bonus += 0.2f;
        return bonus;
    }

    public bool CheckTamanduaProtection()
    {
        if (!IsUnlocked("tamandua_mirim")) return false;
        // 50% de chance de afugentar automaticamente
        float chance = EcologySimulation.Instance != null ? EcologySimulation.Instance.SpeciesBonus("tamandua_mirim") / 100f : 0.5f;
        return UnityEngine.Random.value < chance;
    }

    public void ConfigureEcologyCatalog()
    {
        var combined = speciesCatalog.Find(s => s.id == "gralha_mico");
        bool wasCombinedUnlocked = combined != null && combined.isUnlocked;
        speciesCatalog.RemoveAll(s => s.id == "gralha_mico");
        AddSpecies("mico", "Mico-leão-dourado", "Leontopithecus rosalia", "A dispersão de sementes ajuda a floresta a se renovar.");
        AddSpecies("gralha", "Gralha-azul", "Cyanocorax caeruleus", "Transporta e armazena sementes como os pinhões da araucária.");
        AddSpecies("embauba", "Embaúba", "Cecropia pachystachya", "Espécie pioneira que participa da regeneração da vegetação.");
        AddSpecies("jequitiba", "Jequitibá-rosa", "Cariniana legalis", "Árvore nativa da Mata Atlântica de grande porte.");
        if (wasCombinedUnlocked)
            foreach (var id in new[] { "mico", "gralha" })
            { var s = speciesCatalog.Find(item => item.id == id); s.isUnlocked = true; s.level = 1; }
        RefreshEcologySettings();
    }

    private void AddSpecies(string id, string commonName, string scientificName, string role)
    {
        if (speciesCatalog.Exists(s => s.id == id)) return;
        speciesCatalog.Add(new SpeciesData { id = id, commonName = commonName, scientificName = scientificName, ecologicalRole = role });
    }

    public float UpgradeCost(SpeciesData species) => species.level >= 3 ? 0 :
        species.level == 1 ? EcologySimulation.Config.Species(species.id).upgradeII : EcologySimulation.Config.Species(species.id).upgradeIII;

    public bool TryUpgrade(string id)
    {
        if (EcologySimulation.Instance == null || StepController.WorldInputBlocked) return false;
        var species = speciesCatalog.Find(s => s.id == id);
        if (species == null || !species.isUnlocked || species.level >= 3 || CarbonManager1.Instance == null) return false;
        if (!CarbonManager1.Instance.SpendCarbon(UpgradeCost(species))) return false;
        species.level++;
        RefreshEcologySettings();
        return true;
    }

    public void RefreshEcologySettings()
    {
        if (EcologySimulation.Instance == null) return;
        var config = EcologySimulation.Config;
        foreach (var s in speciesCatalog)
        {
            var setting = config.Species(s.id);
            if (setting == null) continue;
            s.costInPpm = setting.unlockCost;
            s.level = s.isUnlocked ? Mathf.Clamp(s.level, 1, 3) : 0;
            float bonus = setting.Bonus(Mathf.Max(1, s.level));
            switch (s.id)
            {
                case "mico": s.gameplayEffect = $"Brotamento gratuito a cada {config.micoInterval * (1 - bonus / 100f):0.#} s. Redução do intervalo: {bonus:0.#}%. Novas árvores entram na manutenção."; break;
                case "gralha": s.gameplayEffect = $"+{bonus:0.#}% de captura com diversidade de {config.diversitySpecies:0} espécies adultas."; break;
                case "besouro_rola_bosta": s.gameplayEffect = $"Reduz a manutenção de Recursos em {bonus:0.#}%."; break;
                case "tamandua_mirim": s.gameplayEffect = $"{bonus:0.#}% de chance de afastar uma invasão automaticamente (bônus do jogo)."; break;
                default:
                    var tree = config.Tree(s.id);
                    string ability = s.id == "araucaria" ? $"-{bonus:0.#}% de consumo de água" : $"+{bonus:0.#}% de captura desta espécie";
                    s.gameplayEffect = $"Muda: {tree.production:0.#}% de captura base; cresce em {tree.growth:0.#} s. Habilidade: {ability}.";
                    break;
            }
        }
        BiodiversityUI.Instance?.UpdateExistingCardStates();
    }
}
