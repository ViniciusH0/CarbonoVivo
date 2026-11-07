using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LumberjackManager : MonoBehaviour
{
    public static LumberjackManager Instance { get; private set; }

    [Header("Intervalos de Invasão (segundos)")]
    [SerializeField] private float minInterval = 18f;
    [SerializeField] private float maxInterval = 32f;
    [SerializeField] private float cutTimeLimit = 15f;
    [SerializeField] private int hitsToDefeat = 3;

    public float MinInterval { get => minInterval; set => minInterval = Mathf.Max(3f, value); }
    public float MaxInterval { get => maxInterval; set => maxInterval = Mathf.Max(minInterval + 1f, value); }
    public float CutTimeLimit { get => cutTimeLimit; set => cutTimeLimit = Mathf.Max(3f, value); }
    public int HitsToDefeat { get => hitsToDefeat; set => hitsToDefeat = Mathf.Max(1, value); }

    public void SetSpawnInterval(float interval)
    {
        minInterval = Mathf.Max(3f, interval * 0.75f);
        maxInterval = Mathf.Max(minInterval + 2f, interval * 1.25f);
        currentTargetInterval = Random.Range(minInterval, maxInterval);
    }

    public void ForceSpawnLumberjack()
    {
        if (currentLumberjack != null)
        {
            Destroy(currentLumberjack.gameObject);
            currentLumberjack = null;
        }

        if (TreeSpawner.Instance != null && TreeSpawner.Instance.GetTreeCount() == 0)
        {
            InteractivePlanting.Instance?.PlantTreeRandomly();
        }

        TrySpawnLumberjack();
    }

    [Header("UI Feedback")]
    [SerializeField] private GameObject alertBanner;
    [SerializeField] private TextMeshProUGUI alertText;

    private float timer = 0f;
    private float currentTargetInterval = 20f;
    private LumberjackActive currentLumberjack = null;

    private void Awake()
    {
        Instance = this;
        currentTargetInterval = Random.Range(minInterval, maxInterval);
    }

    private void Start()
    {
        if (alertBanner != null) alertBanner.SetActive(false);
    }

    private void Update()
    {
        // Só atua se o jogo estiver rodando e não houver um lenhador ativo
        if (currentLumberjack != null) return;
        if (TreeSpawner.Instance == null || TreeSpawner.Instance.GetTreeCount() < 2) return;

        timer += Time.deltaTime;
        if (timer >= currentTargetInterval)
        {
            timer = 0f;
            currentTargetInterval = Random.Range(minInterval, maxInterval);
            TrySpawnLumberjack();
        }
    }

    public void TrySpawnLumberjack()
    {
        // 1. Checa proteção simbiótica do Tamanduá-mirim
        if (BiodiversityManager.Instance != null && BiodiversityManager.Instance.CheckTamanduaProtection())
        {
            ShowAlert("[SIMBIOSE] Tamandua-mirim afugentou o lenhador clandestino!", 3.5f, new Color(0.3f, 0.95f, 0.5f));
            return;
        }

        GameObject targetTree = TreeSpawner.Instance.GetRandomTree();
        if (targetTree == null) return;

        // 2. Calcula posição próxima à árvore (1.3m de distância)
        Vector3 treePos = targetTree.transform.position;
        Vector2 randomOffset2D = Random.insideUnitCircle.normalized * 1.35f;
        Vector3 spawnPos = new Vector3(treePos.x + randomOffset2D.x, treePos.y, treePos.z + randomOffset2D.y);

        // 3. Constrói o personagem do Lenhador em 3D
        GameObject ljObj = CreateLumberjackVisual(spawnPos, targetTree);
        currentLumberjack = ljObj.GetComponent<LumberjackActive>();
        currentLumberjack.Setup(this, targetTree, cutTimeLimit, hitsToDefeat);

        ShowAlert("[ALERTA] Lenhador atacando uma arvore! Toque nele repetidamente para defender!", 4.5f, new Color(1f, 0.35f, 0.15f));

        if (BioacousticAudioManager.Instance != null)
        {
            BioacousticAudioManager.Instance.PlayAlertSound();
        }
    }

    public void OnLumberjackRepelled(LumberjackActive lj)
    {
        if (currentLumberjack == lj)
        {
            currentLumberjack = null;
            ShowAlert("[DEFESA] Floresta protegida! O lenhador foi expulso! (+10 ppm CO2)", 3f, new Color(0.3f, 0.95f, 0.45f));

            var im = Object.FindAnyObjectByType<IndicatorManager>();
            if (im != null) im.ModifyForestHealth(0.4f);

            if (CarbonManager1.Instance != null)
            {
                CarbonManager1.Instance.AddCarbon(10f);
            }
        }
    }

    public void OnTreeCutDown(LumberjackActive lj, GameObject tree)
    {
        if (currentLumberjack == lj)
        {
            currentLumberjack = null;
            ShowAlert("[DANO] Arvore derrubada! A integridade da floresta diminuiu.", 3.5f, new Color(0.95f, 0.25f, 0.25f));

            if (TreeSpawner.Instance != null && tree != null)
            {
                TreeSpawner.Instance.RemoveSpecificTree(tree);
            }

            var im = Object.FindAnyObjectByType<IndicatorManager>();
            if (im != null)
            {
                im.ModifyForestHealth(-0.6f);
                im.ModifyWater(-0.4f);
            }
        }
    }

    public void ShowAlert(string message, float duration, Color color)
    {
        if (alertBanner == null && alertText == null)
        {
            Debug.Log("[Alerta Florestal] " + message);
            return;
        }

        StopAllCoroutines();
        StartCoroutine(AlertRoutine(message, duration, color));
    }

    private IEnumerator AlertRoutine(string message, float duration, Color color)
    {
        if (alertText != null)
        {
            alertText.text = message;
            alertText.color = color;
        }
        if (alertBanner != null) alertBanner.SetActive(true);

        yield return new WaitForSeconds(duration);

        if (alertBanner != null) alertBanner.SetActive(false);
    }

    private GameObject CreateLumberjackVisual(Vector3 position, GameObject targetTree)
    {
        GameObject root = new GameObject("Lumberjack");
        root.transform.position = position;

        // Faz o lenhador olhar em direção ao tronco da árvore
        Vector3 lookDir = (targetTree.transform.position - position);
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
        {
            root.transform.rotation = Quaternion.LookRotation(lookDir);
        }

        // Colisor generoso para cliques fáceis no mobile/PC
        var col = root.AddComponent<CapsuleCollider>();
        col.radius = 1.15f;
        col.height = 3.2f;
        col.center = new Vector3(0, 1.5f, 0);

        // Container do modelo do personagem
        GameObject model = new GameObject("CharacterModel");
        model.transform.SetParent(root.transform, false);

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

        // Materiais
        Material shirtMat = CreateMaterial(litShader, new Color(0.85f, 0.2f, 0.16f)); // Camisa xadrez vermelha
        Material skinMat = CreateMaterial(litShader, new Color(0.96f, 0.78f, 0.65f)); // Pele
        Material beanieMat = CreateMaterial(litShader, new Color(0.65f, 0.12f, 0.12f)); // Gorro de lã vermelho
        Material beardMat = CreateMaterial(litShader, new Color(0.32f, 0.18f, 0.1f)); // Barba castanha
        Material jeansMat = CreateMaterial(litShader, new Color(0.2f, 0.35f, 0.55f)); // Calça jeans
        Material bootMat = CreateMaterial(litShader, new Color(0.22f, 0.14f, 0.08f)); // Botas de couro
        Material woodMat = CreateMaterial(litShader, new Color(0.48f, 0.3f, 0.16f)); // Cabo do machado
        Material metalMat = CreateMaterial(litShader, new Color(0.82f, 0.85f, 0.9f)); // Lâmina de aço
        Material beltMat = CreateMaterial(litShader, new Color(0.18f, 0.12f, 0.08f)); // Cinto

        // 1. Tronco / Torso
        CreatePart(PrimitiveType.Cube, "Torso", model.transform, new Vector3(0, 1.0f, 0), new Vector3(0.72f, 0.85f, 0.45f), shirtMat);

        // 2. Cinto
        CreatePart(PrimitiveType.Cube, "Belt", model.transform, new Vector3(0, 0.6f, 0), new Vector3(0.74f, 0.12f, 0.47f), beltMat);
        CreatePart(PrimitiveType.Cube, "Buckle", model.transform, new Vector3(0, 0.6f, 0.25f), new Vector3(0.18f, 0.14f, 0.05f), metalMat);

        // 3. Cabeça
        CreatePart(PrimitiveType.Cube, "Head", model.transform, new Vector3(0, 1.55f, 0), new Vector3(0.48f, 0.48f, 0.48f), skinMat);

        // 4. Gorro de Lenhador
        CreatePart(PrimitiveType.Cube, "Beanie", model.transform, new Vector3(0, 1.82f, 0), new Vector3(0.52f, 0.22f, 0.52f), beanieMat);
        CreatePart(PrimitiveType.Sphere, "BeaniePompom", model.transform, new Vector3(0, 1.95f, -0.05f), new Vector3(0.16f, 0.16f, 0.16f), beanieMat);

        // 5. Barba de Lenhador
        CreatePart(PrimitiveType.Cube, "Beard", model.transform, new Vector3(0, 1.42f, 0.18f), new Vector3(0.5f, 0.32f, 0.3f), beardMat);

        // 6. Nariz e Olhos
        CreatePart(PrimitiveType.Cube, "Nose", model.transform, new Vector3(0, 1.55f, 0.27f), new Vector3(0.1f, 0.1f, 0.12f), skinMat);
        Material eyeMat = CreateMaterial(litShader, Color.black);
        CreatePart(PrimitiveType.Cube, "EyeL", model.transform, new Vector3(-0.13f, 1.62f, 0.25f), new Vector3(0.06f, 0.06f, 0.04f), eyeMat);
        CreatePart(PrimitiveType.Cube, "EyeR", model.transform, new Vector3(0.13f, 1.62f, 0.25f), new Vector3(0.06f, 0.06f, 0.04f), eyeMat);

        // 7. Pernas e Botas
        CreatePart(PrimitiveType.Cube, "LegL", model.transform, new Vector3(-0.2f, 0.3f, 0), new Vector3(0.24f, 0.6f, 0.24f), jeansMat);
        CreatePart(PrimitiveType.Cube, "LegR", model.transform, new Vector3(0.2f, 0.3f, 0), new Vector3(0.24f, 0.6f, 0.24f), jeansMat);
        CreatePart(PrimitiveType.Cube, "BootL", model.transform, new Vector3(-0.2f, 0.08f, 0.05f), new Vector3(0.26f, 0.16f, 0.34f), bootMat);
        CreatePart(PrimitiveType.Cube, "BootR", model.transform, new Vector3(0.2f, 0.08f, 0.05f), new Vector3(0.26f, 0.16f, 0.34f), bootMat);

        // 8. Braço Esquerdo
        CreatePart(PrimitiveType.Cube, "ArmL", model.transform, new Vector3(-0.46f, 1.0f, 0.12f), new Vector3(0.18f, 0.65f, 0.18f), shirtMat);

        // 9. Pivô do Braço Direito com Machado (para animar o golpe)
        GameObject armPivot = new GameObject("RightArmPivot");
        armPivot.transform.SetParent(model.transform, false);
        armPivot.transform.localPosition = new Vector3(0.46f, 1.25f, 0f);

        CreatePart(PrimitiveType.Cube, "ArmR", armPivot.transform, new Vector3(0, -0.28f, 0.1f), new Vector3(0.18f, 0.65f, 0.18f), shirtMat);

        // Machado
        GameObject axe = new GameObject("Axe");
        axe.transform.SetParent(armPivot.transform, false);
        axe.transform.localPosition = new Vector3(0, -0.45f, 0.35f);
        axe.transform.localRotation = Quaternion.Euler(30, 0, 0);

        CreatePart(PrimitiveType.Cylinder, "AxeHandle", axe.transform, Vector3.zero, new Vector3(0.06f, 0.6f, 0.06f), woodMat);
        CreatePart(PrimitiveType.Cube, "AxeBlade", axe.transform, new Vector3(0, 0.5f, 0.12f), new Vector3(0.08f, 0.32f, 0.28f), metalMat);

        // 10. Pilar de Alerta Luminoso (Beacon vertical visível de qualquer lugar da tela)
        GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beacon.name = "AlertBeacon";
        beacon.transform.SetParent(root.transform, false);
        beacon.transform.localPosition = new Vector3(0, 6.0f, 0);
        beacon.transform.localScale = new Vector3(0.6f, 6.0f, 0.6f);
        var beaconCol = beacon.GetComponent<Collider>();
        if (beaconCol != null) Destroy(beaconCol);

        var beaconRend = beacon.GetComponent<Renderer>();
        Material beaconMat = new Material(unlitShader);
        beaconMat.color = new Color(1f, 0.3f, 0.1f, 0.38f);
        SetMaterialTransparent(beaconMat);
        beaconRend.material = beaconMat;

        // 11. Anel de Perigo no chão
        GameObject dangerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dangerRing.name = "DangerRing";
        dangerRing.transform.SetParent(root.transform, false);
        dangerRing.transform.localPosition = new Vector3(0, 0.05f, 0);
        dangerRing.transform.localScale = new Vector3(3.2f, 0.02f, 3.2f);
        var ringCol = dangerRing.GetComponent<Collider>();
        if (ringCol != null) Destroy(ringCol);

        var ringRend = dangerRing.GetComponent<Renderer>();
        Material ringMat = new Material(unlitShader);
        ringMat.color = new Color(1f, 0.2f, 0.1f, 0.75f);
        SetMaterialTransparent(ringMat);
        ringRend.material = ringMat;

        // 12. Indicador 3D Superior (Billboard com corações e contagem de cliques)
        GameObject overheadGo = new GameObject("OverheadIndicator");
        overheadGo.transform.SetParent(root.transform, false);
        overheadGo.transform.localPosition = new Vector3(0, 2.95f, 0);

        var tmp = overheadGo.AddComponent<TextMeshPro>();
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = 3.6f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = "<b>! LENHADOR !</b>\n<size=90%><color=#FF4444>[ Vida: 3/3 ]</color></size>\n<size=75%><i>Toque para atacar!</i></size>";

        // Adiciona e configura o componente ativo
        var lj = root.AddComponent<LumberjackActive>();
        lj.ConfigureReferences(model.transform, armPivot.transform, axe.transform, overheadGo.transform, tmp, beaconMat, ringMat);

        return root;
    }

    private static GameObject CreatePart(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = localScale;

        var col = part.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var rend = part.GetComponent<Renderer>();
        if (rend != null) rend.material = mat;

        return part;
    }

    private static Material CreateMaterial(Shader shader, Color color)
    {
        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        mat.color = color;
        return mat;
    }

    private static void SetMaterialTransparent(Material mat)
    {
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 0); // Alpha
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
}

