using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ForestFireManager : MonoBehaviour
{
    public static ForestFireManager Instance { get; private set; }

    [Header("Fire Settings")]
    [SerializeField] private float fireBurnDurationLimit = 28f; // Tempo até o fogo destruir a árvore se ninguém apagar
    [SerializeField] private float playerClickExtinguishAmount = 0.36f; // ~3 cliques apagam uma árvore
    [SerializeField] private float firefighterExtinguishRate = 0.22f; // Bombeiro apaga em ~4.5s de jato d'água
    [SerializeField] private int maxSimultaneousBurningTrees = 3;
    [SerializeField] private int maxFirefighters = 2;

    private readonly List<BurningTreeEffect> activeFires = new List<BurningTreeEffect>();
    private readonly List<FirefighterAgent> activeFirefighters = new List<FirefighterAgent>();
    private bool pendingFireFromNegativeChoice = false;
    private float lastFireTriggerTime = -30f;

    public bool IsFireActive => activeFires.Count > 0;
    public int ActiveFireCount => activeFires.Count;

    public static Vector3 GetBaseSpawnPoint()
    {
        float groundY = 0.5f;
        if (TreeSpawner.Instance != null && TreeSpawner.Instance.plane != null)
            groundY = TreeSpawner.Instance.plane.transform.position.y;
        return new Vector3(-83.5f, groundY + 1.2f, 48.0f);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        activeFires.RemoveAll(f => f == null || !f.IsBurning);
        activeFirefighters.RemoveAll(ff => ff == null);

        // Atualiza intensidade do áudio procedural de incêndio
        if (BioacousticAudioManager.Instance != null)
        {
            float audioIntensity = Mathf.Clamp01(activeFires.Count * 0.55f);
            BioacousticAudioManager.Instance.SetFireAudioIntensity(audioIntensity);
        }

        // Se havia um incêndio engatilhado por escolha negativa esperando uma árvore existir
        if (pendingFireFromNegativeChoice && TreeSpawner.Instance != null && TreeSpawner.Instance.GetTreeCount() > 0)
        {
            pendingFireFromNegativeChoice = false;
            TriggerForestFire(1);
        }

        // Se a chuva tropical começar, ela ajuda a apagar os incêndios gradualmente
        if (activeFires.Count > 0 && WeatherManager.Instance != null && WeatherManager.Instance.IsRaining)
        {
            for (int i = activeFires.Count - 1; i >= 0; i--)
            {
                if (activeFires[i] != null)
                {
                    activeFires[i].ApplyExtinguish(0.15f * Time.deltaTime, false);
                }
            }
        }
    }

    /// <summary>
    /// Chamado quando o jogador toma uma decisão com impacto ambiental negativo.
    /// </summary>
    public void OnNegativePlayerAction(float deltaForest, float deltaWater)
    {
        // Desperta incêndio florestal quando uma escolha prejudica a floresta, a água ou o ecossistema
        if (Time.time - lastFireTriggerTime < 6f) return;

        if (TreeSpawner.Instance == null || TreeSpawner.Instance.GetTreeCount() == 0)
        {
            pendingFireFromNegativeChoice = true;
            return;
        }

        int severity = (deltaForest <= -1.2f || deltaWater <= -1.2f) ? 2 : 1;
        TriggerForestFire(severity);
    }

    /// <summary>
    /// Inicia um evento climático de Incêndio Florestal em 1 ou mais árvores.
    /// </summary>
    public void TriggerForestFire(int treeCountToIgnite = 2)
    {
        if (TreeSpawner.Instance == null) return;

        // Se não houver árvores plantadas ainda, planta uma instantaneamente para poder demonstrar o evento
        if (TreeSpawner.Instance.GetTreeCount() == 0)
        {
            TreeSpawner.Instance.ForceSpawnSingleTree(false);
        }

        var allTrees = TreeSpawner.Instance.SpawnedTrees;
        if (allTrees == null || allTrees.Count == 0) return;

        int ignitedNow = 0;
        int targetIgnite = Mathf.Clamp(treeCountToIgnite, 1, maxSimultaneousBurningTrees);

        // Embaralha índices para pegar árvores que ainda não estão queimando
        List<GameObject> candidates = new List<GameObject>();
        foreach (var t in allTrees)
        {
            if (t == null) continue;
            var existingFire = t.GetComponent<BurningTreeEffect>();
            if (existingFire == null || !existingFire.IsBurning)
            {
                candidates.Add(t);
            }
        }

        for (int i = 0; i < candidates.Count && ignitedNow < targetIgnite && activeFires.Count < maxSimultaneousBurningTrees; i++)
        {
            int rndIdx = Random.Range(i, candidates.Count);
            GameObject chosen = candidates[rndIdx];
            candidates[rndIdx] = candidates[i];
            candidates[i] = chosen;

            IgniteTree(chosen);
            ignitedNow++;
        }

        if (ignitedNow > 0)
        {
            lastFireTriggerTime = Time.time;

            if (LumberjackManager.Instance != null)
            {
                LumberjackManager.Instance.ShowAlert(
                    "[INCÊNDIO FLORESTAL] Árvores em chamas! Clique nas árvores em fogo para ajudar os bombeiros!",
                    5.0f,
                    new Color(1f, 0.32f, 0.1f)
                );
            }

            if (BioacousticAudioManager.Instance != null)
            {
                BioacousticAudioManager.Instance.PlayAlertSound();
            }

            EnsureFirefightersDispatched();
        }
    }

    private void IgniteTree(GameObject tree)
    {
        if (tree == null) return;

        BurningTreeEffect fire = tree.GetComponent<BurningTreeEffect>();
        if (fire == null)
        {
            fire = tree.AddComponent<BurningTreeEffect>();
        }

        fire.Ignite(this, fireBurnDurationLimit, playerClickExtinguishAmount);
        if (!activeFires.Contains(fire))
        {
            activeFires.Add(fire);
        }
    }

    private void EnsureFirefightersDispatched()
    {
        activeFirefighters.RemoveAll(ff => ff == null);

        int desiredCount = Mathf.Clamp(activeFires.Count, 1, maxFirefighters);
        while (activeFirefighters.Count < desiredCount)
        {
            Vector3 spawnPos = GetBaseSpawnPoint() + new Vector3(Random.Range(-2f, 2f), 0.5f, Random.Range(-1.5f, 1.5f));
            GameObject ffObj = CreateFirefighterVisual(spawnPos);
            FirefighterAgent agent = ffObj.AddComponent<FirefighterAgent>();
            agent.Setup(this, firefighterExtinguishRate);
            activeFirefighters.Add(agent);
        }
    }

    public BurningTreeEffect GetNearestBurningTree(Vector3 fromPos, FirefighterAgent requester)
    {
        activeFires.RemoveAll(f => f == null || !f.IsBurning);
        if (activeFires.Count == 0) return null;

        BurningTreeEffect best = null;
        float bestScore = float.MaxValue;

        foreach (var fire in activeFires)
        {
            if (fire == null || !fire.IsBurning) continue;
            float dist = Vector3.Distance(fromPos, fire.transform.position);
            // Prioriza árvores que ainda não têm outro bombeiro atendendo
            bool targetedByOther = false;
            foreach (var ff in activeFirefighters)
            {
                if (ff != null && ff != requester && ff.CurrentTarget == fire)
                {
                    targetedByOther = true;
                    break;
                }
            }
            float score = dist + (targetedByOther ? 35f : 0f);
            if (score < bestScore)
            {
                bestScore = score;
                best = fire;
            }
        }

        return best;
    }

    public void OnTreeFireExtinguished(BurningTreeEffect fire, bool byPlayer)
    {
        activeFires.Remove(fire);

        if (BioacousticAudioManager.Instance != null)
        {
            BioacousticAudioManager.Instance.PlayExtinguishSound();
        }

        if (byPlayer && CarbonManager1.Instance != null)
        {
            CarbonManager1.Instance.AddCarbon(5f);
        }

        if (IndicatorManager.Instance != null)
        {
            IndicatorManager.Instance.ModifyForestHealth(0.25f, false);
        }

        if (activeFires.Count == 0 && LumberjackManager.Instance != null)
        {
            LumberjackManager.Instance.ShowAlert(
                "[INCÊNDIO CONTROLADO] As chamas foram apagadas e a floresta está a salvo! (+5 ppm CO2)",
                3.5f,
                new Color(0.25f, 0.95f, 0.55f)
            );
        }
    }

    public void OnTreeBurnedDown(BurningTreeEffect fire, GameObject tree)
    {
        activeFires.Remove(fire);

        if (LumberjackManager.Instance != null)
        {
            LumberjackManager.Instance.ShowAlert(
                "[DANO AMBIENTAL] Uma árvore foi consumida pelo incêndio florestal!",
                3.5f,
                new Color(0.95f, 0.25f, 0.2f)
            );
        }

        if (IndicatorManager.Instance != null)
        {
            IndicatorManager.Instance.ModifyForestHealth(-0.6f);
            IndicatorManager.Instance.ModifyWater(-0.3f);
        }

        if (TreeSpawner.Instance != null && tree != null)
        {
            TreeSpawner.Instance.RemoveSpecificTree(tree);
        }
    }

    /// <summary>
    /// Constrói o modelo 3D low-poly do Pequeno Bombeiro (mesma arquitetura base dos Cientistas, com traje anti-chamas e jetpack).
    /// </summary>
    public static GameObject CreateFirefighterVisual(Vector3 position)
    {
        GameObject root = new GameObject("Firefighter_Bombeiro");
        root.transform.position = position;

        GameObject model = new GameObject("CharacterModel");
        model.transform.SetParent(root.transform, false);
        model.transform.localScale = Vector3.one * 1.45f; // Pequeno bombeiro bem visível na câmera isométrica

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

        Material coatMat = CreateMat(litShader, new Color(0.92f, 0.18f, 0.12f));    // Casaco vermelho de bombeiro
        Material stripeMat = CreateMat(unlitShader, new Color(1.0f, 0.92f, 0.18f)); // Faixa refletiva amarela
        Material skinMat = CreateMat(litShader, new Color(0.95f, 0.78f, 0.64f));    // Pele
        Material helmetMat = CreateMat(litShader, new Color(0.98f, 0.82f, 0.12f));  // Capacete amarelo-ouro de bombeiro
        Material visorMat = CreateMat(unlitShader, new Color(0.35f, 0.90f, 1.0f));  // Visor térmico ciano
        Material pantsMat = CreateMat(litShader, new Color(0.18f, 0.20f, 0.24f));   // Calça térmica escura
        Material bootMat = CreateMat(litShader, new Color(0.12f, 0.12f, 0.12f));    // Botas
        Material tankMat = CreateMat(litShader, new Color(0.25f, 0.75f, 0.95f));    // Cilindros de água pressurizada / Jetpack
        Material nozzleMat = CreateMat(litShader, new Color(0.75f, 0.78f, 0.82f));  // Mangueira/Esguicho metálico

        // 1. Torso e faixa refletiva
        CreatePart(PrimitiveType.Cube, "Torso", model.transform, new Vector3(0, 1.0f, 0), new Vector3(0.68f, 0.82f, 0.44f), coatMat);
        CreatePart(PrimitiveType.Cube, "ReflectiveStripe", model.transform, new Vector3(0, 0.82f, 0), new Vector3(0.70f, 0.12f, 0.46f), stripeMat);

        // 2. Cabeça e Capacete de Bombeiro
        CreatePart(PrimitiveType.Cube, "Head", model.transform, new Vector3(0, 1.54f, 0), new Vector3(0.46f, 0.46f, 0.46f), skinMat);
        CreatePart(PrimitiveType.Cube, "HelmetDome", model.transform, new Vector3(0, 1.80f, 0), new Vector3(0.54f, 0.22f, 0.56f), helmetMat);
        CreatePart(PrimitiveType.Cube, "HelmetBrim", model.transform, new Vector3(0, 1.70f, -0.06f), new Vector3(0.62f, 0.06f, 0.68f), helmetMat);
        CreatePart(PrimitiveType.Cube, "Visor", model.transform, new Vector3(0, 1.58f, 0.24f), new Vector3(0.42f, 0.16f, 0.06f), visorMat);

        // 3. Cilindros Duplos de Água / Jetpack nas costas
        CreatePart(PrimitiveType.Cylinder, "WaterTankL", model.transform, new Vector3(-0.18f, 1.08f, -0.30f), new Vector3(0.22f, 0.36f, 0.22f), tankMat);
        CreatePart(PrimitiveType.Cylinder, "WaterTankR", model.transform, new Vector3(0.18f, 1.08f, -0.30f), new Vector3(0.22f, 0.36f, 0.22f), tankMat);

        // 4. Pernas e Botas
        CreatePart(PrimitiveType.Cube, "LegL", model.transform, new Vector3(-0.18f, 0.32f, 0), new Vector3(0.22f, 0.56f, 0.22f), pantsMat);
        CreatePart(PrimitiveType.Cube, "LegR", model.transform, new Vector3(0.18f, 0.32f, 0), new Vector3(0.22f, 0.56f, 0.22f), pantsMat);
        CreatePart(PrimitiveType.Cube, "BootL", model.transform, new Vector3(-0.18f, 0.08f, 0.04f), new Vector3(0.24f, 0.15f, 0.30f), bootMat);
        CreatePart(PrimitiveType.Cube, "BootR", model.transform, new Vector3(0.18f, 0.08f, 0.04f), new Vector3(0.24f, 0.15f, 0.30f), bootMat);

        // 5. Braços e Esguicho de Água (Hose Nozzle)
        CreatePart(PrimitiveType.Cube, "ArmL", model.transform, new Vector3(-0.44f, 1.02f, 0.16f), new Vector3(0.18f, 0.58f, 0.18f), coatMat);
        CreatePart(PrimitiveType.Cube, "ArmR", model.transform, new Vector3(0.44f, 1.02f, 0.22f), new Vector3(0.18f, 0.58f, 0.18f), coatMat);

        GameObject nozzle = CreatePart(PrimitiveType.Cylinder, "WaterNozzle", model.transform, new Vector3(0.32f, 0.92f, 0.52f), new Vector3(0.12f, 0.35f, 0.12f), nozzleMat);
        nozzle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        return root;
    }

    private static Material CreateMat(Shader s, Color c)
    {
        Material m = new Material(s);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        return m;
    }

    private static GameObject CreatePart(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        var col = go.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }
        var rend = go.GetComponent<Renderer>();
        if (rend != null) rend.sharedMaterial = mat;
        return go;
    }
}

