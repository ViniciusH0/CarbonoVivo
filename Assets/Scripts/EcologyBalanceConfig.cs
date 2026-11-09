using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public sealed class BalanceValueAttribute : Attribute
{
    public readonly string label, unit;
    public readonly float min, max;
    public BalanceValueAttribute(string label, float min, float max, string unit = "%")
    { this.label = label; this.min = min; this.max = max; this.unit = unit; }
}

[Serializable]
public class TreeBalance
{
    public string id, name;
    [BalanceValue("Captura da árvore adulta", 0, 500)] public float production = 100;
    [BalanceValue("Multiplicador do custo", 0, 10, "x")] public float cost = 1;
    [BalanceValue("Crescimento", 1, 180, "s")] public float growth = 10;
    [BalanceValue("Peso na manutenção", 0, 10, "x")] public float maintenance = 1;
    public TreeBalance(string id, string name, float production, float cost, float growth, float maintenance)
    { this.id = id; this.name = name; this.production = production; this.cost = cost; this.growth = growth; this.maintenance = maintenance; }
}

[Serializable]
public class SpeciesBalance
{
    public string id, name;
    [BalanceValue("Custo para desbloquear", 0, 500, "ppm")] public float unlockCost;
    [BalanceValue("Custo de evolução II", 0, 800, "ppm")] public float upgradeII = 90;
    [BalanceValue("Custo de evolução III", 0, 1000, "ppm")] public float upgradeIII = 180;
    [BalanceValue("Habilidade I", 0, 90)] public float bonusI;
    [BalanceValue("Habilidade II", 0, 90)] public float bonusII;
    [BalanceValue("Habilidade III", 0, 90)] public float bonusIII;
    public SpeciesBalance(string id, string name, float cost, float first, float second, float third)
    { this.id = id; this.name = name; unlockCost = cost; bonusI = first; bonusII = second; bonusIII = third; }
    public float Bonus(int level) => level <= 0 ? 0 : level == 1 ? bonusI : level == 2 ? bonusII : bonusIII;
}

[Serializable]
public class AbilityBalance
{
    public string id, name;
    [BalanceValue("Efeito", 0, 90)] public float effect;
    [BalanceValue("Custo de Recursos", 0, 50, "p.p.")] public float cost;
    [BalanceValue("Duração", 1, 120, "s")] public float duration = 20;
    [BalanceValue("Recarga", 1, 240, "s")] public float cooldown = 60;
    public AbilityBalance(string id, string name, float effect, float cost, float cooldown)
    { this.id = id; this.name = name; this.effect = effect; this.cost = cost; this.cooldown = cooldown; }
}

[Serializable]
public class EcologyBalanceConfig
{
    [BalanceValue("Distância mínima entre árvores", 1, 20, "m")] public float treeSpacing = 6;
    [BalanceValue("Carência inicial", 0, 180, "s")] public float graceSeconds = 60;
    [BalanceValue("Manutenção base de Recursos", 0, 30, "p.p./min")] public float resourceUpkeep = 5;
    [BalanceValue("Recursos por peso de árvore", 0, 2, "p.p./min")] public float upkeepPerTree = 0.15f;
    [BalanceValue("Recursos por drone ativo", 0, 5, "p.p./min")] public float upkeepPerOperation = 0.5f;
    [BalanceValue("Consumo base de Água", 0, 25, "p.p./min")] public float waterConsumption = 2;
    [BalanceValue("Água por peso de árvore", 0, 2, "p.p./min")] public float waterPerTree = 0.08f;
    [BalanceValue("Recuperação máxima de Água", 0, 30, "p.p./min")] public float waterRecovery = 8;
    [BalanceValue("Recuperação da Comunidade", 0, 15, "p.p./min")] public float communityRecovery = 1;
    [BalanceValue("Perda da Comunidade na seca", 0, 25, "p.p./min")] public float communityDryLoss = 6;
    [BalanceValue("Perda da Comunidade por escassez", 0, 25, "p.p./min")] public float communityResourceLoss = 4;
    [BalanceValue("Água crítica para a Comunidade", 0, 70)] public float criticalWater = 40;
    [BalanceValue("Recursos críticos para a Comunidade", 0, 70)] public float criticalResources = 25;
    [BalanceValue("Início da recuperação reduzida", 40, 95)] public float highThreshold = 70;
    [BalanceValue("Recuperação restante no teto", 0, 100)] public float recoveryAtFull = 25;
    [BalanceValue("Impacto pequeno (+/-)", 1, 30, "p.p.")] public float smallImpact = 5;
    [BalanceValue("Impacto médio (++/--)", 1, 40, "p.p.")] public float mediumImpact = 10;
    [BalanceValue("Impacto grande (+++/---)", 1, 50, "p.p.")] public float largeImpact = 20;
    [BalanceValue("Limite do impacto pequeno no baralho", 0.1f, 2.9f, "pontos")] public float smallCutoff = 0.9f;
    [BalanceValue("Limite do impacto médio no baralho", 0.1f, 4, "pontos")] public float mediumCutoff = 1.9f;
    [BalanceValue("Bônus de diversidade", 0, 50)] public float diversityBonus = 10;
    [BalanceValue("Espécies adultas para diversidade", 1, 4, "espécies")] public float diversitySpecies = 3;
    [BalanceValue("Teto dos bônus passivos", 0, 90)] public float passiveCap = 40;
    [BalanceValue("Intervalo base do mico", 2, 120, "s")] public float micoInterval = 16;
    [BalanceValue("Floresta por brotamento do mico", 0, 5, "p.p.")] public float seedForestGain = 0.25f;
    [BalanceValue("Aumento de custo a cada 3 árvores", 0, 10, "ppm")] public float plantingCostStep = 2;
    [BalanceValue("Acréscimo máximo de custo", 0, 100, "ppm")] public float plantingCostCap = 12;

