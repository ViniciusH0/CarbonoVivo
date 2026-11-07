using System.Collections;
using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("Rain Settings")]
    [SerializeField] private float minIntervalBetweenRain = 65f;
    [SerializeField] private float maxIntervalBetweenRain = 100f;
    [SerializeField] private float defaultRainDuration = 28f;

    public float MinIntervalBetweenRain { get => minIntervalBetweenRain; set => minIntervalBetweenRain = Mathf.Max(5f, value); }
    public float MaxIntervalBetweenRain { get => maxIntervalBetweenRain; set => maxIntervalBetweenRain = Mathf.Max(minIntervalBetweenRain + 1f, value); }
    public float DefaultRainDuration { get => defaultRainDuration; set => defaultRainDuration = Mathf.Max(3f, value); }

    public void SetRainInterval(float interval)
    {
        minIntervalBetweenRain = Mathf.Max(5f, interval * 0.8f);
        maxIntervalBetweenRain = Mathf.Max(minIntervalBetweenRain + 2f, interval * 1.2f);
    }

    private bool isRaining = false;
    private float rainIntensity = 0f;
    private float nextRainInterval;
    private float rainCooldownTimer = 0f;
    private float currentRainDurationTimer = 0f;

    private ParticleSystem rainParticleSystem;
    private Camera mainCam;

    public bool IsRaining => isRaining;
    public float RainIntensity => rainIntensity;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        nextRainInterval = Random.Range(minIntervalBetweenRain, maxIntervalBetweenRain);
        mainCam = Camera.main;
        CreateRainParticleSystem();
    }

    private void Update()
    {
        // Interpola suavemente a intensidade da chuva (0 a 1)
        float targetIntensity = isRaining ? 1f : 0f;
        rainIntensity = Mathf.MoveTowards(rainIntensity, targetIntensity, Time.deltaTime * 0.35f);

        UpdateRainParticles();
        UpdateGameplayEffects();

        if (isRaining)
        {
            currentRainDurationTimer -= Time.deltaTime;
            if (currentRainDurationTimer <= 0f)
            {
                StopRain();
            }
        }
        else
        {
            rainCooldownTimer += Time.deltaTime;
            if (rainCooldownTimer >= nextRainInterval)
            {
                rainCooldownTimer = 0f;
                nextRainInterval = Random.Range(minIntervalBetweenRain, maxIntervalBetweenRain);
                TriggerRain(defaultRainDuration);
            }
        }
    }

    public void TriggerRain(float duration = 28f)
    {
        if (isRaining) return;

        isRaining = true;
        currentRainDurationTimer = duration;

        if (BioacousticAudioManager.Instance != null)
        {
            BioacousticAudioManager.Instance.StartRainSound();
        }

        if (LumberjackManager.Instance != null)
        {
            LumberjackManager.Instance.ShowAlert(
                "[CLIMA] Chuva tropical no bioma! Recuperando lencois freaticos e acelerando a fotossintese (+35%).",
                4.5f,
                new Color(0.25f, 0.85f, 1f)
            );
        }
    }

    public void StopRain()
    {
        if (!isRaining) return;

        isRaining = false;

        if (BioacousticAudioManager.Instance != null)
        {
            BioacousticAudioManager.Instance.StopRainSound();
        }

        if (LumberjackManager.Instance != null)
        {
            LumberjackManager.Instance.ShowAlert(
                "[CLIMA] O ceu se abre! A vegetacao absorveu a agua da chuva.",
                3.5f,
                new Color(0.4f, 0.95f, 0.7f)
            );
        }
    }

    private void CreateRainParticleSystem()
    {
        GameObject rainGo = new GameObject("RainParticleSystem");
        rainGo.transform.SetParent(transform, false);

        // Posiciona no centro da floresta
        Vector3 centerPos = new Vector3(-85f, 55f, 90f);
        rainGo.transform.position = centerPos;

        rainParticleSystem = rainGo.AddComponent<ParticleSystem>();
        rainParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = rainParticleSystem.main;
        main.loop = true;
        main.startLifetime = 1.9f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(26f, 34f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.18f);
        main.startColor = new Color(0.75f, 0.88f, 0.98f, 0.45f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 800;

        var emission = rainParticleSystem.emission;
        emission.rateOverTime = 0f; // Controlado dinamicamente

        var shape = rainParticleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(140f, 15f, 140f);

        var velocity = rainParticleSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-4f); // Ventinho lateral tropical
        velocity.y = new ParticleSystem.MinMaxCurve(-28f); // Queda d'água veloz
        velocity.z = new ParticleSystem.MinMaxCurve(1.5f);

        var rend = rainParticleSystem.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Stretch;
        rend.velocityScale = 0.05f; // Alongamento das gotas em traços nítidos
        rend.lengthScale = 2.4f;

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        Material mat = new Material(unlit);
        mat.color = new Color(0.72f, 0.86f, 1f, 0.5f);

        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        rend.material = mat;
        rainParticleSystem.Play();
    }

    private void UpdateRainParticles()
    {
        if (rainParticleSystem == null) return;

        // Se a câmera se mover, posiciona a caixa de chuva acompanhando a câmera
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null)
        {
            Vector3 camPos = mainCam.transform.position;
            rainParticleSystem.transform.position = new Vector3(camPos.x, 50f, camPos.z + 180f);
        }

        var emission = rainParticleSystem.emission;
        emission.rateOverTime = rainIntensity * 450f;
    }

    private void UpdateGameplayEffects()
    {
        if (rainIntensity <= 0.05f) return;

        // Regeneração contínua da água da região durante a chuva
        if (IndicatorManager.Instance != null)
        {
            IndicatorManager.Instance.ModifyWater(Time.deltaTime * 0.085f * rainIntensity);
            IndicatorManager.Instance.ModifyForestHealth(Time.deltaTime * 0.025f * rainIntensity);
        }
    }
}