/// <summary>
/// Componente anexado a uma árvore em chamas. Cria partículas de fogo, fumaça, fagulhas, luz trêmula e permite cliques do jogador para apagar.
/// </summary>
public class BurningTreeEffect : MonoBehaviour
{
    private ForestFireManager manager;
    private float fireHP = 1.0f; // 1.0 = fogo máximo, 0.0 = apagado
    private float burnTimer = 0f;
    private float maxBurnDuration = 28f;
    private float clickExtinguishPower = 0.36f;

    private GameObject fxContainer;
    private ParticleSystem flamePS;
    private ParticleSystem smokePS;
    private ParticleSystem emberPS;
    private Light fireLight;
    private Transform billboardTransform;
    private TextMeshPro statusText;
    private SphereCollider fireClickCollider;

    public bool IsBurning => fireHP > 0f && fxContainer != null;
    public float FireNormalized => Mathf.Clamp01(fireHP);

    public void Ignite(ForestFireManager mgr, float burnLimit, float clickPower)
    {
        manager = mgr;
        maxBurnDuration = burnLimit;
        clickExtinguishPower = clickPower;
        fireHP = 1.0f;
        burnTimer = 0f;

        if (fxContainer != null) Destroy(fxContainer);
        BuildFireVisuals();
    }

    private void BuildFireVisuals()
    {
        fxContainer = new GameObject("FireFX_Container");
        fxContainer.transform.SetParent(transform, false);
        fxContainer.transform.localPosition = Vector3.zero;
        // Compensa a rotação (-90, 0, 0) do prefab da árvore para que o fogo suba em +Y do mundo
        fxContainer.transform.rotation = Quaternion.identity;
        float invScale = transform.lossyScale.magnitude > 0.01f ? (1f / (transform.lossyScale.x)) : 1f;
        fxContainer.transform.localScale = Vector3.one * invScale;

        // Colisor amplo para facilitar o clique do jogador na árvore em chamas
        fireClickCollider = fxContainer.AddComponent<SphereCollider>();
        fireClickCollider.center = new Vector3(0f, 3.2f, 0f);
        fireClickCollider.radius = 3.2f;

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

        // 1. Partículas de Chamas (Flames)
        GameObject flameGo = new GameObject("FlamesPS");
        flameGo.transform.SetParent(fxContainer.transform, false);
        flameGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        flamePS = flameGo.AddComponent<ParticleSystem>();
        flamePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = flamePS.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 4.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.1f, 2.4f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1.0f, 0.85f, 0.15f, 0.92f),
            new Color(1.0f, 0.28f, 0.05f, 0.88f)
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 90;

