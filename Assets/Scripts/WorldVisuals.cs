using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WorldVisuals : MonoBehaviour
{
    public static WorldVisuals Instance { get; private set; }

    [Header("URP Post-Processing")]
    [SerializeField] private Volume globalVolume;
    [SerializeField] private Light directionalLight;

    [Header("Saturation Ranges (GDD 6.1)")]
    [Tooltip("Saturação quando a floresta está degradada (cinza)")]
    [SerializeField] private float minSaturation = -85f;
    [Tooltip("Saturação quando a floresta está exuberante (verde vibrante)")]
    [SerializeField] private float maxSaturation = 15f;

    [Header("Lighting Ranges")]
    [SerializeField] private Color degradedLightColor = new Color(0.7f, 0.68f, 0.65f);
    [SerializeField] private Color vibrantLightColor = new Color(1.0f, 0.96f, 0.88f);

    private ColorAdjustments colorAdjustments;

    private void Awake()
    {
        Instance = this;
        SetupVolume();
    }

    private void SetupVolume()
    {
        // Garante que a câmera tenha pós-processamento ligado
        var cam = Camera.main;
        if (cam != null)
        {
            var urpCam = cam.GetComponent<UniversalAdditionalCameraData>();
            if (urpCam != null) urpCam.renderPostProcessing = true;
        }

        if (directionalLight == null)
        {
            var lightGo = GameObject.Find("Directional Light");
            if (lightGo != null) directionalLight = lightGo.GetComponent<Light>();
        }

        if (globalVolume == null)
        {
            var volGo = new GameObject("Global PostProcess Volume");
            volGo.transform.SetParent(transform);
            globalVolume = volGo.AddComponent<Volume>();
            globalVolume.isGlobal = true;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "DynamicEcosystemProfile";
            globalVolume.profile = profile;
        }

        if (globalVolume.profile != null)
        {
            if (!globalVolume.profile.TryGet(out colorAdjustments))
            {
                colorAdjustments = globalVolume.profile.Add<ColorAdjustments>(true);
            }
            colorAdjustments.saturation.overrideState = true;
        }
    }

    private void Update()
    {
        if (colorAdjustments == null) return;

        float healthRatio = 0.15f;
        if (IndicatorManager.Instance != null)
        {
            healthRatio = Mathf.Clamp01(IndicatorManager.Instance.forestHealth / 10f);
        }

        // Interpola a saturação suavemente
        float targetSat = Mathf.Lerp(minSaturation, maxSaturation, healthRatio);
        colorAdjustments.saturation.value = Mathf.Lerp(colorAdjustments.saturation.value, targetSat, Time.deltaTime * 2f);

        // Interpola a luz ambiente / solar
        if (directionalLight != null)
        {
            directionalLight.color = Color.Lerp(directionalLight.color, Color.Lerp(degradedLightColor, vibrantLightColor, healthRatio), Time.deltaTime * 2f);
        }
    }
}

