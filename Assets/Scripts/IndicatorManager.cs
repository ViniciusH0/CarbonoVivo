using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IndicatorManager : MonoBehaviour
{
    public static IndicatorManager Instance { get; private set; }

    [Header("Values (0 to 10)")]
    [Range(0, 10)] public float forestHealth = 4.0f; // Começa em 40% (patamar estável inicial)
    [Range(0, 10)] public float water = 5.0f;        // Começa em 50%
    [Range(0, 10)] public float community = 5.0f;    // Começa em 50%
    [Range(0, 10)] public float resources = 5.0f;    // Começa em 50%

    [Header("UI Text References")]
    public TextMeshProUGUI forestHealthText;
    public TextMeshProUGUI waterText;
    public TextMeshProUGUI communityText;
    public TextMeshProUGUI resourcesText;

    [Header("Conectividade Matemática (GDD 3.2 & Casual Zen)")]
    [Tooltip("Período de carência inicial (segundos) sem degradação para o jogador se ambientar")]
    [SerializeField] private float initialGracePeriod = 60f;
    [Tooltip("Taxa suave de degradação da água se a floresta for muito negligenciada")]
    [SerializeField] private float waterDecayRate = 0.012f;
    [SerializeField] private float decayMultiplier = 1.0f;
    [Tooltip("Taxa de recuperação hídrica se a mata ciliar estiver saudável")]
    [SerializeField] private float waterRegenRate = 0.04f;

    public float DecayMultiplier
    {
        get => decayMultiplier;
        set => decayMultiplier = Mathf.Max(0f, value);
    }

    public bool IsGameOver { get; private set; } = false;
    public event Action<string> OnGameOverTriggered;

    private float gameTimer = 0f;

    private readonly IndicatorVisualItem forestVisual = new IndicatorVisualItem();
    private readonly IndicatorVisualItem waterVisual = new IndicatorVisualItem();
    private readonly IndicatorVisualItem communityVisual = new IndicatorVisualItem();
    private readonly IndicatorVisualItem resourcesVisual = new IndicatorVisualItem();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Auto-busca referências caso estejam desvinculadas no Inspector
        if (forestHealthText == null) forestHealthText = GameObject.Find("UI_Canvas/Game/Indicators/Indicators/Health/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
        if (waterText == null) waterText = GameObject.Find("UI_Canvas/Game/Indicators/Indicators/Water/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
        if (communityText == null) communityText = GameObject.Find("UI_Canvas/Game/Indicators/Indicators/Community/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
        if (resourcesText == null) resourcesText = GameObject.Find("UI_Canvas/Game/Indicators/Indicators/Resources/Text (TMP)")?.GetComponent<TextMeshProUGUI>();

        forestVisual.Initialize(this, forestHealthText, forestHealth);
        waterVisual.Initialize(this, waterText, water);
        communityVisual.Initialize(this, communityText, community);
        resourcesVisual.Initialize(this, resourcesText, resources);

        UpdateUI();
    }

    private void Update()
    {
        if (IsGameOver) return;
        if (WinScreen.Instance != null && WinScreen.Instance.IsGameWon) return;

        gameTimer += Time.deltaTime;

        // Não aplica decaimento durante o tutorial ou período de carência inicial
        if (EcologySimulation.Instance != null)
        {
            EcologySimulation.Instance.ApplyEconomy(this, Time.deltaTime);
        }
        else if (gameTimer > initialGracePeriod)
        {
            ApplyInterdependence();
        }

        UpdateUI();
        CheckDefeatConditions();
    }

    private void ApplyInterdependence()
    {
        float bonusRetention = 0f;
        if (BiodiversityManager.Instance != null)
        {
            bonusRetention = BiodiversityManager.Instance.GetWaterRetentionBonus();
        }

        // Pressão climática progressiva: conforme o jogo avança e o carbono é capturado, a evaporação térmica aumenta
        float carbonRatio = 0f;
        if (CarbonManager1.Instance != null && CarbonManager1.Instance.MaxCarbon > 0f)
        {
            carbonRatio = Mathf.Clamp01(CarbonManager1.Instance.CurrentCarbon / CarbonManager1.Instance.MaxCarbon);
        }
        float climateHeatFactor = 1.0f + 0.65f * carbonRatio;

        // Se a mata ciliar estiver desprotegida (< 3.2), a evaporação drena a água da bacia
        if (forestHealth < 3.2f)
        {
            float severity = (3.2f - forestHealth) / 3.2f;
            float decay = (waterDecayRate * decayMultiplier * climateHeatFactor * severity * (1f - bonusRetention)) * Time.deltaTime;
            water = Mathf.Clamp(water - decay, 0.4f, 10f);
        }
        else if (forestHealth >= 3.8f)
        {
            // Mata ciliar saudável recupera os aquíferos e reservatórios
            float regen = (waterRegenRate * (1f + bonusRetention) * (forestHealth / 10f)) * Time.deltaTime;
            water = Mathf.Clamp(water + regen, 0f, 10f);
        }

        // Clima/Recursos se beneficiam da sinergia entre floresta e água
        if (forestHealth > 4.5f && water > 4.5f)
        {
            resources = Mathf.Clamp(resources + 0.015f * Time.deltaTime, 0f, 10f);
            community = Mathf.Clamp(community + 0.015f * Time.deltaTime, 0f, 10f);
        }
    }

    private void CheckDefeatConditions()
    {
        if (IsGameOver) return;

        // O jogo só termina se um indicador for ativamente zerado por danos/decisões
        if (forestHealth <= 0f)
        {
            TriggerDefeat("A Mata Atlântica foi completamente desmatada.");
        }
        else if (water <= 0f)
        {
            TriggerDefeat("Crise hídrica extrema: os reservatórios secaram completamente.");
        }
        else if (community <= 0f)
        {
            TriggerDefeat("A comunidade perdeu as condições de sobrevivência na região.");
        }
        else if (resources <= 0f)
        {
            TriggerDefeat("Esgotamento crítico de recursos naturais essenciais.");
        }
    }

    private void TriggerDefeat(string reason)
    {
        IsGameOver = true;
        OnGameOverTriggered?.Invoke(reason);
        Debug.LogWarning("[Colapso Ambiental] " + reason);

        var goScreen = UnityEngine.Object.FindAnyObjectByType<GameOverScreen>();
        if (goScreen != null)
        {
            goScreen.Show(reason);
        }
    }

    public void ModifyForestHealth(float amount, bool triggerVisualEffect = true)
    {
        forestHealth = Mathf.Clamp(forestHealth + amount, 0f, 10f);
        int deltaPercent = Mathf.RoundToInt((Mathf.Abs(amount) / 10f) * 100f);
        if (triggerVisualEffect && deltaPercent >= 1)
        {
            forestVisual.TriggerImpact(amount, forestHealth);
        }
    }

    public void ModifyWater(float amount, bool triggerVisualEffect = true)
    {
        water = Mathf.Clamp(water + amount, 0f, 10f);
        int deltaPercent = Mathf.RoundToInt((Mathf.Abs(amount) / 10f) * 100f);
        if (triggerVisualEffect && deltaPercent >= 1)
        {
            waterVisual.TriggerImpact(amount, water);
        }
    }

    public void ModifyCommunity(float amount, bool triggerVisualEffect = true)
    {
        community = Mathf.Clamp(community + amount, 0f, 10f);
        int deltaPercent = Mathf.RoundToInt((Mathf.Abs(amount) / 10f) * 100f);
        if (triggerVisualEffect && deltaPercent >= 1)
        {
            communityVisual.TriggerImpact(amount, community);
        }
    }

    public void ModifyResources(float amount, bool triggerVisualEffect = true)
    {
        resources = Mathf.Clamp(resources + amount, 0f, 10f);
        int deltaPercent = Mathf.RoundToInt((Mathf.Abs(amount) / 10f) * 100f);
        if (triggerVisualEffect && deltaPercent >= 1)
        {
            resourcesVisual.TriggerImpact(amount, resources);
        }
    }

    /// <summary>
    /// Regeneração ou consumo contínuo por frame (ex: chuva ou processos naturais), sem disparar popups de impacto.
    /// </summary>
    public void AddContinuousWater(float amount)
    {
        water = Mathf.Clamp(water + amount, 0f, 10f);
    }

    /// <summary>
    /// Regeneração ou consumo contínuo por frame da floresta, sem disparar popups de impacto.
    /// </summary>
    public void AddContinuousForestHealth(float amount)
    {
        forestHealth = Mathf.Clamp(forestHealth + amount, 0f, 10f);
    }

    /// <summary>
    /// Aplica as consequências ecológicas de uma escolha do jogador com feedback visual e sonoro em tempo real.
    /// </summary>
    public void ApplyChoiceImpacts(float deltaForest, float deltaWater, float deltaCommunity, float deltaResources)
    {
        forestHealth = Mathf.Clamp(forestHealth + deltaForest, 0f, 10f);
        water = Mathf.Clamp(water + deltaWater, 0f, 10f);
        community = Mathf.Clamp(community + deltaCommunity, 0f, 10f);
        resources = Mathf.Clamp(resources + deltaResources, 0f, 10f);

        int dpForest = Mathf.RoundToInt((Mathf.Abs(deltaForest) / 10f) * 100f);
        int dpWater = Mathf.RoundToInt((Mathf.Abs(deltaWater) / 10f) * 100f);
        int dpComm = Mathf.RoundToInt((Mathf.Abs(deltaCommunity) / 10f) * 100f);
        int dpRes = Mathf.RoundToInt((Mathf.Abs(deltaResources) / 10f) * 100f);

        if (dpForest >= 1) forestVisual.TriggerImpact(deltaForest, forestHealth);
        if (dpWater >= 1) waterVisual.TriggerImpact(deltaWater, water);
        if (dpComm >= 1) communityVisual.TriggerImpact(deltaCommunity, community);
        if (dpRes >= 1) resourcesVisual.TriggerImpact(deltaResources, resources);

        bool hasNegative = (dpForest >= 1 && deltaForest < 0) || (dpWater >= 1 && deltaWater < 0) || (dpComm >= 1 && deltaCommunity < 0) || (dpRes >= 1 && deltaResources < 0);
        bool hasPositive = (dpForest >= 1 && deltaForest > 0) || (dpWater >= 1 && deltaWater > 0) || (dpComm >= 1 && deltaCommunity > 0) || (dpRes >= 1 && deltaResources > 0);

        if (BioacousticAudioManager.Instance != null)
        {
            if (hasNegative) BioacousticAudioManager.Instance.PlayNegativeWarning();
            else if (hasPositive) BioacousticAudioManager.Instance.PlayPositiveChime();
        }

        // Ações negativas do jogador (especialmente danos à floresta, água ou recursos) podem despertar um Incêndio Florestal
        if (hasNegative && ForestFireManager.Instance != null)
        {
            ForestFireManager.Instance.OnNegativePlayerAction(deltaForest, deltaWater);
        }
    }

    public void RestoreAll(float value = 10f)
    {
        float df = value - forestHealth;
        float dw = value - water;
        float dc = value - community;
        float dr = value - resources;

        forestHealth = Mathf.Clamp(value, 0f, 10f);
        water = Mathf.Clamp(value, 0f, 10f);
        community = Mathf.Clamp(value, 0f, 10f);
        resources = Mathf.Clamp(value, 0f, 10f);

        if (Mathf.Abs(df) > 0.001f) forestVisual.TriggerImpact(df, forestHealth);
        if (Mathf.Abs(dw) > 0.001f) waterVisual.TriggerImpact(dw, water);
        if (Mathf.Abs(dc) > 0.001f) communityVisual.TriggerImpact(dc, community);
        if (Mathf.Abs(dr) > 0.001f) resourcesVisual.TriggerImpact(dr, resources);
    }

    private void UpdateUI()
    {
        forestVisual.UpdateIdleDisplay(forestHealth);
        waterVisual.UpdateIdleDisplay(water);
        communityVisual.UpdateIdleDisplay(community);
        resourcesVisual.UpdateIdleDisplay(resources);
    }

    [System.Serializable]
    public class IndicatorVisualItem
    {
        public TextMeshProUGUI numberText;
        public RectTransform containerRect;
        public Graphic backgroundGraphic;
        public TextMeshProUGUI labelText;

        public float targetValue;
        public float displayedValue;

        private Color defaultTextColor = Color.white;
        private Color defaultBgColor = Color.white;
        private Vector2 baseAnchoredPosition;
        private Coroutine impactCoroutine;
        private MonoBehaviour host;
        private GameObject currentPopup;
        private float lastImpactTime = -10f;

        private static readonly Color POSITIVE_TEXT_COLOR = new Color(0.18f, 0.95f, 0.42f, 1f); // Verde brilhante
        private static readonly Color NEGATIVE_TEXT_COLOR = new Color(1f, 0.25f, 0.22f, 1f); // Vermelho vibrante de alerta
        private static readonly Color POSITIVE_BG_TINT = new Color(0.82f, 0.96f, 0.86f, 1f); // Leve brilho esmeralda
        private static readonly Color NEGATIVE_BG_TINT = new Color(0.98f, 0.84f, 0.84f, 1f); // Leve brilho coral/carmesim

        public void Initialize(MonoBehaviour runner, TextMeshProUGUI text, float initialVal)
        {
            host = runner;
            numberText = text;
            targetValue = initialVal;
            displayedValue = initialVal;

            if (numberText != null)
            {
                defaultTextColor = numberText.color;
                if (defaultTextColor.a < 0.1f) defaultTextColor = Color.white;

                Transform parent = numberText.transform.parent;
                if (parent != null)
                {
                    containerRect = parent as RectTransform;
                    if (containerRect != null) baseAnchoredPosition = containerRect.anchoredPosition;

                    Transform surf = parent.Find("OrganicSurface");
                    if (surf != null) backgroundGraphic = surf.GetComponent<Graphic>();
                    if (backgroundGraphic == null) backgroundGraphic = parent.GetComponent<Graphic>();
                    if (backgroundGraphic != null) defaultBgColor = backgroundGraphic.color;

                    Transform lbl = parent.Find("MetricLabel");
                    if (lbl != null) labelText = lbl.GetComponent<TextMeshProUGUI>();
                }

                numberText.text = $"{Mathf.RoundToInt((displayedValue / 10f) * 100f)}%";
            }
        }

        public void UpdateIdleDisplay(float targetVal)
        {
            targetValue = targetVal;

            // Se não houver animação pontual em curso, ajusta suavemente qualquer transição sutil contínua
            if (impactCoroutine == null && numberText != null)
            {
                displayedValue = Mathf.MoveTowards(displayedValue, targetValue, 2.5f * Time.unscaledDeltaTime);
                numberText.text = $"{Mathf.RoundToInt((displayedValue / 10f) * 100f)}%";
            }
        }

        public void TriggerImpact(float delta, float newTargetVal)
        {
            targetValue = newTargetVal;
            int deltaPercent = Mathf.RoundToInt((delta / 10f) * 100f);
            if (deltaPercent == 0) return;

            // Evita disparos múltiplos e repetidos em fração de segundo
            if (Time.unscaledTime - lastImpactTime < 0.25f) return;
            lastImpactTime = Time.unscaledTime;

            if (host == null || numberText == null)
            {
                displayedValue = targetValue;
                if (numberText != null) numberText.text = $"{Mathf.RoundToInt((displayedValue / 10f) * 100f)}%";
                return;
            }

            if (impactCoroutine != null)
            {
                host.StopCoroutine(impactCoroutine);
                impactCoroutine = null;
            }

            if (currentPopup != null)
            {
                UnityEngine.Object.Destroy(currentPopup);
                currentPopup = null;
            }

            if (containerRect != null)
            {
                containerRect.localScale = Vector3.one;
                containerRect.anchoredPosition = baseAnchoredPosition;
            }

            impactCoroutine = host.StartCoroutine(ImpactAnimationRoutine(delta));
        }

        private IEnumerator ImpactAnimationRoutine(float delta)
        {
            bool isPositive = delta > 0f;
            int deltaPercent = Mathf.RoundToInt((delta / 10f) * 100f);
            float startVal = displayedValue;
            float endVal = targetValue;

            Color targetColor = isPositive ? POSITIVE_TEXT_COLOR : NEGATIVE_TEXT_COLOR;
            Color targetBgColor = isPositive ? POSITIVE_BG_TINT : NEGATIVE_BG_TINT;

            // 1. Spawna o popup indicador flutuante com a mudança (+X% ou -X%)
            SpawnDeltaPopup(deltaPercent, isPositive);

            float duration = 1.25f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // 2. Mudança gradual dos números com curva suave ease-out
                float smoothT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / (duration * 0.85f)));
                displayedValue = Mathf.Lerp(startVal, endVal, smoothT);
                numberText.text = $"{Mathf.RoundToInt((displayedValue / 10f) * 100f)}%";

                // 3. Efeito de cor no texto e no fundo do widget
                if (t < 0.65f)
                {
                    numberText.color = targetColor;
                    if (backgroundGraphic != null) backgroundGraphic.color = targetBgColor;
                }
                else
                {
                    float fadeT = (t - 0.65f) / 0.35f;
                    numberText.color = Color.Lerp(targetColor, defaultTextColor, fadeT);
                    if (backgroundGraphic != null) backgroundGraphic.color = Color.Lerp(targetBgColor, defaultBgColor, fadeT);
                }

                // 4. Efeito físico no widget (Pulso positivo ou Tremor negativo)
                if (containerRect != null)
                {
                    if (isPositive)
                    {
                        float bounce = Mathf.Sin(t * Mathf.PI) * 0.14f;
                        containerRect.localScale = Vector3.one * (1.0f + bounce);
                        containerRect.anchoredPosition = baseAnchoredPosition;
                    }
                    else
                    {
                        float bounce = Mathf.Sin(t * Mathf.PI) * 0.08f;
                        containerRect.localScale = Vector3.one * (1.0f + bounce);

                        float shake = 0f;
                        if (elapsed < 0.45f)
                        {
                            float shakeFactor = 1f - (elapsed / 0.45f);
                            shake = Mathf.Sin(elapsed * 45f) * 4f * shakeFactor;
                        }
                        containerRect.anchoredPosition = baseAnchoredPosition + new Vector2(shake, 0f);
                    }
                }

                yield return null;
            }

            // Finalização limpa
            displayedValue = endVal;
            numberText.text = $"{Mathf.RoundToInt((displayedValue / 10f) * 100f)}%";
            numberText.color = defaultTextColor;
            if (backgroundGraphic != null) backgroundGraphic.color = defaultBgColor;

            if (containerRect != null)
            {
                containerRect.localScale = Vector3.one;
                containerRect.anchoredPosition = baseAnchoredPosition;
            }

            if (currentPopup != null)
            {
                UnityEngine.Object.Destroy(currentPopup);
                currentPopup = null;
            }

            impactCoroutine = null;
        }

        private void SpawnDeltaPopup(int deltaPercent, bool isPositive)
        {
            if (containerRect == null && numberText == null) return;
            Transform parentTransform = containerRect != null ? containerRect.transform : numberText.transform.parent;

            GameObject popupObj = new GameObject("DeltaPopup", typeof(RectTransform), typeof(CanvasGroup));
            popupObj.transform.SetParent(parentTransform, false);
            currentPopup = popupObj;

            var rt = popupObj.GetComponent<RectTransform>();
            // Posiciona logo abaixo do widget do indicador
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(130, 36);
            rt.anchoredPosition = new Vector2(0, -6f);

            var cg = popupObj.GetComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // Fundo escuro semitransparente em alto contraste
            GameObject bgObj = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(popupObj.transform, false);
            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bgObj.GetComponent<Image>();
            bgImg.color = isPositive ? new Color(0.04f, 0.22f, 0.08f, 0.92f) : new Color(0.26f, 0.05f, 0.05f, 0.92f);
            bgImg.raycastTarget = false;

            // Texto com delta (+X% ou -X%)
            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(popupObj.transform, false);
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.font = numberText.font ?? TMPro.TMP_Settings.defaultFontAsset;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            string prefix = deltaPercent > 0 ? "+" : "";
            tmp.text = $"{prefix}{deltaPercent}%";
            tmp.color = isPositive ? POSITIVE_TEXT_COLOR : NEGATIVE_TEXT_COLOR;

            host.StartCoroutine(AnimatePopupRoutine(popupObj, rt, cg));
        }

        private IEnumerator AnimatePopupRoutine(GameObject popup, RectTransform rt, CanvasGroup cg)
        {
            float duration = 1.25f;
            float elapsed = 0f;
            Vector2 startPos = rt.anchoredPosition;

            while (elapsed < duration)
            {
                if (popup == null || rt == null || cg == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                // Flutua sutilmente para baixo (-20px)
                rt.anchoredPosition = startPos - new Vector2(0, Mathf.SmoothStep(0, 20f, t));

                // Pop scale
                if (t < 0.18f)
                {
                    float popScale = Mathf.Lerp(0.5f, 1.08f, t / 0.18f);
                    rt.localScale = Vector3.one * popScale;
                }
                else
                {
                    rt.localScale = Vector3.one;
                }

                // Fade out no terço final
                if (t > 0.65f)
                {
                    cg.alpha = Mathf.Lerp(1f, 0f, (t - 0.65f) / 0.35f);
                }

                yield return null;
            }

            if (popup != null)
            {
                UnityEngine.Object.Destroy(popup);
                if (currentPopup == popup) currentPopup = null;
            }
        }
    }
}