        var emission = flamePS.emission;
        emission.rateOverTime = 38f;

        var shape = flamePS.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 1.15f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var sizeOverLife = flamePS.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.35f, 1.0f), new Keyframe(1f, 0.1f));
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var noise = flamePS.noise;
        noise.enabled = true;
        noise.strength = 0.85f;
        noise.frequency = 1.5f;

        var flameRend = flameGo.GetComponent<ParticleSystemRenderer>();
        Material flameMat = new Material(unlitShader);
        flameMat.color = new Color(1.0f, 0.55f, 0.1f, 0.9f);
        if (flameMat.HasProperty("_BaseColor")) flameMat.SetColor("_BaseColor", flameMat.color);
        flameRend.material = flameMat;
        flamePS.Play();

        // 2. Partículas de Fumaça Densa (Smoke)
        GameObject smokeGo = new GameObject("SmokePS");
        smokeGo.transform.SetParent(fxContainer.transform, false);
        smokeGo.transform.localPosition = new Vector3(0f, 3.8f, 0f);
        smokePS = smokeGo.AddComponent<ParticleSystem>();
        smokePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var sMain = smokePS.main;
        sMain.loop = true;
        sMain.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
        sMain.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4.2f);
        sMain.startSize = new ParticleSystem.MinMaxCurve(1.4f, 2.8f);
        sMain.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.22f, 0.20f, 0.18f, 0.65f),
            new Color(0.38f, 0.35f, 0.32f, 0.45f)
        );
        sMain.simulationSpace = ParticleSystemSimulationSpace.World;
        sMain.maxParticles = 60;

        var sEmiss = smokePS.emission;
        sEmiss.rateOverTime = 14f;

        var sShape = smokePS.shape;
        sShape.shapeType = ParticleSystemShapeType.Cone;
        sShape.angle = 22f;
        sShape.radius = 0.9f;
        sShape.rotation = new Vector3(-90f, 0f, 0f);

        var sRend = smokeGo.GetComponent<ParticleSystemRenderer>();
        Material smokeMat = new Material(unlitShader);
        smokeMat.color = new Color(0.25f, 0.24f, 0.22f, 0.55f);
        if (smokeMat.HasProperty("_BaseColor")) smokeMat.SetColor("_BaseColor", smokeMat.color);
        sRend.material = smokeMat;
        smokePS.Play();

        // 3. Fagulhas / Brasas Subindo (Embers)
        GameObject emberGo = new GameObject("EmbersPS");
        emberGo.transform.SetParent(fxContainer.transform, false);
        emberGo.transform.localPosition = new Vector3(0f, 2.0f, 0f);
        emberPS = emberGo.AddComponent<ParticleSystem>();
        emberPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var eMain = emberPS.main;
        eMain.loop = true;
        eMain.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 2.0f);
        eMain.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 6.5f);
        eMain.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
        eMain.startColor = new Color(1.0f, 0.75f, 0.2f, 1.0f);
        eMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var eEmiss = emberPS.emission;
        eEmiss.rateOverTime = 18f;

        var eShape = emberPS.shape;
        eShape.shapeType = ParticleSystemShapeType.Sphere;
        eShape.radius = 1.2f;

        var eNoise = emberPS.noise;
        eNoise.enabled = true;
        eNoise.strength = 1.4f;

        var eRend = emberGo.GetComponent<ParticleSystemRenderer>();
        Material emberMat = new Material(unlitShader);
        emberMat.color = new Color(1.0f, 0.8f, 0.2f, 1.0f);
        if (emberMat.HasProperty("_BaseColor")) emberMat.SetColor("_BaseColor", emberMat.color);
        eRend.material = emberMat;
        emberPS.Play();

        // 4. Luz Pontual Quente Trêmula
        GameObject lightGo = new GameObject("FirePointLight");
        lightGo.transform.SetParent(fxContainer.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 3.0f, 0f);
        fireLight = lightGo.AddComponent<Light>();
        fireLight.type = LightType.Point;
        fireLight.color = new Color(1.0f, 0.48f, 0.12f);
        fireLight.range = 12f;
        fireLight.intensity = 3.2f;

        // 5. Billboard Superior de Alerta ([ FOGO! CLIQUE ])
        GameObject bbObj = new GameObject("FireBillboard");
        bbObj.transform.SetParent(fxContainer.transform, false);
        bbObj.transform.localPosition = new Vector3(0f, 7.2f, 0f);
        billboardTransform = bbObj.transform;

        GameObject bgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgQuad.transform.SetParent(bbObj.transform, false);
        bgQuad.transform.localScale = new Vector3(5.2f, 1.8f, 1f);
        var qCol = bgQuad.GetComponent<Collider>();
        if (qCol != null) Destroy(qCol);
        Material bgMat = new Material(unlitShader);
        bgMat.color = new Color(0.55f, 0.10f, 0.05f, 0.92f);
        if (bgMat.HasProperty("_BaseColor")) bgMat.SetColor("_BaseColor", bgMat.color);
        bgQuad.GetComponent<Renderer>().sharedMaterial = bgMat;

        GameObject txtGo = new GameObject("FireLabel");
        txtGo.transform.SetParent(bbObj.transform, false);
        txtGo.transform.localPosition = new Vector3(0f, 0f, -0.05f);
        statusText = txtGo.AddComponent<TextMeshPro>();
        statusText.font = TMP_Settings.defaultFontAsset;
        statusText.fontSize = 4.2f;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = new Color(1f, 0.92f, 0.55f);
        statusText.text = "<b>[ FOGO! CLIQUE ]</b>\n<color=#FF6633>████</color>";
    }

    private void Update()
    {
        if (!IsBurning) return;

        // Mantém rotação mundial vertical independente da árvore
        fxContainer.transform.rotation = Quaternion.identity;
        if (transform.lossyScale.x > 0.05f)
        {
            fxContainer.transform.localScale = Vector3.one * (1f / transform.lossyScale.x);
        }

        // Faz o billboard encarar a câmera
        if (billboardTransform != null && Camera.main != null)
        {
            billboardTransform.rotation = Camera.main.transform.rotation;
            float pulse = 1.0f + Mathf.Sin(Time.time * 8f) * 0.08f;
            billboardTransform.localScale = Vector3.one * pulse;
        }

        // Cintilação da luz do fogo
        if (fireLight != null)
        {
            fireLight.intensity = (2.4f + Mathf.PerlinNoise(Time.time * 9f, 0f) * 2.2f) * fireHP;
        }

        // Escala emissão das chamas conforme fireHP
        if (flamePS != null)
        {
            var em = flamePS.emission;
            em.rateOverTime = 38f * Mathf.Clamp(fireHP, 0.2f, 1f);
        }

        burnTimer += Time.deltaTime;
        if (burnTimer >= maxBurnDuration)
        {
            // Fogo consumiu a árvore
            CleanupVisuals();
            if (manager != null)
            {
                manager.OnTreeBurnedDown(this, gameObject);
            }
            Destroy(this);
        }
    }

    public void OnPlayerExtinguishClick()
    {
        if (!IsBurning) return;

        SpawnWaterSplashBurst(transform.position + Vector3.up * 2.5f);
        ApplyExtinguish(clickExtinguishPower, true);
    }

    public void ApplyExtinguish(float amount, bool byPlayer)
    {
        if (!IsBurning) return;

        fireHP = Mathf.Clamp01(fireHP - amount);
        UpdateBillboardBar();

        if (byPlayer && BioacousticAudioManager.Instance != null)
        {
            BioacousticAudioManager.Instance.PlayExtinguishSound();
        }

        if (fireHP <= 0.01f)
        {
            fireHP = 0f;
            SpawnWaterSplashBurst(transform.position + Vector3.up * 2.5f);
            CleanupVisuals();
            if (manager != null)
            {
                manager.OnTreeFireExtinguished(this, byPlayer);
            }
            Destroy(this);
        }
    }

    private void UpdateBillboardBar()
    {
        if (statusText == null) return;
        int filled = Mathf.CeilToInt(fireHP * 4f);
        filled = Mathf.Clamp(filled, 1, 4);
        string bar = new string('█', filled) + new string('░', 4 - filled);
        statusText.text = $"<b>[ FOGO! CLIQUE ]</b>\n<color=#FF6633>{bar}</color>";
    }

    public static void SpawnWaterSplashBurst(Vector3 worldPos)
    {
        GameObject splashGo = new GameObject("WaterSplashBurst");
        splashGo.transform.position = worldPos;

        ParticleSystem ps = splashGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 7.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.25f, 0.85f, 1.0f, 0.95f),
            new Color(0.85f, 0.96f, 1.0f, 0.90f)
        );
        main.gravityModifier = 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 28) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.8f;

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        Material mat = new Material(unlit);
        mat.color = new Color(0.3f, 0.88f, 1f, 0.9f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mat.color);
        splashGo.GetComponent<ParticleSystemRenderer>().material = mat;

        ps.Play();
        Destroy(splashGo, 1.0f);
    }

    private void CleanupVisuals()
    {
        if (fxContainer != null)
        {
            Destroy(fxContainer);
            fxContainer = null;
        }
    }

    private void OnDestroy()
    {
        CleanupVisuals();
    }
}

