using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [Header("Cycle Settings")]
    [Tooltip("Duração de um ciclo completo de 24 horas em segundos reais")]
    [SerializeField] private float dayDurationSeconds = 120f;
    [Range(0f, 24f)]
    [SerializeField] private float timeOfDay = 8.5f; // Começa às 08:30 (manhã agradável)
    [SerializeField] private bool pauseCycle = false;
    [SerializeField] private float sunIntensityMultiplier = 1.0f;
    [SerializeField] private float fogDensityMultiplier = 1.0f;

    public float DayDurationSeconds { get => dayDurationSeconds; set => dayDurationSeconds = Mathf.Max(5f, value); }
    public float TimeOfDay { get => timeOfDay; set => timeOfDay = value; }
    public bool PauseCycle { get => pauseCycle; set => pauseCycle = value; }
    public float SunIntensityMultiplier { get => sunIntensityMultiplier; set => sunIntensityMultiplier = Mathf.Max(0.05f, value); }
    public float FogDensityMultiplier { get => fogDensityMultiplier; set => fogDensityMultiplier = Mathf.Max(0f, value); }

    public void SetTime(float hour)
    {
        timeOfDay = Mathf.Repeat(hour, 24f);
    }

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

        if (directionalLight != null)
        {
            directionalLight.shadows = LightShadows.Soft;
            directionalLight.shadowStrength = 0.85f;
            directionalLight.shadowBias = 0.05f;
            directionalLight.shadowNormalBias = 0.4f;
            RenderSettings.sun = directionalLight;
        }

        if (timeDisplayText == null)
        {
            FindOrCreateTimeHud();
        }
    }

    private void InitializeLightingPhases()
    {
        // 1. Alvorada / Dawn (06:00)
        dawnPhase = new LightingPhase
        {
            name = "Alvorada",
            hour = 6.0f,
            sunColor = new Color(1.0f, 0.78f, 0.54f),
            sunIntensity = 1.15f,
            skyColor = new Color(0.85f, 0.74f, 0.65f),
            equatorColor = new Color(0.42f, 0.52f, 0.35f),
            groundColor = new Color(0.24f, 0.18f, 0.12f),
            fogColor = new Color(0.85f, 0.72f, 0.62f),
            fogDensity = 0.0055f,
            cameraBgColor = new Color(0.82f, 0.68f, 0.58f)
        };

        // 2. Dia / Midday (12:00) - Verde exuberante de Mata Atlântica
        dayPhase = new LightingPhase
        {
            name = "Dia",
            hour = 12.0f,
            sunColor = new Color(1.0f, 0.96f, 0.88f),
            sunIntensity = 1.55f,
            skyColor = new Color(0.72f, 0.88f, 0.98f),
            equatorColor = new Color(0.32f, 0.58f, 0.36f), // Reflexo de clorofila da copa
            groundColor = new Color(0.25f, 0.19f, 0.14f),
            fogColor = new Color(0.65f, 0.82f, 0.84f),
            fogDensity = 0.0035f,
            cameraBgColor = new Color(0.62f, 0.78f, 0.88f)
        };

        // 3. Entardecer / Sunset (18:00)
        sunsetPhase = new LightingPhase
        {
            name = "Entardecer",
            hour = 18.0f,
            sunColor = new Color(1.0f, 0.54f, 0.24f),
            sunIntensity = 1.15f,
            skyColor = new Color(0.88f, 0.55f, 0.42f),
            equatorColor = new Color(0.48f, 0.42f, 0.26f),
            groundColor = new Color(0.28f, 0.18f, 0.12f),
            fogColor = new Color(0.88f, 0.52f, 0.38f),
            fogDensity = 0.006f,
            cameraBgColor = new Color(0.82f, 0.46f, 0.32f)
        };

        // 4. Noite / Night (00:00) - Luar místico e musgo noturno
        nightPhase = new LightingPhase
        {
            name = "Noite",
            hour = 24.0f, // e 0.0f
            sunColor = new Color(0.45f, 0.62f, 0.88f), // Luz da Lua
            sunIntensity = 0.42f,
            skyColor = new Color(0.08f, 0.14f, 0.25f),
            equatorColor = new Color(0.08f, 0.16f, 0.14f),
            groundColor = new Color(0.06f, 0.08f, 0.10f),
            fogColor = new Color(0.06f, 0.12f, 0.22f),
            fogDensity = 0.0048f,
            cameraBgColor = new Color(0.05f, 0.09f, 0.16f)
        };
    }

    private void SetupForestAmbientLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
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
        }

        // Iluminação de dossel florestal (Trilight)
        Color targetSky = Color.Lerp(currentParams.skyColor, new Color(0.42f, 0.48f, 0.55f), rainWeight);
        Color targetEquator = Color.Lerp(currentParams.equatorColor, new Color(0.24f, 0.32f, 0.28f), rainWeight);
        Color targetGround = Color.Lerp(currentParams.groundColor, new Color(0.18f, 0.14f, 0.12f), rainWeight);

        RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, targetSky, Time.deltaTime * 3f);
        RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, targetEquator, Time.deltaTime * 3f);
        RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, targetGround, Time.deltaTime * 3f);

        // Neblina e profundidade atmosférica
        Color targetFogColor = Color.Lerp(currentParams.fogColor, new Color(0.45f, 0.52f, 0.58f), rainWeight);
        float targetFogDensity = Mathf.Lerp(currentParams.fogDensity, 0.012f, rainWeight) * fogDensityMultiplier;

        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetFogColor, Time.deltaTime * 3f);
        RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetFogDensity, Time.deltaTime * 3f);

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

        // Órbita solar:
        // Dia (06h às 18h): Sol sobe pelo leste (rot X 10 a 65), desce no oeste
        // Noite (18h às 06h): Lua sobe pelo lado oposto para dar iluminação suave
        float rotX;
        float rotY;

        if (timeOfDay >= 6f && timeOfDay <= 18f)
        {
            // Trajetória do Sol durante o dia
            float sunProgress = (timeOfDay - 6f) / 12f; // 0 a 1
            rotX = Mathf.Lerp(15f, 65f, Mathf.Sin(sunProgress * Mathf.PI));
            rotY = Mathf.Lerp(45f, 225f, sunProgress);
        }
        else
        {
            // Trajetória da Lua durante a noite
            float nightProgress = (timeOfDay > 18f) ? (timeOfDay - 18f) / 12f : (timeOfDay + 6f) / 12f;
            rotX = Mathf.Lerp(20f, 55f, Mathf.Sin(nightProgress * Mathf.PI));
            rotY = Mathf.Lerp(225f, 405f, nightProgress) % 360f;
        }

        directionalLight.transform.rotation = Quaternion.Slerp(
            directionalLight.transform.rotation,
            Quaternion.Euler(rotX, rotY, 0f),
            Time.deltaTime * 2f
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