public class LumberjackActive : MonoBehaviour
{
    private LumberjackManager manager;
    private GameObject targetTree;
    private float timeLimit = 15f;
    private float timer = 0f;

    private int hitsRequired = 3;
    private int hitsReceived = 0;
    private bool isDefeated = false;

    private Transform modelTransform;
    private Transform armPivot;
    private Transform axeTransform;
    private Transform overheadTransform;
    private TextMeshPro overheadText;
    private Material beaconMaterial;
    private Material ringMaterial;

    private float chopTimer = 0f;
    private float staggerTimer = 0f;
    private Vector3 initialTreeLocalRot;

    public bool IsDefeated => isDefeated;

    public void ConfigureReferences(Transform model, Transform arm, Transform axe, Transform overhead, TextMeshPro tmp, Material beaconMat, Material ringMat)
    {
        modelTransform = model;
        armPivot = arm;
        axeTransform = axe;
        overheadTransform = overhead;
        overheadText = tmp;
        beaconMaterial = beaconMat;
        ringMaterial = ringMat;
    }

    public void Setup(LumberjackManager mgr, GameObject tree, float duration, int hits)
    {
        manager = mgr;
        targetTree = tree;
        timeLimit = duration;
        hitsRequired = Mathf.Max(1, hits);
        hitsReceived = 0;
        timer = 0f;

        UpdateOverheadUI();
    }