/// <summary>
/// Agente Bombeiro que sai voando da base principal, vai até as árvores pegando fogo, lança jato de água e retorna à base.
/// </summary>
public class FirefighterAgent : MonoBehaviour
{
    private ForestFireManager manager;
    private float extinguishRate = 0.22f;
    private float flightSpeed = 15.5f;
    private float hoverHeight = 2.6f;

    private BurningTreeEffect currentTarget;
    private ParticleSystem waterJetPS;
    private ParticleSystem jetpackPS;
    private bool returningToBase = false;

    public BurningTreeEffect CurrentTarget => currentTarget;

    public void Setup(ForestFireManager mgr, float rate)
    {
        manager = mgr;
        extinguishRate = rate;
        BuildJetpackAndWaterStream();
    }

    private void BuildJetpackAndWaterStream()
    {
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

        // 1. Jato de propulsão do Jetpack
        GameObject jpGo = new GameObject("JetpackThrusterPS");
        jpGo.transform.SetParent(transform, false);
        jpGo.transform.localPosition = new Vector3(0f, 0.8f, -0.4f);
        jetpackPS = jpGo.AddComponent<ParticleSystem>();
        jetpackPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var jMain = jetpackPS.main;
        jMain.loop = true;
        jMain.startLifetime = 0.25f;
        jMain.startSpeed = 4.0f;
        jMain.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        jMain.startColor = new Color(0.45f, 0.92f, 1.0f, 0.85f);
        jMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var jEm = jetpackPS.emission;
        jEm.rateOverTime = 25f;

        var jShape = jetpackPS.shape;
        jShape.shapeType = ParticleSystemShapeType.Cone;
        jShape.angle = 12f;
        jShape.radius = 0.15f;
        jShape.rotation = new Vector3(90f, 0f, 0f);

        Material jpMat = new Material(unlit);
        jpMat.color = new Color(0.45f, 0.92f, 1.0f, 0.85f);
        if (jpMat.HasProperty("_BaseColor")) jpMat.SetColor("_BaseColor", jpMat.color);
        jpGo.GetComponent<ParticleSystemRenderer>().material = jpMat;
        jetpackPS.Play();

        // 2. Jato de água da mangueira para apagar o incêndio
        GameObject wGo = new GameObject("WaterHoseStreamPS");
        wGo.transform.SetParent(transform, false);
        wGo.transform.localPosition = new Vector3(0.45f, 1.35f, 0.85f);
        waterJetPS = wGo.AddComponent<ParticleSystem>();
        waterJetPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var wMain = waterJetPS.main;
        wMain.loop = true;
        wMain.startLifetime = 0.45f;
        wMain.startSpeed = 9.5f;
        wMain.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.52f);
        wMain.startColor = new Color(0.20f, 0.82f, 1.0f, 0.92f);
        wMain.gravityModifier = 0.6f;
        wMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var wEm = waterJetPS.emission;
        wEm.rateOverTime = 45f;

