using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForestOperationsManager : MonoBehaviour
{
    public static ForestOperationsManager Instance { get; private set; }

    [Header("Drone Settings")]
    [SerializeField] private float droneFlightSpeed = 34f;
    [SerializeField] private float droneVerticalSpeed = 16f;
    [SerializeField] private float droneCruiseHeight = 11.5f;

    [Header("Scientist Settings")]
    [SerializeField] private int scientistCount = 3;
    [SerializeField] private float scientistWalkSpeed = 5.5f;

    private readonly List<ScientistAgent> activeScientists = new List<ScientistAgent>();
    private readonly List<PlantingDroneAgent> activeDrones = new List<PlantingDroneAgent>();

    public static Vector3 GetBaseRoofDronePad()
    {
        float groundY = 0.5f;
        if (TreeSpawner.Instance != null && TreeSpawner.Instance.plane != null)
            groundY = TreeSpawner.Instance.plane.transform.position.y;
        return new Vector3(-75.0f, groundY + 10.5f, 60.0f);
    }

    public static Vector3 GetBaseFrontDoor()
    {
        float groundY = 0.5f;
        if (TreeSpawner.Instance != null && TreeSpawner.Instance.plane != null)
            groundY = TreeSpawner.Instance.plane.transform.position.y;
        return new Vector3(-84.0f, groundY, 44.0f);
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

    private void Start()
    {
        SpawnInitialScientists();
    }

    private void Update()
    {
        activeScientists.RemoveAll(s => s == null);
        activeDrones.RemoveAll(d => d == null);

        if (activeScientists.Count < scientistCount)
        {
            SpawnInitialScientists();
        }
    }

    private void SpawnInitialScientists()
    {
        Vector3 doorPos = GetBaseFrontDoor();
        while (activeScientists.Count < scientistCount)
        {
            int idx = activeScientists.Count;
            Vector3 offset = new Vector3((idx - 1) * 2.5f, 0f, -idx * 1.8f);
            GameObject sciObj = CreateScientistVisual(doorPos + offset, idx);
            ScientistAgent agent = sciObj.AddComponent<ScientistAgent>();
            agent.Setup(this, scientistWalkSpeed, idx);
            activeScientists.Add(agent);
        }
    }

    /// <summary>
    /// Despacha um drone da base principal para ir até targetPos, descer, plantar a semente e voltar à base.
    /// </summary>
    public void DispatchPlantingDrone(Vector3 targetPos, System.Action<GameObject> onPlantedCallback = null)
    {
        Vector3 padPos = GetBaseRoofDronePad() + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
        GameObject droneObj = CreateDroneVisual(padPos);
        PlantingDroneAgent drone = droneObj.AddComponent<PlantingDroneAgent>();
        drone.StartMission(targetPos, droneFlightSpeed, droneVerticalSpeed, droneCruiseHeight, onPlantedCallback);
        activeDrones.Add(drone);

        if (BioacousticAudioManager.Instance != null)
        {
            BioacousticAudioManager.Instance.PlayDroneLaunchSound();
        }
    }

    public GameObject GetUnvisitedOrRandomTree(ScientistAgent requester, GameObject lastVisited)
    {
        if (TreeSpawner.Instance == null) return null;
        var trees = TreeSpawner.Instance.SpawnedTrees;
        if (trees == null || trees.Count == 0) return null;

        List<GameObject> candidates = new List<GameObject>();
        foreach (var t in trees)
        {
            if (t == null) continue;
            // Evita árvores pegando fogo
            var fire = t.GetComponent<BurningTreeEffect>();
            if (fire != null && fire.IsBurning) continue;

            candidates.Add(t);
        }

        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];

        // Filtra a última visitada e árvores que outros cientistas já estão inspecionando
        List<GameObject> preferred = new List<GameObject>();
        foreach (var c in candidates)
        {
            if (c == lastVisited) continue;
            bool busy = false;
            foreach (var sci in activeScientists)
            {
                if (sci != null && sci != requester && sci.TargetTree == c)
                {
                    busy = true;
                    break;
                }
            }
            if (!busy) preferred.Add(c);
        }

        if (preferred.Count > 0)
        {
            return preferred[Random.Range(0, preferred.Count)];
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    /// <summary>
    /// Constrói o modelo 3D low-poly do Drone de Reflorestamento que sai da base principal.
    /// </summary>
    public static GameObject CreateDroneVisual(Vector3 position)
    {
        GameObject root = new GameObject("PlantingDrone");
        root.transform.position = position;

        GameObject model = new GameObject("DroneModel");
        model.transform.SetParent(root.transform, false);
        model.transform.localScale = Vector3.one * 1.9f;

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

        Material bodyMat = CreateMat(litShader, new Color(0.92f, 0.95f, 0.96f));   // Branco sci-fi limpo
        Material frameMat = CreateMat(litShader, new Color(0.20f, 0.26f, 0.30f));  // Cinza grafite das hastes
        Material glowMat = CreateMat(unlitShader, new Color(0.20f, 0.95f, 0.55f)); // Verde bio-energia luminoso
        Material podMat = CreateMat(litShader, new Color(0.35f, 0.72f, 0.38f));    // Cápsula de sementes verde
        Material rotorMat = CreateMat(unlitShader, new Color(0.75f, 0.92f, 0.98f, 0.85f));

        // 1. Corpo central do Drone
        CreatePart(PrimitiveType.Cube, "ChassisCenter", model.transform, Vector3.zero, new Vector3(0.85f, 0.28f, 1.05f), bodyMat);
        CreatePart(PrimitiveType.Cube, "BioCoreDome", model.transform, new Vector3(0, 0.18f, 0), new Vector3(0.52f, 0.14f, 0.62f), glowMat);

        // 2. Cápsula de Sementes Inferior
        CreatePart(PrimitiveType.Sphere, "SeedPod", model.transform, new Vector3(0, -0.24f, 0), new Vector3(0.45f, 0.38f, 0.45f), podMat);

        // 3. Quatro Braços Diagonais e Rotores
        Vector3[] armDirs = new Vector3[]
        {
            new Vector3(-0.65f, 0.05f, 0.65f),
            new Vector3(0.65f, 0.05f, 0.65f),
            new Vector3(-0.65f, 0.05f, -0.65f),
            new Vector3(0.65f, 0.05f, -0.65f)
        };

        for (int i = 0; i < armDirs.Length; i++)
        {
            Vector3 p = armDirs[i];
            GameObject arm = CreatePart(PrimitiveType.Cube, "Arm_" + i, model.transform, p * 0.55f, new Vector3(0.14f, 0.10f, 0.85f), frameMat);
            arm.transform.localRotation = Quaternion.LookRotation(new Vector3(p.x, 0, p.z));

            // Motor e Hélice
            CreatePart(PrimitiveType.Cylinder, "Motor_" + i, model.transform, p, new Vector3(0.22f, 0.12f, 0.22f), frameMat);
            GameObject blade = CreatePart(PrimitiveType.Cube, "RotorBlade_" + i, model.transform, p + new Vector3(0, 0.14f, 0), new Vector3(0.82f, 0.03f, 0.12f), rotorMat);
            blade.AddComponent<DroneRotorSpinner>();
        }

        return root;
    }

    /// <summary>
    /// Constrói o modelo 3D low-poly do Cientista da Floresta (mesma arquitetura base dos Bombeiros).
    /// </summary>
    public static GameObject CreateScientistVisual(Vector3 position, int variantIndex)
    {
        GameObject root = new GameObject("Scientist_Cientista_" + variantIndex);
        root.transform.position = position;

        GameObject model = new GameObject("CharacterModel");
        model.transform.SetParent(root.transform, false);
        model.transform.localScale = Vector3.one * 1.50f;

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

        Color[] hairColors = new Color[]
        {
            new Color(0.22f, 0.15f, 0.10f),
            new Color(0.75f, 0.52f, 0.22f),
            new Color(0.15f, 0.15f, 0.18f)
        };

        Material coatMat = CreateMat(litShader, new Color(0.95f, 0.97f, 0.98f));    // Jaleco branco de cientista
        Material shirtMat = CreateMat(litShader, new Color(0.18f, 0.72f, 0.65f));   // Camisa verde-água/ciano sob o jaleco
        Material skinMat = CreateMat(litShader, new Color(0.95f, 0.78f, 0.65f));    // Pele
        Material hairMat = CreateMat(litShader, hairColors[variantIndex % hairColors.Length]);
        Material glassesMat = CreateMat(unlitShader, new Color(0.25f, 0.88f, 0.98f)); // Óculos/lentes de análise
        Material pantsMat = CreateMat(litShader, new Color(0.22f, 0.28f, 0.36f));   // Calça social escura
        Material bootMat = CreateMat(litShader, new Color(0.18f, 0.15f, 0.12f));    // Sapatos
        Material tabletMat = CreateMat(unlitShader, new Color(0.25f, 0.95f, 0.72f)); // Prancheta / Tablet holográfico

        // 1. Torso (Jaleco branco + camisa interna)
        CreatePart(PrimitiveType.Cube, "Torso", model.transform, new Vector3(0, 1.0f, 0), new Vector3(0.68f, 0.84f, 0.42f), coatMat);
        CreatePart(PrimitiveType.Cube, "ShirtTie", model.transform, new Vector3(0, 1.12f, 0.22f), new Vector3(0.24f, 0.45f, 0.04f), shirtMat);
        CreatePart(PrimitiveType.Cube, "CoatHem", model.transform, new Vector3(0, 0.55f, 0), new Vector3(0.70f, 0.24f, 0.44f), coatMat);

        // 2. Cabeça, Cabelo e Óculos
        CreatePart(PrimitiveType.Cube, "Head", model.transform, new Vector3(0, 1.54f, 0), new Vector3(0.46f, 0.46f, 0.46f), skinMat);
        CreatePart(PrimitiveType.Cube, "HairTop", model.transform, new Vector3(0, 1.80f, -0.02f), new Vector3(0.50f, 0.16f, 0.50f), hairMat);
        CreatePart(PrimitiveType.Cube, "Glasses", model.transform, new Vector3(0, 1.58f, 0.24f), new Vector3(0.38f, 0.12f, 0.05f), glassesMat);

        // 3. Pernas e Sapatos
        CreatePart(PrimitiveType.Cube, "LegL", model.transform, new Vector3(-0.18f, 0.30f, 0), new Vector3(0.22f, 0.55f, 0.22f), pantsMat);
        CreatePart(PrimitiveType.Cube, "LegR", model.transform, new Vector3(0.18f, 0.30f, 0), new Vector3(0.22f, 0.55f, 0.22f), pantsMat);
        CreatePart(PrimitiveType.Cube, "ShoeL", model.transform, new Vector3(-0.18f, 0.08f, 0.04f), new Vector3(0.24f, 0.14f, 0.30f), bootMat);
        CreatePart(PrimitiveType.Cube, "ShoeR", model.transform, new Vector3(0.18f, 0.08f, 0.04f), new Vector3(0.24f, 0.14f, 0.30f), bootMat);

        // 4. Braços e Bio-Scanner Tablet
        CreatePart(PrimitiveType.Cube, "ArmL", model.transform, new Vector3(-0.44f, 1.0f, 0.08f), new Vector3(0.18f, 0.60f, 0.18f), coatMat);
        CreatePart(PrimitiveType.Cube, "ArmR", model.transform, new Vector3(0.44f, 1.02f, 0.18f), new Vector3(0.18f, 0.60f, 0.18f), coatMat);

        GameObject tablet = CreatePart(PrimitiveType.Cube, "BioTablet", model.transform, new Vector3(0.38f, 0.92f, 0.42f), new Vector3(0.28f, 0.04f, 0.36f), tabletMat);
        tablet.transform.localRotation = Quaternion.Euler(25f, -15f, 0f);

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

public class DroneRotorSpinner : MonoBehaviour
{
    private void Update()
    {
        transform.Rotate(0f, 1440f * Time.deltaTime, 0f, Space.Self);
    }
}

/// <summary>
/// Controla o voo do Drone: sai da base -> voa até o ponto -> desce -> planta a semente -> sobe -> volta para a base.
/// </summary>
public class PlantingDroneAgent : MonoBehaviour
{
    private Vector3 targetGroundPos;
    private float flightSpeed = 34f;
    private float verticalSpeed = 16f;
    private float cruiseHeight = 11.5f;
    private System.Action<GameObject> onPlantedCallback;
    private GameObject landingMarker;

    public void StartMission(Vector3 targetPos, float hSpeed, float vSpeed, float height, System.Action<GameObject> callback)
    {
        targetGroundPos = targetPos;
        flightSpeed = hSpeed;
        verticalSpeed = vSpeed;
        cruiseHeight = height;
        onPlantedCallback = callback;

        CreateLandingMarker(targetPos);
        StartCoroutine(DroneMissionRoutine());
    }

    private void CreateLandingMarker(Vector3 pos)
    {
        landingMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        landingMarker.name = "DronePlantingTargetMarker";
        landingMarker.transform.position = pos + Vector3.up * 0.06f;
        landingMarker.transform.localScale = new Vector3(1.8f, 0.02f, 1.8f);
        var col = landingMarker.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        Material m = new Material(unlit);
        m.color = new Color(0.25f, 0.98f, 0.55f, 0.75f);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", m.color);
        landingMarker.GetComponent<Renderer>().material = m;
    }

    private IEnumerator DroneMissionRoutine()
    {
        Vector3 basePad = ForestOperationsManager.GetBaseRoofDronePad();
        float groundY = targetGroundPos.y;
        float flightAltitude = groundY + cruiseHeight;

        // 1. Decola verticalmente do telhado da base até a altitude de cruzeiro
        Vector3 takeoffTop = new Vector3(transform.position.x, flightAltitude, transform.position.z);
        while (Vector3.Distance(transform.position, takeoffTop) > 0.15f)
        {
            transform.position = Vector3.MoveTowards(transform.position, takeoffTop, verticalSpeed * Time.deltaTime);
            yield return null;
        }

        // 2. Voa horizontalmente até ficar exatamente sobre o ponto de plantio
        Vector3 aboveTarget = new Vector3(targetGroundPos.x, flightAltitude, targetGroundPos.z);
        while (Vector3.Distance(transform.position, aboveTarget) > 0.25f)
        {
            Vector3 dir = (aboveTarget - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(14f, 0f, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);
            }

            transform.position = Vector3.MoveTowards(transform.position, aboveTarget, flightSpeed * Time.deltaTime);
            yield return null;
        }

        // Nivela o drone
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        // 3. Desce até próximo ao solo para plantar a semente
        Vector3 plantHoverPos = new Vector3(targetGroundPos.x, groundY + 2.4f, targetGroundPos.z);
        while (Vector3.Distance(transform.position, plantHoverPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, plantHoverPos, verticalSpeed * Time.deltaTime);
            yield return null;
        }

        // 4. Planta a semente! (Pequena pausa de liberação da semente + feixe verde)
        SpawnSeedReleaseEffect(transform.position, targetGroundPos);
        yield return new WaitForSeconds(0.25f);

        if (landingMarker != null) Destroy(landingMarker);

        GameObject plantedTree = null;
        if (TreeSpawner.Instance != null)
        {
            plantedTree = TreeSpawner.Instance.SpawnTreeFromDrone(targetGroundPos);
        }

        if (BioacousticAudioManager.Instance != null)
        {
            BioacousticAudioManager.Instance.PlayPlantSound();
        }

        onPlantedCallback?.Invoke(plantedTree);

        yield return new WaitForSeconds(0.2f);

        // 5. Sobe de volta para a altitude de cruzeiro
        while (Vector3.Distance(transform.position, aboveTarget) > 0.15f)
        {
            transform.position = Vector3.MoveTowards(transform.position, aboveTarget, verticalSpeed * Time.deltaTime);
            yield return null;
        }

        // 6. Voa de volta para a base principal
        Vector3 aboveBase = new Vector3(basePad.x, flightAltitude, basePad.z);
        while (Vector3.Distance(transform.position, aboveBase) > 0.35f)
        {
            Vector3 dir = (aboveBase - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(14f, 0f, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);
            }

            transform.position = Vector3.MoveTowards(transform.position, aboveBase, flightSpeed * Time.deltaTime);
            yield return null;
        }

        // 7. Pousa de volta no heliponto da base e recolhe
        transform.rotation = Quaternion.identity;
        while (Vector3.Distance(transform.position, basePad) > 0.15f)
        {
            transform.position = Vector3.MoveTowards(transform.position, basePad, verticalSpeed * Time.deltaTime);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SpawnSeedReleaseEffect(Vector3 dronePos, Vector3 groundPos)
    {
        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.name = "SeedPlantBeam";
        beam.transform.position = (dronePos + groundPos) * 0.5f;
        beam.transform.localScale = new Vector3(0.45f, Vector3.Distance(dronePos, groundPos) * 0.5f, 0.45f);
        var col = beam.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        Material m = new Material(unlit);
        m.color = new Color(0.35f, 1.0f, 0.55f, 0.85f);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", m.color);
        beam.GetComponent<Renderer>().material = m;

        Destroy(beam, 0.35f);
    }

    private void OnDestroy()
    {
        if (landingMarker != null) Destroy(landingMarker);
    }
}

/// <summary>
/// Agente Cientista que caminha de ponto em ponto inspecionando e cuidando das árvores plantadas.
/// </summary>
public class ScientistAgent : MonoBehaviour
{
    private ForestOperationsManager manager;
    private float walkSpeed = 5.5f;
    private int scientistIndex = 0;

    private GameObject targetTree;
    private GameObject lastVisitedTree;
    private Vector3 currentDestination;
    private bool isCaring = false;
    private float careTimer = 0f;
    private float walkAnimPhase = 0f;

    private Transform modelTransform;
    private Transform legL;
    private Transform legR;
    private Transform armL;
    private Transform armR;
    private ParticleSystem careSparklesPS;

    public GameObject TargetTree => targetTree;

    public void Setup(ForestOperationsManager mgr, float speed, int index)
    {
        manager = mgr;
        walkSpeed = speed + Random.Range(-0.4f, 0.5f);
        scientistIndex = index;
        walkAnimPhase = index * 1.7f;

        modelTransform = transform.Find("CharacterModel");
        if (modelTransform != null)
        {
            legL = modelTransform.Find("LegL");
            legR = modelTransform.Find("LegR");
            armL = modelTransform.Find("ArmL");
            armR = modelTransform.Find("ArmR");
        }

        BuildCareParticles();
        PickNextDestination();
    }

    private void BuildCareParticles()
    {
        GameObject pGo = new GameObject("ScientistCareSparkles");
        pGo.transform.SetParent(transform, false);
        pGo.transform.localPosition = new Vector3(0f, 1.8f, 1.1f);

        careSparklesPS = pGo.AddComponent<ParticleSystem>();
        careSparklesPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = careSparklesPS.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.25f, 0.98f, 0.62f, 0.9f),
            new Color(0.35f, 0.92f, 1.0f, 0.9f)
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = careSparklesPS.emission;
        em.rateOverTime = 8f;

        var shape = careSparklesPS.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.7f;

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        Material mat = new Material(unlit);
        mat.color = new Color(0.3f, 0.96f, 0.68f, 0.9f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mat.color);
        pGo.GetComponent<ParticleSystemRenderer>().material = mat;
    }

    private void Update()
    {
        if (manager == null) return;

        float groundY = 0.5f;
        if (TreeSpawner.Instance != null && TreeSpawner.Instance.plane != null)
            groundY = TreeSpawner.Instance.plane.transform.position.y;

        if (isCaring)
        {
            careTimer -= Time.deltaTime;

            // Olha para a árvore sendo cuidada
            if (targetTree != null)
            {
                Vector3 lookDir = targetTree.transform.position - transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 8f);
                }
            }

            // Animação suave de análise com a prancheta/tablet
            AnimateCaringPose();

            if (careTimer <= 0f)
            {
                isCaring = false;
                if (careSparklesPS != null && careSparklesPS.isPlaying) careSparklesPS.Stop();

                // Cuidado científico gera um pequeno bônus de sequestro de carbono
                if (targetTree != null && CarbonManager1.Instance != null)
                {
                    CarbonManager1.Instance.AddCarbon(0.5f);
                }

                lastVisitedTree = targetTree;
                targetTree = null;
                PickNextDestination();
            }
            return;
        }

        // Se o cientista estava indo para uma árvore que sumiu ou pegou fogo, escolhe outro ponto
        if (targetTree != null)
        {
            var fire = targetTree.GetComponent<BurningTreeEffect>();
            if (fire != null && fire.IsBurning)
            {
                targetTree = null;
                PickNextDestination();
                return;
            }
        }
        else if (TreeSpawner.Instance != null && TreeSpawner.Instance.GetTreeCount() > 0 && Random.value < 0.02f)
        {
            // Se surgiram novas árvores enquanto patrulhava o solo, vai visitá-las
            PickNextDestination();
        }

        currentDestination.y = groundY;
        Vector3 currentPos = new Vector3(transform.position.x, groundY, transform.position.z);
        float dist = Vector3.Distance(currentPos, currentDestination);

        if (dist > 0.35f)
        {
            Vector3 moveDir = (currentDestination - currentPos).normalized;
            if (moveDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 9f);
            }

            transform.position = Vector3.MoveTowards(currentPos, currentDestination, walkSpeed * Time.deltaTime);
            AnimateWalking();
        }
        else
        {
            // Chegou ao destino! Inicia inspeção/cuidado
            isCaring = true;
            careTimer = Random.Range(3.2f, 4.8f);
            if (careSparklesPS != null && !careSparklesPS.isPlaying) careSparklesPS.Play();
        }
    }

    private void PickNextDestination()
    {
        float groundY = 0.5f;
        if (TreeSpawner.Instance != null && TreeSpawner.Instance.plane != null)
            groundY = TreeSpawner.Instance.plane.transform.position.y;

        // Tenta escolher uma árvore plantada (85% das vezes quando há árvores)
        if (manager != null && Random.value < 0.85f)
        {
            GameObject chosenTree = manager.GetUnvisitedOrRandomTree(this, lastVisitedTree);
            if (chosenTree != null)
            {
                targetTree = chosenTree;
                Vector2 circle = Random.insideUnitCircle.normalized * 1.9f;
                currentDestination = chosenTree.transform.position + new Vector3(circle.x, 0f, circle.y);
                currentDestination.y = groundY;
                return;
            }
        }

        // Caso não haja árvores ainda ou esteja retornando para coletar amostras perto da estação
        targetTree = null;
        Vector3 baseDoor = ForestOperationsManager.GetBaseFrontDoor();
        Vector3[] patrolOffsets = new Vector3[]
        {
            new Vector3(-8f, 0f, -12f),
            new Vector3(14f, 0f, -16f),
            new Vector3(-18f, 0f, -22f),
            new Vector3(6f, 0f, -28f),
            new Vector3(0f, 0f, -4f)
        };

        Vector3 offset = patrolOffsets[Random.Range(0, patrolOffsets.Length)] + new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
        currentDestination = new Vector3(baseDoor.x + offset.x, groundY, baseDoor.z + offset.z);
    }

    private void AnimateWalking()
    {
        walkAnimPhase += Time.deltaTime * 10f;
        float swing = Mathf.Sin(walkAnimPhase) * 28f;
        float bounce = Mathf.Abs(Mathf.Sin(walkAnimPhase)) * 0.14f;

        if (modelTransform != null)
        {
            modelTransform.localPosition = new Vector3(0f, bounce, 0f);
        }
        if (legL != null) legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
        if (legR != null) legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
        if (armL != null) armL.localRotation = Quaternion.Euler(-swing * 0.7f, 0f, 0f);
        if (armR != null) armR.localRotation = Quaternion.Euler(swing * 0.4f - 20f, 0f, 0f);
    }

    private void AnimateCaringPose()
    {
        float wave = Mathf.Sin(Time.time * 4f);
        if (modelTransform != null)
        {
            modelTransform.localPosition = Vector3.zero;
        }
        if (legL != null) legL.localRotation = Quaternion.identity;
        if (legR != null) legR.localRotation = Quaternion.identity;
        if (armL != null) armL.localRotation = Quaternion.Euler(-15f, 0f, 0f);
        if (armR != null) armR.localRotation = Quaternion.Euler(-38f + wave * 10f, 0f, 0f);
    }
}