    public TreeBalance[] trees = {
        new TreeBalance("guapuruvu", "Guapuruvu", 100, 1, 10, 1),
        new TreeBalance("embauba", "Embaúba", 125, 1.5f, 15, 1.3f),
        new TreeBalance("araucaria", "Araucária", 160, 3, 35, 1.8f),
        new TreeBalance("jequitiba", "Jequitibá-rosa", 220, 5, 60, 3)
    };
    public SpeciesBalance[] species = {
        new SpeciesBalance("guapuruvu", "Guapuruvu · captura", 25, 10, 15, 20),
        new SpeciesBalance("araucaria", "Araucária · economia de água", 60, 10, 20, 30),
        new SpeciesBalance("mico", "Mico · redução do intervalo", 100, 0, 20, 35),
        new SpeciesBalance("gralha", "Gralha · bônus de diversidade", 80, 5, 10, 15),
        new SpeciesBalance("besouro_rola_bosta", "Besouro · economia de manutenção", 75, 5, 10, 15),
        new SpeciesBalance("tamandua_mirim", "Tamanduá · chance de proteção", 120, 20, 35, 50),
        new SpeciesBalance("embauba", "Embaúba · captura", 40, 5, 10, 15),
        new SpeciesBalance("jequitiba", "Jequitibá · captura", 150, 5, 10, 15)
    };
    public AbilityBalance[] abilities = {
        new AbilityBalance("manejo", "Mutirão de manejo", 25, 8, 60),
        new AbilityBalance("agua", "Recuperação hídrica", 15, 10, 75),
        new AbilityBalance("brigada", "Brigada de proteção", 40, 8, 60)
    };

    public TreeBalance Tree(string id) => Array.Find(trees, t => t.id == id) ?? trees[0];
    public SpeciesBalance Species(string id) => Array.Find(species, s => s.id == id);
    public AbilityBalance Ability(string id) => Array.Find(abilities, a => a.id == id);
    public float ChoiceImpact(float original)
    {
        if (Mathf.Approximately(original, 0)) return 0;
        float magnitude = Mathf.Abs(original);
        float pp = magnitude <= smallCutoff ? smallImpact : magnitude <= mediumCutoff ? mediumImpact : largeImpact;
        return Mathf.Sign(original) * pp / 10f;
    }
    public int ImpactTier(float original) => Mathf.Abs(original) <= smallCutoff ? 1 : Mathf.Abs(original) <= mediumCutoff ? 2 : 3;
    public float RecoveryFactor(float indicator) => Mathf.Lerp(1f, recoveryAtFull / 100f,
        Mathf.InverseLerp(highThreshold, 100, indicator));
}
