using System.Collections.Generic;
using UnityEngine;

public class EcologySimulation : MonoBehaviour
{
    public static EcologySimulation Instance { get; private set; }
    private static readonly EcologyBalanceConfig defaults = new EcologyBalanceConfig();
    public static EcologyBalanceConfig Config => GameDebugManager.Instance != null && GameDebugManager.Instance.Config != null
        ? GameDebugManager.Instance.Config.ecology : defaults;
    public string SelectedTreeId { get; private set; } = "guapuruvu";
    private readonly Dictionary<string, float> readyAt = new Dictionary<string, float>();
    private readonly Dictionary<string, float> endsAt = new Dictionary<string, float>();
    private float elapsed;
    public float ResourceRate { get; private set; }
    public float WaterRate { get; private set; }
    public float CommunityRate { get; private set; }
    public bool GraceActive => elapsed < Config.graceSeconds;

    private void Awake() { Instance = this; }
    private void Start()
    {
        BiodiversityManager.Instance?.ConfigureEcologyCatalog();
        gameObject.AddComponent<EcologyHUD>();
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    private void Update()
    {
        if (StepController.WorldInputBlocked || (WinScreen.Instance != null && WinScreen.Instance.IsGameWon) ||
            (IndicatorManager.Instance != null && IndicatorManager.Instance.IsGameOver)) return;
        elapsed += Time.deltaTime;
    }

    public bool SelectTree(string id)
    {
        if (id != "guapuruvu" && (BiodiversityManager.Instance == null || !BiodiversityManager.Instance.IsUnlocked(id))) return false;
        SelectedTreeId = Config.Tree(id).id;
        InteractivePlanting.Instance?.UpdateButtonUI();
        return true;
    }

    public float MaintenanceWeight
    {
        get
        {
            float sum = 0;
            if (TreeSpawner.Instance != null)
            foreach (var tree in TreeSpawner.Instance.SpawnedTrees)
                if (tree != null) sum += tree.GetComponent<ForestTreeSpecies>()?.Balance.maintenance ?? 1f;
            return sum;
        }
    }

    public float ProductionWeight
    {
        get
        {
            float sum = 0;
            if (TreeSpawner.Instance != null)
            foreach (var tree in TreeSpawner.Instance.SpawnedTrees)
            {
                if (tree == null) continue;
                var fire = tree.GetComponent<BurningTreeEffect>();
                if (fire != null && fire.IsBurning) continue;
                var identity = tree.GetComponent<ForestTreeSpecies>();
                float weight = identity != null ? identity.ProductionWeight : 1;
                float bonus = identity != null ? SpeciesBonus(identity.speciesId) : 0;
                // Araucária conserves water; its production is already in TreeBalance.
                if (identity != null && identity.speciesId == "araucaria") bonus = 0;
                sum += weight * (1f + Mathf.Min(Config.passiveCap, bonus) / 100f);
            }
            return sum;
        }
    }

    public int MatureSpeciesCount
    {
        get
        {
            var ids = new HashSet<string>();
            if (TreeSpawner.Instance != null)
            foreach (var tree in TreeSpawner.Instance.SpawnedTrees)
            {
                var identity = tree != null ? tree.GetComponent<ForestTreeSpecies>() : null;
                if (identity != null && identity.IsMature) ids.Add(identity.speciesId);
            }
            return ids.Count;
        }
    }
    public float DiversityMultiplier => MatureSpeciesCount >= Mathf.RoundToInt(Config.diversitySpecies)
        ? 1f + Mathf.Min(Config.passiveCap, Config.diversityBonus + SpeciesBonus("gralha")) / 100f : 1f;
    public float SpeciesBonus(string id)
    {
        var species = BiodiversityManager.Instance?.speciesCatalog.Find(s => s.id == id);
        return species != null && species.isUnlocked ? Config.Species(id)?.Bonus(species.level) ?? 0 : 0;
    }
    public float MicoInterval => Mathf.Max(0.25f, Config.micoInterval * (1f - SpeciesBonus("mico") / 100f));
    public bool AbilityActive(string id) => endsAt.TryGetValue(id, out float end) && Time.time < end;
    public float RemainingCooldown(string id) => readyAt.TryGetValue(id, out float ready) ? Mathf.Max(0, ready - Time.time) : 0;
    public void ResetAbilities() { readyAt.Clear(); endsAt.Clear(); }
    public float ActiveProductionMultiplier => AbilityActive("manejo") ? 1f + Config.Ability("manejo").effect / 100f : 1f;
    public float FireDamageMultiplier => AbilityActive("brigada") ? 1f - Config.Ability("brigada").effect / 100f : 1f;

    public bool TryUseAbility(string id)
    {
        var ability = Config.Ability(id);
        var im = IndicatorManager.Instance;
        if (ability == null || im == null || im.IsGameOver || StepController.WorldInputBlocked ||
            (BiodiversityUI.Instance != null && BiodiversityUI.Instance.IsOpen) ||
            (WinScreen.Instance != null && WinScreen.Instance.IsGameWon) || RemainingCooldown(id) > 0 ||
            im.resources <= ability.cost / 10f) return false;
        im.ModifyResources(-ability.cost / 10f);
        readyAt[id] = Time.time + ability.cooldown;
        if (id == "agua") im.ModifyWater(ability.effect / 10f);
        else endsAt[id] = Time.time + ability.duration;
        return true;
    }

    public void ApplyEconomy(IndicatorManager im, float seconds)
    {
        if (GraceActive) { ResourceRate = WaterRate = CommunityRate = 0; return; }
        var c = Config;
        float retention = Mathf.Min(c.passiveCap, SpeciesBonus("araucaria")) / 100f;
        float maintenanceDiscount = Mathf.Min(c.passiveCap, SpeciesBonus("besouro_rola_bosta")) / 100f;
        float weight = MaintenanceWeight;
        int operations = ForestOperationsManager.Instance != null ? ForestOperationsManager.Instance.ActiveDroneCount : 0;
        ResourceRate = -(c.resourceUpkeep + weight * c.upkeepPerTree + operations * c.upkeepPerOperation) * (1 - maintenanceDiscount);
        WaterRate = c.waterRecovery * (im.forestHealth / 10f) * c.RecoveryFactor(im.water * 10f)
            - (c.waterConsumption + weight * c.waterPerTree) * (1 - retention);
        CommunityRate = im.water * 10f < c.criticalWater ? -c.communityDryLoss : 0;
        if (im.resources * 10f < c.criticalResources) CommunityRate -= c.communityResourceLoss;
        if (im.water >= 6f && im.resources >= 3.5f)
            CommunityRate += c.communityRecovery * c.RecoveryFactor(im.community * 10f);
        float step = seconds * im.DecayMultiplier / 600f;
        im.ModifyResources(ResourceRate * step, false);
        im.ModifyWater(WaterRate * step, false);
        im.ModifyCommunity(CommunityRate * step, false);
    }
}