    private void Update()
    {
        if (isDefeated) return;

        // 1. Billboard do indicador superior em direção à câmera
        if (Camera.main != null && overheadTransform != null)
        {
            overheadTransform.rotation = Camera.main.transform.rotation;
        }

        // 2. Pulsação de alerta no anel e no beacon
        float pulse = 1f + Mathf.PingPong(Time.time * 3.5f, 0.3f);
        if (beaconMaterial != null)
        {
            Color c = beaconMaterial.color;
            c.a = 0.3f + Mathf.PingPong(Time.time * 2f, 0.25f);
            beaconMaterial.color = c;
        }

        // 3. Recuperação de Stagger / Knockback após clique
        if (staggerTimer > 0f)
        {
            staggerTimer -= Time.deltaTime;
            return; // Pausa o ataque enquanto está atordoado
        }

        timer += Time.deltaTime;

        // 4. Animação de Golpe do Machado (Chopping)
        if (armPivot != null)
        {
            float swing = Mathf.Sin(Time.time * 8.5f);
            armPivot.localRotation = Quaternion.Euler(swing * 38f - 15f, 0f, 0f);

            chopTimer += Time.deltaTime;
            if (chopTimer >= 0.74f)
            {
                chopTimer = 0f;
                OnAxeStrikeTree();
            }
        }

        // 5. Se o tempo limite expirar, a árvore é cortada
        if (timer >= timeLimit)
        {
            isDefeated = true;
            StartCoroutine(CutDownTreeRoutine());
        }
    }

