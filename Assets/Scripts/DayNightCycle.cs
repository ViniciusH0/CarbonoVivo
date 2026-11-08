using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [Header("Cycle Settings")]
    [Tooltip("Duração de um ciclo completo de 24 horas em segundos reais")]
    [SerializeField] private float dayDurationSeconds = 300f;
    [Range(0f, 24f)]
    [SerializeField] private float timeOfDay = 12.0f; // Começa no meio-dia (iluminação base perfeita original)
    [SerializeField] private bool pauseCycle = false;
    [SerializeField] private float sunIntensityMultiplier = 1.0f;
    [SerializeField] private float fogDensityMultiplier = 1.0f;

    public float DayDurationSeconds { get => dayDurationSeconds; set => dayDurationSeconds = Mathf.Max(5f, value); }
    public float TimeOfDay { get => timeOfDay; set => timeOfDay = value; }
    public bool PauseCycle { get => pauseCycle; set => pauseCycle = value; }
    public float SunIntensityMultiplier { get => sunIntensityMultiplier; set => sunIntensityMultiplier = Mathf.Max(0.05f, value); }
    public float FogDensityMultiplier { get => fogDensityMultiplier; set => fogDensityMultiplier = Mathf.Max(0f, value); }

    public void SetTime(float hour, bool instant = false)
    {
        timeOfDay = Mathf.Repeat(hour, 24f);
        if (instant && directionalLight != null)
        {
            float rotX, rotY;
            if (timeOfDay >= 6f && timeOfDay <= 18f)
            {
                float sunProgress = (timeOfDay - 6f) / 12f;
                float altitudeFactor = Mathf.Sin(sunProgress * Mathf.PI);
                rotX = Mathf.Lerp(26f, 35f, Mathf.Pow(altitudeFactor, 0.5f));
                rotY = Mathf.Lerp(-65f, -10f, sunProgress);
            }
            else
            {
                float nightProgress = (timeOfDay > 18f) ? (timeOfDay - 18f) / 12f : (timeOfDay + 6f) / 12f;
                float moonAltitudeFactor = Mathf.Sin(nightProgress * Mathf.PI);
                rotX = Mathf.Lerp(26f, 40f, Mathf.Pow(moonAltitudeFactor, 0.5f));
                rotY = Mathf.Lerp(-10f, -65f, nightProgress);
            }
            directionalLight.transform.rotation = Quaternion.Euler(rotX, rotY, 0f);
        }
    }

    [Header("Shadow Settings")]
    [SerializeField] private float dayShadowStrength = 0.85f;
    [SerializeField] private float nightShadowStrength = 0.45f;
    [SerializeField] private float targetShadowDistance = 380f;

    public float DayShadowStrength { get => dayShadowStrength; set => dayShadowStrength = Mathf.Clamp01(value); }
    public float NightShadowStrength { get => nightShadowStrength; set => nightShadowStrength = Mathf.Clamp01(value); }

    [Header("Light References")]
    [SerializeField] private Light directionalLight;

    [Header("HUD Clock Indicator")]
    [SerializeField] private TextMeshProUGUI timeDisplayText;

    [System.Serializable]
    public struct LightingPhase
    {
        public string name;
        public float hour;
        public Color sunColor;
        public float sunIntensity;
        public Color skyColor;
        public Color equatorColor;
        public Color groundColor;
        public Color fogColor;
        public float fogDensity;
        public Color cameraBgColor;
    }

    private LightingPhase dawnPhase;
    private LightingPhase dayPhase;
    private LightingPhase sunsetPhase;
    private LightingPhase nightPhase;

    private ParticleSystem firefliesParticleSystem;
    private Camera targetCamera;

    public float CurrentHour => timeOfDay;
    public bool IsNight => timeOfDay < 5f || timeOfDay >= 20f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        targetCamera = Camera.main;
        InitializeLightingPhases();
        SetupForestAmbientLighting();
        CreateFirefliesSystem();
    }

    private void Start()
    {
        if (directionalLight == null)
        {
            var lightGo = GameObject.Find("Directional Light");
            if (lightGo != null) directionalLight = lightGo.GetComponent<Light>();
        }

        EnsureShadowSettings();

        if (timeDisplayText == null)
        {
            FindOrCreateTimeHud();
        }
    }

    private void EnsureShadowSettings()
    {
        // Garante que o pipeline URP tenha distância de sombras suficiente para a câmera isométrica
        var urpAsset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            if (urpAsset.shadowDistance < targetShadowDistance)
            {
                urpAsset.shadowDistance = targetShadowDistance;
            }
        }

        if (directionalLight != null)
        {
            directionalLight.shadows = LightShadows.Soft;
            directionalLight.shadowStrength = dayShadowStrength;
            directionalLight.shadowBias = 0.02f;
            directionalLight.shadowNormalBias = 0.25f;
            RenderSettings.sun = directionalLight;
        }
    }

    private void InitializeLightingPhases()
    {
        // 1. Alvorada / Dawn (06:00) - Manhã suave, ambarina e aconchegante
        dawnPhase = new LightingPhase
        {
            name = "Alvorada",
            hour = 6.0f,
            sunColor = new Color(1.0f, 0.84f, 0.60f, 1.0f),
            sunIntensity = 1.85f,
            skyColor = new Color(0.65f, 0.72f, 0.85f, 1.0f),
            equatorColor = new Color(0.20f, 0.30f, 0.20f, 1.0f),
            groundColor = new Color(0.14f, 0.10f, 0.08f, 1.0f),
            fogColor = new Color(0.70f, 0.65f, 0.60f, 1.0f),
            fogDensity = 0.0015f,
            cameraBgColor = new Color(0.12f, 0.16f, 0.11f, 1.0f)
        };

        // 2. Dia / Golden Hour (12:00) - ILUMINAÇÃO GOLDEN HOUR CINEMATOGRÁFICA, NÍTIDA E VIBRANTE
        dayPhase = new LightingPhase
        {
            name = "Dia",
            hour = 12.0f,
            sunColor = new Color(1.0f, 0.76f, 0.42f, 1.0f), // Luz solar dourada-âmbar rica
            sunIntensity = 2.05f,                             // Realce límpido e vibrante das faces frontais
            skyColor = new Color(0.62f, 0.70f, 0.82f, 1.0f),   // Contraste cinematográfico com as sombras
            equatorColor = new Color(0.20f, 0.32f, 0.20f, 1.0f),// Reflexo verde da floresta
            groundColor = new Color(0.15f, 0.11f, 0.08f, 1.0f), // Terra terracota rica
            fogColor = new Color(0.75f, 0.68f, 0.58f, 1.0f),
            fogDensity = 0.0015f,
            cameraBgColor = new Color(0.12f, 0.16f, 0.11f, 1.0f)
        };

        // 3. Entardecer / Sunset (18:00) - Dourado alaranjado crepuscular
        sunsetPhase = new LightingPhase
        {
            name = "Entardecer",
            hour = 18.0f,
            sunColor = new Color(1.0f, 0.65f, 0.35f, 1.0f),
            sunIntensity = 1.90f,
            skyColor = new Color(0.70f, 0.58f, 0.72f, 1.0f),
            equatorColor = new Color(0.22f, 0.28f, 0.22f, 1.0f),
            groundColor = new Color(0.16f, 0.10f, 0.08f, 1.0f),
            fogColor = new Color(0.76f, 0.62f, 0.55f, 1.0f),
            fogDensity = 0.0018f,
            cameraBgColor = new Color(0.10f, 0.12f, 0.10f, 1.0f)
        };

        // 4. Noite / Night (00:00) - Luar sereno e cristalino com visibilidade preservada
        nightPhase = new LightingPhase
        {
            name = "Noite",
            hour = 24.0f, // e 0.0f
            sunColor = new Color(0.60f, 0.75f, 0.98f, 1.0f), // Luar azul translúcido
            sunIntensity = 0.75f,
            skyColor = new Color(0.24f, 0.32f, 0.45f, 1.0f),
            equatorColor = new Color(0.12f, 0.18f, 0.16f, 1.0f),
            groundColor = new Color(0.08f, 0.08f, 0.08f, 1.0f),
            fogColor = new Color(0.30f, 0.38f, 0.45f, 1.0f),
            fogDensity = 0.0020f,
            cameraBgColor = new Color(0.06f, 0.08f, 0.08f, 1.0f)
        };
    }

    private void SetupForestAmbientLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.70f, 0.82f, 1.0f);
        RenderSettings.ambientEquatorColor = new Color(0.20f, 0.32f, 0.20f, 1.0f);
        RenderSettings.ambientGroundColor = new Color(0.15f, 0.11f, 0.08f, 1.0f);
        RenderSettings.ambientIntensity = 1.0f;
        RenderSettings.fog = false;
    }

    private void Update()
    {
        if (!pauseCycle)
        {
            // Avança o horário das 24 horas
            float hoursPerSecond = 24f / Mathf.Max(10f, dayDurationSeconds);
            timeOfDay += Time.deltaTime * hoursPerSecond;
            if (timeOfDay >= 24f) timeOfDay -= 24f;
        }

        UpdateLighting();
        UpdateSunPosition();
        UpdateFireflies();
        UpdateClockHUD();
    }

    private void UpdateLighting()
    {
        // Se estiver chovendo, o WeatherManager aplica influência adicional
        float rainWeight = (WeatherManager.Instance != null) ? WeatherManager.Instance.RainIntensity : 0f;

        LightingPhase currentParams = InterpolateLighting(timeOfDay);

        // Ajusta luz direcional (Sol / Lua)
        if (directionalLight != null)
        {
            Color targetSunColor = Color.Lerp(currentParams.sunColor, new Color(0.55f, 0.62f, 0.7f), rainWeight);
            float targetSunIntensity = currentParams.sunIntensity * (1f - rainWeight * 0.65f) * sunIntensityMultiplier;

            directionalLight.color = Color.Lerp(directionalLight.color, targetSunColor, Time.deltaTime * 3f);
            directionalLight.intensity = Mathf.Lerp(directionalLight.intensity, targetSunIntensity, Time.deltaTime * 3f);

            // Sombras dinâmicas: firmes e nítidas durante o dia, suaves ao luar, difusas na chuva
            float baseShadow = (timeOfDay >= 6f && timeOfDay <= 18f) ? dayShadowStrength : nightShadowStrength;
            float targetShadowStrength = baseShadow * (1f - rainWeight * 0.5f);
            directionalLight.shadowStrength = Mathf.Lerp(directionalLight.shadowStrength, targetShadowStrength, Time.deltaTime * 3f);
        }

        // Iluminação de dossel florestal (Trilight)
        Color targetSky = Color.Lerp(currentParams.skyColor, new Color(0.42f, 0.48f, 0.55f), rainWeight);
        Color targetEquator = Color.Lerp(currentParams.equatorColor, new Color(0.24f, 0.32f, 0.28f), rainWeight);
        Color targetGround = Color.Lerp(currentParams.groundColor, new Color(0.18f, 0.14f, 0.12f), rainWeight);

        RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, targetSky, Time.deltaTime * 3f);
        RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, targetEquator, Time.deltaTime * 3f);
        RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, targetGround, Time.deltaTime * 3f);

        // Neblina e profundidade atmosférica: ativa apenas se estiver chovendo ou se configurada no painel de debug
        if (rainWeight > 0.05f)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            Color rainFogColor = new Color(0.45f, 0.52f, 0.58f, 1.0f);
            float rainFogDensity = Mathf.Lerp(0.001f, 0.009f, rainWeight) * fogDensityMultiplier;

            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, rainFogColor, Time.deltaTime * 3f);
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, rainFogDensity, Time.deltaTime * 3f);
        }
        else if (fogDensityMultiplier > 1.05f)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, currentParams.fogColor, Time.deltaTime * 3f);
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, 0.002f * (fogDensityMultiplier - 1f), Time.deltaTime * 3f);
        }
        else
        {
            RenderSettings.fog = false;
        }

        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null)
        {
            Color targetBg = Color.Lerp(currentParams.cameraBgColor, new Color(0.38f, 0.44f, 0.5f), rainWeight);
            targetCamera.backgroundColor = Color.Lerp(targetCamera.backgroundColor, targetBg, Time.deltaTime * 3f);
        }
    }

    private void UpdateSunPosition()
    {
        if (directionalLight == null) return;

        // O ângulo de iluminação Golden Hour é raiado e cinematográfico:
        // - Manhã (06h): (26, -65, 0) -> raios longos e suaves matinais
        // - Meio-dia (12h): (35, -38, 0) -> Golden Hour vibrante com sombras elegantes e frontais iluminadas
        // - Tarde (18h): (26, -10, 0) -> entardecer dourado e aconchegante
        // As faces frontais da estação e montanhas permanecem sempre limpas, vibrantes e bem iluminadas!
        float rotX;
        float rotY;

        if (timeOfDay >= 6f && timeOfDay <= 18f)
        {
            float sunProgress = (timeOfDay - 6f) / 12f; // 0 a 1
            float altitudeFactor = Mathf.Sin(sunProgress * Mathf.PI);

            rotX = Mathf.Lerp(26f, 35f, Mathf.Pow(altitudeFactor, 0.5f));
            rotY = Mathf.Lerp(-65f, -10f, sunProgress);
        }
        else
        {
            float nightProgress = (timeOfDay > 18f) ? (timeOfDay - 18f) / 12f : (timeOfDay + 6f) / 12f;
            float moonAltitudeFactor = Mathf.Sin(nightProgress * Mathf.PI);

            rotX = Mathf.Lerp(26f, 40f, Mathf.Pow(moonAltitudeFactor, 0.5f));
            rotY = Mathf.Lerp(-10f, -65f, nightProgress);
        }

        Quaternion targetRotation = Quaternion.Euler(rotX, rotY, 0f);

        float slerpSpeed = pauseCycle ? 18f : 8f;
        directionalLight.transform.rotation = Quaternion.Slerp(
            directionalLight.transform.rotation,
            targetRotation,
            Time.deltaTime * slerpSpeed
        );
    }

    private LightingPhase InterpolateLighting(float hour)
    {
        if (hour < 6f)
        {
            // 00:00 -> 06:00 (Noite para Alvorada)
            float t = hour / 6f;
            return LerpPhase(nightPhase, dawnPhase, t);
        }
        else if (hour < 12f)
        {
            // 06:00 -> 12:00 (Alvorada para Dia)
            float t = (hour - 6f) / 6f;
            return LerpPhase(dawnPhase, dayPhase, t);
        }
        else if (hour < 18f)
        {
            // 12:00 -> 18:00 (Dia para Entardecer)
            float t = (hour - 12f) / 6f;
            return LerpPhase(dayPhase, sunsetPhase, t);
        }
        else
        {
            // 18:00 -> 24:00 (Entardecer para Noite)
            float t = (hour - 18f) / 6f;
            return LerpPhase(sunsetPhase, nightPhase, t);
        }
    }

    private LightingPhase LerpPhase(LightingPhase a, LightingPhase b, float t)
    {
        return new LightingPhase
        {
            name = t < 0.5f ? a.name : b.name,
            hour = Mathf.Lerp(a.hour, b.hour, t),
            sunColor = Color.Lerp(a.sunColor, b.sunColor, t),
            sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t),
            skyColor = Color.Lerp(a.skyColor, b.skyColor, t),
            equatorColor = Color.Lerp(a.equatorColor, b.equatorColor, t),
            groundColor = Color.Lerp(a.groundColor, b.groundColor, t),
            fogColor = Color.Lerp(a.fogColor, b.fogColor, t),
            fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, t),
            cameraBgColor = Color.Lerp(a.cameraBgColor, b.cameraBgColor, t)
        };
    }

    private void CreateFirefliesSystem()
    {
        GameObject fireflyGo = new GameObject("Fireflies_Vagalumes");
        fireflyGo.transform.SetParent(transform, false);
        fireflyGo.transform.position = new Vector3(-85f, 2.5f, 90f); // Centralizado sobre o terreno da floresta

        firefliesParticleSystem = fireflyGo.AddComponent<ParticleSystem>();
        firefliesParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = firefliesParticleSystem.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.startColor = new Color(0.85f, 1f, 0.25f, 0.85f); // Amarelo-esverdeado bioluminescente
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 120;

        var emission = firefliesParticleSystem.emission;
        emission.rateOverTime = 0f; // Controlado dinamicamente em UpdateFireflies

        var shape = firefliesParticleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(150f, 6f, 150f);

        var noise = firefliesParticleSystem.noise;
        noise.enabled = true;
        noise.strength = 0.6f;
        noise.frequency = 0.35f;

        var rend = firefliesParticleSystem.GetComponent<ParticleSystemRenderer>();
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        Material mat = new Material(unlit);
        mat.color = new Color(0.88f, 1f, 0.3f, 0.9f);
        rend.material = mat;
        firefliesParticleSystem.Play();
    }

    private void UpdateFireflies()
    {
        if (firefliesParticleSystem == null) return;

        // Vagalumes brilham apenas durante a noite (entre 19:30 e 05:30)
        bool isNightTime = timeOfDay >= 19.5f || timeOfDay <= 5.5f;
        float targetRate = isNightTime ? 18f : 0f;

        var emission = firefliesParticleSystem.emission;
        emission.rateOverTime = Mathf.MoveTowards(emission.rateOverTime.constant, targetRate, Time.deltaTime * 5f);
    }

    private void FindOrCreateTimeHud()
    {
        var hudCo2 = GameObject.Find("UI_Canvas/Game/CO2");
        if (hudCo2 == null) return;

        Transform existingTime = hudCo2.transform.parent.Find("TimeClockWidget");
        if (existingTime != null)
        {
            timeDisplayText = existingTime.GetComponentInChildren<TextMeshProUGUI>();
            return;
        }

        GameObject widget = new GameObject("TimeClockWidget");
        widget.transform.SetParent(hudCo2.transform.parent, false);

        var rt = widget.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(25f, -125f);
        rt.sizeDelta = new Vector2(240f, 42f);

        var img = widget.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.08f, 0.12f, 0.16f, 0.85f);

        GameObject txtGo = new GameObject("TimeText");
        txtGo.transform.SetParent(widget.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        timeDisplayText = txtGo.AddComponent<TextMeshProUGUI>();
        timeDisplayText.font = TMP_Settings.defaultFontAsset;
        timeDisplayText.fontSize = 17;
        timeDisplayText.alignment = TextAlignmentOptions.Center;
        timeDisplayText.color = Color.white;
        timeDisplayText.raycastTarget = false;
    }

    private void UpdateClockHUD()
    {
        if (timeDisplayText == null) return;

        int hours = Mathf.FloorToInt(timeOfDay);
        int minutes = Mathf.FloorToInt((timeOfDay - hours) * 60f);

        string phaseIcon;
        if (WeatherManager.Instance != null && WeatherManager.Instance.IsRaining)
        {
            phaseIcon = "[Chuva]";
        }
        else if (timeOfDay >= 5f && timeOfDay < 8f)
        {
            phaseIcon = "[Alvorada]";
        }
        else if (timeOfDay >= 8f && timeOfDay < 17f)
        {
            phaseIcon = "[Dia]";
        }
        else if (timeOfDay >= 17f && timeOfDay < 20f)
        {
            phaseIcon = "[Entardecer]";
        }
        else
        {
            phaseIcon = "[Noite]";
        }

        timeDisplayText.text = $"<b>{hours:00}:{minutes:00}</b> <size=85%>{phaseIcon}</size>";
    }
}