        var wShape = waterJetPS.shape;
        wShape.shapeType = ParticleSystemShapeType.Cone;
        wShape.angle = 8f;
        wShape.radius = 0.1f;

        Material wMat = new Material(unlit);
        wMat.color = new Color(0.20f, 0.82f, 1.0f, 0.92f);
        if (wMat.HasProperty("_BaseColor")) wMat.SetColor("_BaseColor", wMat.color);
        wGo.GetComponent<ParticleSystemRenderer>().material = wMat;
    }

    private void Update()
    {
        if (manager == null)
        {
            Destroy(gameObject);
            return;
        }

        float groundY = 0.5f;
        if (TreeSpawner.Instance != null && TreeSpawner.Instance.plane != null)
            groundY = TreeSpawner.Instance.plane.transform.position.y;

        // Busca uma árvore pegando fogo se não tiver alvo atual
        if (currentTarget == null || !currentTarget.IsBurning)
        {
            if (waterJetPS != null && waterJetPS.isPlaying) waterJetPS.Stop();
            currentTarget = manager.GetNearestBurningTree(transform.position, this);

            if (currentTarget == null)
            {
                returningToBase = true;
            }
            else
            {
                returningToBase = false;
            }
        }

        if (returningToBase)
        {
            Vector3 basePos = ForestFireManager.GetBaseSpawnPoint() + Vector3.up * hoverHeight;
            MoveTowardsPoint(basePos, true);

            if (Vector3.Distance(transform.position, basePos) < 1.2f)
            {
                Destroy(gameObject);
            }
            return;
        }

        // Voa até ficar próximo à árvore em chamas
        Vector3 treePos = currentTarget.transform.position;
        Vector3 toFire = (transform.position - treePos);
        toFire.y = 0f;
        if (toFire.sqrMagnitude < 0.01f) toFire = Vector3.back;
        Vector3 standPos = treePos + toFire.normalized * 3.2f + Vector3.up * (hoverHeight + Mathf.Sin(Time.time * 4.5f) * 0.25f);

        float distToStand = Vector3.Distance(transform.position, standPos);
        if (distToStand > 0.85f)
        {
            if (waterJetPS != null && waterJetPS.isPlaying) waterJetPS.Stop();
            MoveTowardsPoint(standPos, true);
        }
        else
        {
            // Paira de frente para a árvore e dispara o jato de água contínuo
            Vector3 lookDir = (treePos + Vector3.up * 2.0f) - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
            }

            // Oscilação suave de hover
            transform.position = Vector3.Lerp(transform.position, standPos, Time.deltaTime * 5f);

            if (waterJetPS != null && !waterJetPS.isPlaying)
            {
                waterJetPS.Play();
            }

            // Apaga gradualmente o fogo da árvore
            currentTarget.ApplyExtinguish(extinguishRate * Time.deltaTime, false);
        }
    }

    private void MoveTowardsPoint(Vector3 targetPos, bool tiltForward)
    {
        Vector3 dir = (targetPos - transform.position);
        Vector3 flatDir = new Vector3(dir.x, 0f, dir.z);

        if (flatDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatDir.normalized);
            if (tiltForward)
            {
                targetRot *= Quaternion.Euler(16f, 0f, 0f); // Inclinação de voo do jetpack
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPos, flightSpeed * Time.deltaTime);
    }
}