    private void OnAxeStrikeTree()
    {
        if (targetTree == null) return;

        // Emite som de machadada
        if (BioacousticAudioManager.Instance != null)
        {
            BioacousticAudioManager.Instance.PlayChopSound();
        }

        // Partículas de lascas de madeira no tronco
        Vector3 strikePoint = Vector3.Lerp(transform.position, targetTree.transform.position, 0.65f) + Vector3.up * 1.0f;
        CreateWoodChips(strikePoint);

        // Leve tremor na árvore golpeada
        StartCoroutine(TreeWobbleRoutine());
    }

    private IEnumerator TreeWobbleRoutine()
    {
        if (targetTree == null) yield break;
        Transform t = targetTree.transform;
        Quaternion origRot = t.localRotation;
        t.localRotation = origRot * Quaternion.Euler(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
        yield return new WaitForSeconds(0.08f);
        if (targetTree != null) t.localRotation = origRot;
    }

    public void OnPlayerTapped()
    {
        if (isDefeated) return;

        hitsReceived++;
        int hitsLeft = hitsRequired - hitsReceived;

        // Efeito sonoro de golpe/impacto
        if (BioacousticAudioManager.Instance != null)
        {
            BioacousticAudioManager.Instance.PlayHitSound();
        }

        // Recuo físico (Knockback) para trás
        staggerTimer = 0.55f;
        transform.position -= transform.forward * 0.28f;

        // Flash visual nos materiais do personagem
        StartCoroutine(FlashWhiteRoutine());

        // Faíscas de impacto
        Vector3 hitPoint = transform.position + Vector3.up * 1.3f + transform.forward * 0.2f;
        CreateHitSparks(hitPoint);

        // Texto flutuante de dano
        if (hitsLeft > 0)
        {
            CreateDamagePopup(hitPoint, $"<b>-1 GOLPE!</b>", new Color(1f, 0.85f, 0.2f));
            UpdateOverheadUI();
        }
        else
        {
            // Derrotado!
            isDefeated = true;
            CreateDamagePopup(hitPoint, $"<b>AFUGENTADO!</b>", new Color(0.3f, 1f, 0.4f));
            if (overheadText != null)
            {
                overheadText.text = "<b>[ DERROTADO! ]</b>";
                overheadText.color = new Color(0.3f, 1f, 0.5f);
            }

            // Solta o machado no chão
            if (axeTransform != null)
            {
                axeTransform.SetParent(null, true);
                axeTransform.position += Vector3.down * 0.4f;
                axeTransform.rotation = Quaternion.Euler(0, 0, 90);
                Destroy(axeTransform.gameObject, 3.5f);
            }

            manager.OnLumberjackRepelled(this);
            StartCoroutine(FleeRoutine());
        }
    }

    private void UpdateOverheadUI()
    {
        if (overheadText == null) return;

        int remaining = hitsRequired - hitsReceived;
        string bars = "";

        for (int i = 0; i < hitsRequired; i++)
        {
            bars += (i < remaining) ? "<color=#FF3333>I</color> " : "<color=#666666>-</color> ";
        }

        overheadText.text = $"<b>! LENHADOR !</b>\n<size=95%>[ {bars.Trim()} ] <size=80%>(Vida: {remaining}/{hitsRequired})</size></size>\n<size=75%><i>Toque mais {remaining}x!</i></size>";
        overheadText.color = remaining == 1 ? new Color(1f, 0.35f, 0.25f) : Color.white;
    }

    private IEnumerator FlashWhiteRoutine()
    {
        if (modelTransform == null) yield break;

        var renderers = modelTransform.GetComponentsInChildren<Renderer>();
        var origColors = new Dictionary<Renderer, Color>();

        foreach (var r in renderers)
        {
            if (r.material != null)
            {
                origColors[r] = r.material.color;
                r.material.color = Color.white;
            }
        }

        yield return new WaitForSeconds(0.12f);

        foreach (var kvp in origColors)
        {
            if (kvp.Key != null && kvp.Key.material != null)
            {
                kvp.Key.material.color = kvp.Value;
            }
        }
    }

    private IEnumerator FleeRoutine()
    {
        if (BioacousticAudioManager.Instance != null)
        {
            BioacousticAudioManager.Instance.PlayFleeSound();
        }

        // Vira de costas para a árvore em pânico
        transform.Rotate(0, 180, 0);

        float t = 0f;
        float duration = 1.1f;
        Vector3 startScale = transform.localScale;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;

            // Corre para longe
            transform.position += transform.forward * Time.deltaTime * 5.5f;

            // Encolhe ao fugir para o horizonte
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress * progress);

            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator CutDownTreeRoutine()
    {
        if (targetTree != null)
        {
            Transform treeT = targetTree.transform;
            Quaternion startRot = treeT.rotation;
            Quaternion fallRot = startRot * Quaternion.Euler(85f, 0, 0);

            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                if (targetTree != null)
                {
                    treeT.rotation = Quaternion.Slerp(startRot, fallRot, t / 0.6f);
                }
                yield return null;
            }

            CreateWoodChips(treeT.position + Vector3.up * 1f);
            manager.OnTreeCutDown(this, targetTree);
        }

        // Fuga do lenhador após o corte
        StartCoroutine(FleeRoutine());
    }

    private void CreateWoodChips(Vector3 position)
    {
        GameObject chipsRoot = new GameObject("WoodChips");
        chipsRoot.transform.position = position;

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        Material chipMat = new Material(unlit);
        chipMat.color = new Color(0.72f, 0.48f, 0.25f);

        for (int i = 0; i < 5; i++)
        {
            GameObject chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chip.transform.SetParent(chipsRoot.transform, false);
            chip.transform.localScale = Vector3.one * Random.Range(0.08f, 0.16f);
            var c = chip.GetComponent<Collider>();
            if (c != null) Destroy(c);
            chip.GetComponent<Renderer>().material = chipMat;

            Vector3 vel = (Random.onUnitSphere + Vector3.up * 0.5f).normalized * Random.Range(2.5f, 4.5f);
            StartCoroutine(ChipPhysicsRoutine(chip, vel));
        }

        Destroy(chipsRoot, 0.8f);
    }

    private IEnumerator ChipPhysicsRoutine(GameObject chip, Vector3 vel)
    {
        float t = 0f;
        while (t < 0.65f && chip != null)
        {
            t += Time.deltaTime;
            vel += Vector3.down * 9.8f * Time.deltaTime;
            chip.transform.position += vel * Time.deltaTime;
            chip.transform.Rotate(150 * Time.deltaTime, 250 * Time.deltaTime, 0);
            yield return null;
        }
    }

    private void CreateHitSparks(Vector3 position)
    {
        GameObject sparksRoot = new GameObject("HitSparks");
        sparksRoot.transform.position = position;

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        Material sparkMat = new Material(unlit);
        sparkMat.color = new Color(1f, 0.9f, 0.3f);

        for (int i = 0; i < 6; i++)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spark.transform.SetParent(sparksRoot.transform, false);
            spark.transform.localScale = Vector3.one * Random.Range(0.06f, 0.12f);
            var c = spark.GetComponent<Collider>();
            if (c != null) Destroy(c);
            spark.GetComponent<Renderer>().material = sparkMat;

            Vector3 vel = Random.insideUnitSphere * 4f;
            StartCoroutine(ChipPhysicsRoutine(spark, vel));
        }

        Destroy(sparksRoot, 0.6f);
    }

    private void CreateDamagePopup(Vector3 position, string text, Color color)
    {
        GameObject popupGo = new GameObject("DamagePopup");
        popupGo.transform.position = position + Vector3.up * 0.4f;

        var tmp = popupGo.AddComponent<TextMeshPro>();
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = 4.2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;

        StartCoroutine(DamagePopupRoutine(popupGo, tmp));
    }

    private IEnumerator DamagePopupRoutine(GameObject popupGo, TextMeshPro tmp)
    {
        float t = 0f;
        float duration = 0.85f;
        Color startCol = tmp.color;

        while (t < duration && popupGo != null)
        {
            t += Time.deltaTime;
            float progress = t / duration;

            if (Camera.main != null)
            {
                popupGo.transform.rotation = Camera.main.transform.rotation;
            }

            popupGo.transform.position += Vector3.up * Time.deltaTime * 1.8f;
            tmp.color = new Color(startCol.r, startCol.g, startCol.b, 1f - progress);

            yield return null;
        }

        if (popupGo != null) Destroy(popupGo);
    }
}
