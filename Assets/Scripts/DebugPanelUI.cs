using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugPanelUI : MonoBehaviour
{
    public static DebugPanelUI Instance { get; private set; }

    [Header("UI State")]
    [SerializeField] private bool startOpen = false;

    private CanvasGroup panelCanvasGroup;
    private RectTransform panelRect;
    private GameObject triggerButtonObj;
    private TMP_FontAsset fontAsset;

    // Tabs
    private GameObject tabContentDesign;
    private GameObject tabContentVisual;
    private GameObject tabContentActions;
    private Button tabBtnDesign;
    private Button tabBtnVisual;
    private Button tabBtnActions;
    private Image tabImgDesign;
    private Image tabImgVisual;
    private Image tabImgActions;
    private int currentTab = 0;

    // Sliders & Readout Texts
    private Slider sliderDecisionInterval;
    private TextMeshProUGUI textDecisionInterval;

    private Slider sliderCarbonMult;
    private TextMeshProUGUI textCarbonMult;

    private Slider sliderTreeCost;
    private TextMeshProUGUI textTreeCost;

    private Slider sliderIndicatorDecay;
    private TextMeshProUGUI textIndicatorDecay;

    private Slider sliderLumberjackInterval;
    private TextMeshProUGUI textLumberjackInterval;

    private Slider sliderLumberjackHits;
    private TextMeshProUGUI textLumberjackHits;

    private Slider sliderLumberjackCutLimit;
    private TextMeshProUGUI textLumberjackCutLimit;

    private Slider sliderTimeOfDay;
    private TextMeshProUGUI textTimeOfDay;

    private Toggle togglePauseCycle;

    private Slider sliderDayDuration;
    private TextMeshProUGUI textDayDuration;

    private Slider sliderFogDensity;
    private TextMeshProUGUI textFogDensity;

    private Slider sliderSunIntensity;
    private TextMeshProUGUI textSunIntensity;

    private Slider sliderRainInterval;
    private TextMeshProUGUI textRainInterval;

    private Slider sliderRainDuration;
    private TextMeshProUGUI textRainDuration;

    // Toast Feedback
    private TextMeshProUGUI toastText;
    private Coroutine toastCoroutine;

    private bool isOpen = false;
    private bool isInitialized = false;

    private readonly Color ACTIVE_TAB_COLOR = new Color(0.18f, 0.48f, 0.32f, 1f);
    private readonly Color INACTIVE_TAB_COLOR = new Color(0.12f, 0.16f, 0.18f, 0.95f);
    private readonly Color BG_PANEL_COLOR = new Color(0.08f, 0.11f, 0.12f, 0.96f);
    private readonly Color BORDER_COLOR = new Color(0.24f, 0.45f, 0.35f, 0.85f);
    private readonly Color ROW_BG_COLOR = new Color(0.13f, 0.18f, 0.19f, 0.75f);
    private readonly Color ACCENT_GREEN = new Color(0.25f, 0.85f, 0.45f, 1f);
    private readonly Color SLIDER_FILL_COLOR = new Color(0.28f, 0.75f, 0.45f, 1f);
    private readonly Color SLIDER_BG_COLOR = new Color(0.22f, 0.28f, 0.28f, 1f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        LoadFontAsset();
    }

    private void Start()
    {
        BuildUIIfNeeded();

        if (GameDebugManager.Instance != null)
        {
            GameDebugManager.Instance.OnConfigChanged += SyncUIWithConfig;
        }

        SyncUIWithConfig();

        if (startOpen)
        {
            OpenPanel();
        }
        else
        {
            ClosePanel();
        }
    }

    private void OnDestroy()
    {
        if (GameDebugManager.Instance != null)
        {
            GameDebugManager.Instance.OnConfigChanged -= SyncUIWithConfig;
        }
    }

    private void Update()
    {
        // Se o painel estiver aberto e o ciclo estiver rodando, atualiza o slider de horário suavemente
        if (isOpen && DayNightCycle.Instance != null && !DayNightCycle.Instance.PauseCycle)
        {
            if (sliderTimeOfDay != null && textTimeOfDay != null)
            {
                sliderTimeOfDay.SetValueWithoutNotify(DayNightCycle.Instance.CurrentHour);
                textTimeOfDay.text = FormatTimeOfDay(DayNightCycle.Instance.CurrentHour);
            }
        }
    }

    private void LoadFontAsset()
    {
        if (fontAsset != null) return;
        fontAsset = TMP_Settings.defaultFontAsset;
        if (fontAsset == null)
        {
            fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }
    }

    public void TogglePanel()
    {
        if (isOpen) ClosePanel();
        else OpenPanel();
    }

    public void OpenPanel()
    {
        isOpen = true;
        BuildUIIfNeeded();
        SyncUIWithConfig();

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.blocksRaycasts = true;
            panelCanvasGroup.interactable = true;
        }

        SwitchTab(currentTab);
    }

    public void ClosePanel()
    {
        isOpen = false;
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.blocksRaycasts = false;
            panelCanvasGroup.interactable = false;
        }
    }

    private void BuildUIIfNeeded()
    {
        if (isInitialized) return;

        LoadFontAsset();

        // Limpa filhos residuais caso existam
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        // 1. Localiza ou usa Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            var canvasGo = GameObject.Find("UI_Canvas");
            if (canvasGo != null)
            {
                transform.SetParent(canvasGo.transform, false);
                canvas = canvasGo.GetComponent<Canvas>();
            }
        }

        // 2. Cria botão disparador flutuante [ DEBUG ] no canto superior esquerdo
        CreateTriggerButton();

        // 3. Cria a janela flutuante principal
        CreateMainWindow();

        // 4. Cria abas e conteúdos
        CreateTabsAndContents();

        isInitialized = true;
    }

    private void CreateTriggerButton()
    {
        if (triggerButtonObj != null) return;

        Transform existing = transform.parent != null ? transform.parent.Find("DebugPanel_TriggerButton") : null;
        if (existing != null)
        {
            triggerButtonObj = existing.gameObject;
            var existingBtn = triggerButtonObj.GetComponent<Button>();
            if (existingBtn != null)
            {
                existingBtn.onClick.RemoveAllListeners();
                existingBtn.onClick.AddListener(TogglePanel);
            }
            return;
        }

        triggerButtonObj = new GameObject("DebugPanel_TriggerButton");
        triggerButtonObj.transform.SetParent(transform.parent != null ? transform.parent : transform, false);

        var rt = triggerButtonObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(25f, -25f);
        rt.sizeDelta = new Vector2(130f, 42f);

        var img = triggerButtonObj.AddComponent<Image>();
        img.color = new Color(0.12f, 0.20f, 0.16f, 0.90f);

        var outline = triggerButtonObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.8f, 0.45f, 0.8f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        var triggerBtn = triggerButtonObj.AddComponent<Button>();
        triggerBtn.targetGraphic = img;
        triggerBtn.onClick.AddListener(TogglePanel);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(triggerButtonObj.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) tmp.font = fontAsset;
        tmp.text = "<b>[ DEBUG ]</b>";
        tmp.fontSize = 17f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private void CreateMainWindow()
    {
        panelRect = GetComponent<RectTransform>();
        if (panelRect == null) panelRect = gameObject.AddComponent<RectTransform>();

        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(680f, 760f);

        var mainImg = GetComponent<Image>();
        if (mainImg == null) mainImg = gameObject.AddComponent<Image>();
        mainImg.color = BG_PANEL_COLOR;

        var outline = GetComponent<Outline>();
        if (outline == null) outline = gameObject.AddComponent<Outline>();
        outline.effectColor = BORDER_COLOR;
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        panelCanvasGroup = GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null) panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Header
        GameObject headerGo = new GameObject("Header");
        headerGo.transform.SetParent(transform, false);
        var headerRt = headerGo.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = Vector2.zero;
        headerRt.sizeDelta = new Vector2(0f, 85f);

        var headerImg = headerGo.AddComponent<Image>();
        headerImg.color = new Color(0.12f, 0.17f, 0.16f, 0.95f);

        // Header Title
        var titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(headerGo.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.4f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.anchoredPosition = new Vector2(20f, 0f);
        titleRt.sizeDelta = new Vector2(-100f, 0f);

        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) titleTmp.font = fontAsset;
        titleTmp.text = "<b>PAINEL DE DEBUG & BALANCEAMENTO</b>";
        titleTmp.fontSize = 20f;
        titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Subtitle / Shortcuts Info
        var subGo = new GameObject("SubTitleText");
        subGo.transform.SetParent(headerGo.transform, false);
        var subRt = subGo.AddComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 0f);
        subRt.anchorMax = new Vector2(1f, 0.45f);
        subRt.anchoredPosition = new Vector2(20f, 0f);
        subRt.sizeDelta = new Vector2(-100f, 0f);

        var subTmp = subGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) subTmp.font = fontAsset;
        subTmp.text = "Persistencia em Disco Ativa | Atalhos: [F1] ou [~] | Play Mode Salvo";
        subTmp.fontSize = 13f;
        subTmp.color = new Color(0.6f, 0.85f, 0.7f);
        subTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Botão Fechar [ X ]
        GameObject closeGo = new GameObject("CloseButton");
        closeGo.transform.SetParent(headerGo.transform, false);
        var closeRt = closeGo.AddComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-14f, -14f);
        closeRt.sizeDelta = new Vector2(46f, 46f);

        var closeImg = closeGo.AddComponent<Image>();
        closeImg.color = new Color(0.7f, 0.2f, 0.2f, 0.9f);
        var closeBtn = closeGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(ClosePanel);

        var closeTxtGo = new GameObject("CloseText");
        closeTxtGo.transform.SetParent(closeGo.transform, false);
        var closeTxtRt = closeTxtGo.AddComponent<RectTransform>();
        closeTxtRt.anchorMin = Vector2.zero;
        closeTxtRt.anchorMax = Vector2.one;
        closeTxtRt.sizeDelta = Vector2.zero;

        var closeTmp = closeTxtGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) closeTmp.font = fontAsset;
        closeTmp.text = "<b>X</b>";
        closeTmp.fontSize = 20f;
        closeTmp.color = Color.white;
        closeTmp.alignment = TextAlignmentOptions.Center;
    }

    private void CreateTabsAndContents()
    {
        // Barra de Abas (Tabs Bar)
        GameObject tabsBarGo = new GameObject("TabsBar");
        tabsBarGo.transform.SetParent(transform, false);
        var tabsBarRt = tabsBarGo.AddComponent<RectTransform>();
        tabsBarRt.anchorMin = new Vector2(0f, 1f);
        tabsBarRt.anchorMax = new Vector2(1f, 1f);
        tabsBarRt.pivot = new Vector2(0.5f, 1f);
        tabsBarRt.anchoredPosition = new Vector2(0f, -87f);
        tabsBarRt.sizeDelta = new Vector2(0f, 48f);

        var hlg = tabsBarGo.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(16, 16, 4, 4);
        hlg.spacing = 10;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        tabBtnDesign = CreateTabButton(tabsBarGo.transform, "DESIGN", () => SwitchTab(0), out tabImgDesign);
        tabBtnVisual = CreateTabButton(tabsBarGo.transform, "VISUAL & CLIMA", () => SwitchTab(1), out tabImgVisual);
        tabBtnActions = CreateTabButton(tabsBarGo.transform, "ACOES & CHEATS", () => SwitchTab(2), out tabImgActions);

        // Container de Conteúdo com Scroll
        GameObject contentContainerGo = new GameObject("TabContentArea");
        contentContainerGo.transform.SetParent(transform, false);
        var contentAreaRt = contentContainerGo.AddComponent<RectTransform>();
        contentAreaRt.anchorMin = new Vector2(0f, 0f);
        contentAreaRt.anchorMax = new Vector2(1f, 1f);
        contentAreaRt.offsetMin = new Vector2(16f, 85f); // espaço para footer
        contentAreaRt.offsetMax = new Vector2(-16f, -140f); // espaço para header + tabs

        // 1. Tab Game Design
        tabContentDesign = CreateScrollTab(contentContainerGo.transform, "Content_Design", out Transform designContent);
        BuildGameDesignTab(designContent);

        // 2. Tab Visual
        tabContentVisual = CreateScrollTab(contentContainerGo.transform, "Content_Visual", out Transform visualContent);
        BuildVisualTab(visualContent);

        // 3. Tab Actions
        tabContentActions = CreateScrollTab(contentContainerGo.transform, "Content_Actions", out Transform actionsContent);
        BuildActionsTab(actionsContent);

        // Footer Bar (Salvar, Resetar, Toast)
        CreateFooterBar();

        SwitchTab(0);
    }

    private Button CreateTabButton(Transform parent, string title, UnityEngine.Events.UnityAction onClick, out Image outImage)
    {
        GameObject btnGo = new GameObject("TabBtn_" + title);
        btnGo.transform.SetParent(parent, false);

        outImage = btnGo.AddComponent<Image>();
        outImage.color = INACTIVE_TAB_COLOR;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = outImage;
        btn.onClick.AddListener(onClick);

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(btnGo.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) tmp.font = fontAsset;
        tmp.text = $"<b>{title}</b>";
        tmp.fontSize = 15f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    private GameObject CreateScrollTab(Transform parent, string name, out Transform contentTransform)
    {
        GameObject scrollGo = new GameObject(name);
        scrollGo.transform.SetParent(parent, false);
        var sRt = scrollGo.AddComponent<RectTransform>();
        sRt.anchorMin = Vector2.zero;
        sRt.anchorMax = Vector2.one;
        sRt.offsetMin = Vector2.zero;
        sRt.offsetMax = Vector2.zero;

        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 25f;

        // Viewport
        GameObject viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var vpRt = viewportGo.AddComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;
        vpRt.pivot = new Vector2(0.5f, 0.5f);

        var mask = viewportGo.AddComponent<RectMask2D>();
        scrollRect.viewport = vpRt;

        // Content
        GameObject contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        var cRt = contentGo.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.anchoredPosition = Vector2.zero;
        cRt.sizeDelta = new Vector2(0f, 0f);
        cRt.offsetMin = Vector2.zero;
        cRt.offsetMax = Vector2.zero;

        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(6, 6, 8, 8);
        vlg.spacing = 8;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = cRt;

        contentTransform = contentGo.transform;
        return scrollGo;
    }

    private void SwitchTab(int index)
    {
        currentTab = index;

        if (tabContentDesign != null) tabContentDesign.SetActive(index == 0);
        if (tabContentVisual != null) tabContentVisual.SetActive(index == 1);
        if (tabContentActions != null) tabContentActions.SetActive(index == 2);

        if (tabImgDesign != null) tabImgDesign.color = (index == 0) ? ACTIVE_TAB_COLOR : INACTIVE_TAB_COLOR;
        if (tabImgVisual != null) tabImgVisual.color = (index == 1) ? ACTIVE_TAB_COLOR : INACTIVE_TAB_COLOR;
        if (tabImgActions != null) tabImgActions.color = (index == 2) ? ACTIVE_TAB_COLOR : INACTIVE_TAB_COLOR;

        GameObject activeObj = (index == 0) ? tabContentDesign : (index == 1) ? tabContentVisual : tabContentActions;
        if (activeObj != null)
        {
            var contentT = activeObj.transform.Find("Viewport/Content");
            if (contentT != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentT.GetComponent<RectTransform>());
            }
        }
    }

    private void BuildGameDesignTab(Transform parent)
    {
        CreateSectionHeader(parent, "EQUILÍBRIO E ECONOMIA ECOLÓGICA");

        // 1. Geração de Carbono
        CreateSliderRow(parent, "Geração de Carbono:", 0.1f, 5.0f, 1.0f, false,
            val => {
                GameDebugManager.Instance?.SetCarbonMultiplier(val);
                if (textCarbonMult != null) textCarbonMult.text = $"{val:F2}x";
                ShowSaveToast("Geração de carbono atualizada: " + val.ToString("F2") + "x");
            },
            out textCarbonMult, out sliderCarbonMult
        );

        // 2. Custo de Plantio de Árvores
        CreateSliderRow(parent, "Custo Plantio de Muda:", 0f, 50f, 10f, true,
            val => {
                GameDebugManager.Instance?.SetTreePlantCost(val);
                if (textTreeCost != null) textTreeCost.text = $"{Mathf.RoundToInt(val)} ppm";
                ShowSaveToast("Custo de plantio atualizado: " + Mathf.RoundToInt(val) + " ppm");
            },
            out textTreeCost, out sliderTreeCost
        );

        // 3. Velocidade Tomada de Decisão (Dilemas SOS)
        CreateSliderRow(parent, "Intervalo entre Dilemas:", 5f, 60f, 22f, false,
            val => {
                GameDebugManager.Instance?.SetDecisionInterval(val);
                if (textDecisionInterval != null) textDecisionInterval.text = $"{val:F1}s";
                ShowSaveToast("Intervalo de dilemas atualizado: " + val.ToString("F1") + "s");
            },
            out textDecisionInterval, out sliderDecisionInterval
        );

        // 4. Decaimento dos Indicadores
        CreateSliderRow(parent, "Dificuldade / Decaimento:", 0f, 3f, 1.0f, false,
            val => {
                GameDebugManager.Instance?.SetIndicatorDecayMultiplier(val);
                if (textIndicatorDecay != null) textIndicatorDecay.text = $"{val:F2}x";
                ShowSaveToast("Taxa de decaimento atualizada: " + val.ToString("F2") + "x");
            },
            out textIndicatorDecay, out sliderIndicatorDecay
        );

        CreateSectionHeader(parent, "INVASÃO DE LENHADORES CLANDESTINOS");

        // 5. Intervalo Lenhadores
        CreateSliderRow(parent, "Intervalo Invasão Lenhadores:", 5f, 60f, 25f, false,
            val => {
                GameDebugManager.Instance?.SetLumberjackSpawnInterval(val);
                if (textLumberjackInterval != null) textLumberjackInterval.text = $"{val:F1}s";
                ShowSaveToast("Intervalo de lenhadores atualizado: " + val.ToString("F1") + "s");
            },
            out textLumberjackInterval, out sliderLumberjackInterval
        );

        // 6. Golpes para Derrotar Lenhador
        CreateSliderRow(parent, "Golpes p/ Expulsar Lenhador:", 1f, 8f, 3f, true,
            val => {
                int hits = Mathf.RoundToInt(val);
                GameDebugManager.Instance?.SetLumberjackHitsToDefeat(hits);
                if (textLumberjackHits != null) textLumberjackHits.text = $"{hits} golpes";
                ShowSaveToast("Golpes para derrotar lenhador: " + hits);
            },
            out textLumberjackHits, out sliderLumberjackHits
        );

        // 7. Tempo Limite de Corte do Lenhador
        CreateSliderRow(parent, "Tempo Limite Corte da Árvore:", 5f, 30f, 15f, false,
            val => {
                GameDebugManager.Instance?.SetLumberjackCutTimeLimit(val);
                if (textLumberjackCutLimit != null) textLumberjackCutLimit.text = $"{val:F1}s";
                ShowSaveToast("Tempo de corte atualizado: " + val.ToString("F1") + "s");
            },
            out textLumberjackCutLimit, out sliderLumberjackCutLimit
        );
    }

    private void BuildVisualTab(Transform parent)
    {
        CreateSectionHeader(parent, "ILUMINAÇÃO E CICLO DIA / NOITE");

        // 1. Horário do Dia
        CreateSliderRow(parent, "Horário do Dia (0h-24h):", 0f, 24f, 8.5f, false,
            val => {
                GameDebugManager.Instance?.SetTimeOfDay(val);
                if (textTimeOfDay != null) textTimeOfDay.text = FormatTimeOfDay(val);
                ShowSaveToast("Horário alterado: " + FormatTimeOfDay(val));
            },
            out textTimeOfDay, out sliderTimeOfDay
        );

        // 2. Congelar Ciclo
        CreateToggleRow(parent, "Congelar Ciclo no Horário Selecionado", false,
            isPaused => {
                GameDebugManager.Instance?.SetPauseDayCycle(isPaused);
                ShowSaveToast(isPaused ? "Ciclo pausado." : "Ciclo despausado.");
            },
            out togglePauseCycle
        );

        // 3. Velocidade / Duração do Dia Completo
        CreateSliderRow(parent, "Duração do Dia Completo (24h):", 15f, 300f, 120f, false,
            val => {
                GameDebugManager.Instance?.SetDayCycleDuration(val);
                if (textDayDuration != null) textDayDuration.text = $"{Mathf.RoundToInt(val)}s";
                ShowSaveToast("Duração do dia atualizada: " + Mathf.RoundToInt(val) + "s");
            },
            out textDayDuration, out sliderDayDuration
        );

        // 4. Intensidade Solar / Lunar
        CreateSliderRow(parent, "Intensidade Luz Solar/Lunar:", 0.2f, 3.0f, 1.0f, false,
            val => {
                GameDebugManager.Instance?.SetSunIntensityMultiplier(val);
                if (textSunIntensity != null) textSunIntensity.text = $"{val:F2}x";
                ShowSaveToast("Intensidade solar: " + val.ToString("F2") + "x");
            },
            out textSunIntensity, out sliderSunIntensity
        );

        // 5. Opacidade / Densidade da Névoa
        CreateSliderRow(parent, "Densidade da Névoa Florestal:", 0f, 3.5f, 1.0f, false,
            val => {
                GameDebugManager.Instance?.SetFogDensityMultiplier(val);
                if (textFogDensity != null) textFogDensity.text = $"{val:F2}x";
                ShowSaveToast("Densidade de névoa: " + val.ToString("F2") + "x");
            },
            out textFogDensity, out sliderFogDensity
        );

        CreateSectionHeader(parent, "SISTEMA METEOROLÓGICO (CHUVA TROPICAL)");

        // 6. Intervalo de Chuva
        CreateSliderRow(parent, "Intervalo entre Tempestades:", 15f, 200f, 80f, false,
            val => {
                GameDebugManager.Instance?.SetRainInterval(val);
                if (textRainInterval != null) textRainInterval.text = $"{val:F0}s";
                ShowSaveToast("Intervalo de chuva: " + val.ToString("F0") + "s");
            },
            out textRainInterval, out sliderRainInterval
        );

        // 7. Duração da Chuva
        CreateSliderRow(parent, "Duração da Tempestade:", 5f, 60f, 28f, false,
            val => {
                GameDebugManager.Instance?.SetRainDuration(val);
                if (textRainDuration != null) textRainDuration.text = $"{val:F0}s";
                ShowSaveToast("Duração da chuva: " + val.ToString("F0") + "s");
            },
            out textRainDuration, out sliderRainDuration
        );
    }

    private void BuildActionsTab(Transform parent)
    {
        CreateSectionHeader(parent, "CONTROLE DE CLIMA E EVENTOS");

        CreateActionButton(parent, "INICIAR CHUVA TROPICAL", new Color(0.2f, 0.45f, 0.7f), () => {
            GameDebugManager.Instance?.TriggerRainNow();
            ShowSaveToast("Tempestade tropical iniciada!");
        });

        CreateActionButton(parent, "PARAR CHUVA", new Color(0.35f, 0.5f, 0.55f), () => {
            GameDebugManager.Instance?.StopRainNow();
            ShowSaveToast("Chuva interrompida.");
        });

        CreateSectionHeader(parent, "EVENTOS E DESAFIOS EM TEMPO REAL");

        CreateActionButton(parent, "SPAWNAR LENHADOR", new Color(0.8f, 0.35f, 0.15f), () => {
            GameDebugManager.Instance?.SpawnLumberjackNow();
            ShowSaveToast("Lenhador clandestino invadiu a floresta!");
        });

        CreateActionButton(parent, "DISPARAR DILEMA SOS", new Color(0.65f, 0.45f, 0.15f), () => {
            GameDebugManager.Instance?.TriggerQuestionNow();
            ShowSaveToast("Dilema ecologico aberto na tela!");
        });

        CreateSectionHeader(parent, "RECURSOS E RESTAURACAO RAPIDA");

        CreateActionButton(parent, "+500 PPM CO2", new Color(0.18f, 0.55f, 0.28f), () => {
            GameDebugManager.Instance?.AddQuickCarbon(500f);
            ShowSaveToast("+500 ppm de CO2 creditados!");
        });

        CreateActionButton(parent, "RESTAURAR INDICADORES (100%)", new Color(0.15f, 0.52f, 0.65f), () => {
            GameDebugManager.Instance?.RestoreIndicatorsNow();
            ShowSaveToast("Todos os indicadores recuperados a 100%!");
        });
    }

    private void CreateFooterBar()
    {
        GameObject footerGo = new GameObject("FooterBar");
        footerGo.transform.SetParent(transform, false);
        var footerRt = footerGo.AddComponent<RectTransform>();
        footerRt.anchorMin = new Vector2(0f, 0f);
        footerRt.anchorMax = new Vector2(1f, 0f);
        footerRt.pivot = new Vector2(0.5f, 0f);
        footerRt.anchoredPosition = Vector2.zero;
        footerRt.sizeDelta = new Vector2(0f, 78f);

        var footerImg = footerGo.AddComponent<Image>();
        footerImg.color = new Color(0.11f, 0.15f, 0.15f, 0.98f);

        // Toast de Status
        GameObject toastGo = new GameObject("ToastText");
        toastGo.transform.SetParent(footerGo.transform, false);
        var toastRt = toastGo.AddComponent<RectTransform>();
        toastRt.anchorMin = new Vector2(0f, 0.55f);
        toastRt.anchorMax = new Vector2(1f, 1f);
        toastRt.offsetMin = new Vector2(16f, 0f);
        toastRt.offsetMax = new Vector2(-16f, 0f);

        toastText = toastGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) toastText.font = fontAsset;
        toastText.text = "[OK] Salvo automaticamente no disco.";
        toastText.fontSize = 13.5f;
        toastText.color = ACCENT_GREEN;
        toastText.alignment = TextAlignmentOptions.Center;

        // Container de Botões
        GameObject btnsGo = new GameObject("ButtonsContainer");
        btnsGo.transform.SetParent(footerGo.transform, false);
        var btnsRt = btnsGo.AddComponent<RectTransform>();
        btnsRt.anchorMin = new Vector2(0f, 0f);
        btnsRt.anchorMax = new Vector2(1f, 0.55f);
        btnsRt.offsetMin = new Vector2(16f, 6f);
        btnsRt.offsetMax = new Vector2(-16f, 0f);

        var hlg = btnsGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 14;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        // Botão Salvar Agora
        GameObject saveGo = new GameObject("Btn_Save");
        saveGo.transform.SetParent(btnsGo.transform, false);
        var saveImg = saveGo.AddComponent<Image>();
        saveImg.color = new Color(0.18f, 0.52f, 0.32f);
        var saveBtn = saveGo.AddComponent<Button>();
        saveBtn.targetGraphic = saveImg;
        saveBtn.onClick.AddListener(() => {
            GameDebugManager.Instance?.ForceSave();
            ShowSaveToast("Configuracoes gravadas no disco com sucesso!");
        });

        var saveTxtGo = new GameObject("Text");
        saveTxtGo.transform.SetParent(saveGo.transform, false);
        var saveTxtRt = saveTxtGo.AddComponent<RectTransform>();
        saveTxtRt.anchorMin = Vector2.zero;
        saveTxtRt.anchorMax = Vector2.one;
        saveTxtRt.sizeDelta = Vector2.zero;
        var sTmp = saveTxtGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) sTmp.font = fontAsset;
        sTmp.text = "<b>SALVAR CONFIGURACOES</b>";
        sTmp.fontSize = 14.5f;
        sTmp.color = Color.white;
        sTmp.alignment = TextAlignmentOptions.Center;

        // Botão Resetar Padrões
        GameObject resetGo = new GameObject("Btn_Reset");
        resetGo.transform.SetParent(btnsGo.transform, false);
        var resetImg = resetGo.AddComponent<Image>();
        resetImg.color = new Color(0.45f, 0.22f, 0.22f);
        var resetBtn = resetGo.AddComponent<Button>();
        resetBtn.targetGraphic = resetImg;
        resetBtn.onClick.AddListener(() => {
            GameDebugManager.Instance?.ResetToDefaults();
            ShowSaveToast("Configuracoes restauradas para os padroes originais!");
        });

        var resetTxtGo = new GameObject("Text");
        resetTxtGo.transform.SetParent(resetGo.transform, false);
        var resetTxtRt = resetTxtGo.AddComponent<RectTransform>();
        resetTxtRt.anchorMin = Vector2.zero;
        resetTxtRt.anchorMax = Vector2.one;
        resetTxtRt.sizeDelta = Vector2.zero;
        var rTmp = resetTxtGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) rTmp.font = fontAsset;
        rTmp.text = "<b>RESETAR PADROES</b>";
        rTmp.fontSize = 14.5f;
        rTmp.color = Color.white;
        rTmp.alignment = TextAlignmentOptions.Center;
    }

    private void CreateSectionHeader(Transform parent, string title)
    {
        GameObject headerGo = new GameObject("Section_" + title);
        headerGo.transform.SetParent(parent, false);

        var rt = headerGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 32f);

        var le = headerGo.AddComponent<LayoutElement>();
        le.preferredHeight = 32f;
        le.minHeight = 32f;
        le.flexibleWidth = 1f;

        var img = headerGo.AddComponent<Image>();
        img.color = new Color(0.14f, 0.22f, 0.19f, 0.95f);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(headerGo.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(12f, 0f);
        textRt.offsetMax = new Vector2(-12f, 0f);

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) tmp.font = fontAsset;
        tmp.text = $"<b>{title}</b>";
        tmp.fontSize = 13.5f;
        tmp.color = ACCENT_GREEN;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private void CreateSliderRow(Transform parent, string label, float min, float max, float currentVal, bool wholeNumbers,
        System.Action<float> onValueChange, out TextMeshProUGUI outValText, out Slider outSlider)
    {
        GameObject rowGo = new GameObject("Row_" + label);
        rowGo.transform.SetParent(parent, false);

        var rt = rowGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 56f);

        var le = rowGo.AddComponent<LayoutElement>();
        le.preferredHeight = 56f;
        le.minHeight = 56f;
        le.flexibleWidth = 1f;

        var bg = rowGo.AddComponent<Image>();
        bg.color = ROW_BG_COLOR;

        // Label à esquerda
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(rowGo.transform, false);
        var lRt = labelGo.AddComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0f, 0.5f);
        lRt.anchorMax = new Vector2(0.65f, 1f);
        lRt.offsetMin = new Vector2(14f, 0f);
        lRt.offsetMax = new Vector2(0f, -2f);

        var lTmp = labelGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) lTmp.font = fontAsset;
        lTmp.text = label;
        lTmp.fontSize = 14f;
        lTmp.color = new Color(0.9f, 0.94f, 0.95f);
        lTmp.alignment = TextAlignmentOptions.MidlineLeft;
        lTmp.textWrappingMode = TextWrappingModes.NoWrap;

        // Valor à direita
        var valGo = new GameObject("ValueText");
        valGo.transform.SetParent(rowGo.transform, false);
        var vRt = valGo.AddComponent<RectTransform>();
        vRt.anchorMin = new Vector2(0.65f, 0.5f);
        vRt.anchorMax = new Vector2(1f, 1f);
        vRt.offsetMin = new Vector2(0f, 0f);
        vRt.offsetMax = new Vector2(-14f, -2f);

        outValText = valGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) outValText.font = fontAsset;
        outValText.text = wholeNumbers ? Mathf.RoundToInt(currentVal).ToString() : currentVal.ToString("F2");
        outValText.fontSize = 14f;
        outValText.color = ACCENT_GREEN;
        outValText.alignment = TextAlignmentOptions.MidlineRight;
        outValText.textWrappingMode = TextWrappingModes.NoWrap;

        // Slider embaixo
        GameObject sliderGo = new GameObject("Slider");
        sliderGo.transform.SetParent(rowGo.transform, false);
        var sRt = sliderGo.AddComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0f);
        sRt.anchorMax = new Vector2(1f, 0.5f);
        sRt.offsetMin = new Vector2(14f, 6f);
        sRt.offsetMax = new Vector2(-14f, -4f);

        outSlider = sliderGo.AddComponent<Slider>();
        outSlider.minValue = min;
        outSlider.maxValue = max;
        outSlider.wholeNumbers = wholeNumbers;

        // Background do slider
        GameObject bgBarGo = new GameObject("Background");
        bgBarGo.transform.SetParent(sliderGo.transform, false);
        var bgBarRt = bgBarGo.AddComponent<RectTransform>();
        bgBarRt.anchorMin = new Vector2(0f, 0.3f);
        bgBarRt.anchorMax = new Vector2(1f, 0.7f);
        bgBarRt.offsetMin = Vector2.zero;
        bgBarRt.offsetMax = Vector2.zero;
        var bgBarImg = bgBarGo.AddComponent<Image>();
        bgBarImg.color = SLIDER_BG_COLOR;

        // Fill Area
        GameObject fillAreaGo = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(sliderGo.transform, false);
        var fillAreaRt = fillAreaGo.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0.3f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.7f);
        fillAreaRt.offsetMin = new Vector2(5f, 0f);
        fillAreaRt.offsetMax = new Vector2(-5f, 0f);

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.color = SLIDER_FILL_COLOR;

        outSlider.fillRect = fillRt;

        // Handle Area
        GameObject handleAreaGo = new GameObject("Handle Slide Area");
        handleAreaGo.transform.SetParent(sliderGo.transform, false);
        var handleAreaRt = handleAreaGo.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(8f, 0f);
        handleAreaRt.offsetMax = new Vector2(-8f, 0f);

        GameObject handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(handleAreaGo.transform, false);
        var handleRt = handleGo.AddComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(16f, 16f);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.color = Color.white;

        outSlider.handleRect = handleRt;
        outSlider.targetGraphic = handleImg;

        outSlider.value = currentVal;
        outSlider.onValueChanged.AddListener(val => onValueChange?.Invoke(val));
    }

    private void CreateToggleRow(Transform parent, string label, bool currentVal, System.Action<bool> onToggle, out Toggle outToggle)
    {
        GameObject rowGo = new GameObject("RowToggle_" + label);
        rowGo.transform.SetParent(parent, false);

        var rt = rowGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 46f);

        var le = rowGo.AddComponent<LayoutElement>();
        le.preferredHeight = 46f;
        le.minHeight = 46f;
        le.flexibleWidth = 1f;

        var bg = rowGo.AddComponent<Image>();
        bg.color = ROW_BG_COLOR;

        // Label à esquerda
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(rowGo.transform, false);
        var lRt = labelGo.AddComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0f, 0f);
        lRt.anchorMax = new Vector2(0.85f, 1f);
        lRt.offsetMin = new Vector2(14f, 0f);
        lRt.offsetMax = new Vector2(0f, 0f);

        var lTmp = labelGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) lTmp.font = fontAsset;
        lTmp.text = label;
        lTmp.fontSize = 14f;
        lTmp.color = new Color(0.9f, 0.94f, 0.95f);
        lTmp.alignment = TextAlignmentOptions.MidlineLeft;
        lTmp.textWrappingMode = TextWrappingModes.NoWrap;

        // Checkbox à direita
        GameObject checkGo = new GameObject("Toggle");
        checkGo.transform.SetParent(rowGo.transform, false);
        var cRt = checkGo.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 0.5f);
        cRt.anchorMax = new Vector2(1f, 0.5f);
        cRt.pivot = new Vector2(1f, 0.5f);
        cRt.anchoredPosition = new Vector2(-14f, 0f);
        cRt.sizeDelta = new Vector2(26f, 26f);

        var checkBg = checkGo.AddComponent<Image>();
        checkBg.color = SLIDER_BG_COLOR;

        outToggle = checkGo.AddComponent<Toggle>();
        outToggle.targetGraphic = checkBg;

        GameObject checkmarkGo = new GameObject("Checkmark");
        checkmarkGo.transform.SetParent(checkGo.transform, false);
        var cmRt = checkmarkGo.AddComponent<RectTransform>();
        cmRt.anchorMin = new Vector2(0.2f, 0.2f);
        cmRt.anchorMax = new Vector2(0.8f, 0.8f);
        cmRt.sizeDelta = Vector2.zero;

        var cmImg = checkmarkGo.AddComponent<Image>();
        cmImg.color = ACCENT_GREEN;
        outToggle.graphic = cmImg;

        outToggle.isOn = currentVal;
        outToggle.onValueChanged.AddListener(val => onToggle?.Invoke(val));
    }

    private void CreateActionButton(Transform parent, string text, Color btnColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnGo = new GameObject("ActionBtn_" + text);
        btnGo.transform.SetParent(parent, false);

        var rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 46f);

        var le = btnGo.AddComponent<LayoutElement>();
        le.preferredHeight = 46f;
        le.minHeight = 46f;
        le.flexibleWidth = 1f;

        var img = btnGo.AddComponent<Image>();
        img.color = btnColor;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(btnGo.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) tmp.font = fontAsset;
        tmp.text = $"<b>{text}</b>";
        tmp.fontSize = 14.5f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private void ShowSaveToast(string message)
    {
        if (toastText == null) return;
        toastText.text = "[OK] " + message;

        if (toastCoroutine != null) StopCoroutine(toastCoroutine);
        toastCoroutine = StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        if (toastText != null) toastText.color = ACCENT_GREEN;
        yield return new WaitForSecondsRealtime(2.5f);
        if (toastText != null)
        {
            toastText.text = "[OK] Salvo automaticamente no disco.";
            toastText.color = new Color(0.6f, 0.85f, 0.7f);
        }
    }

    public void SyncUIWithConfig()
    {
        var config = GameDebugManager.Instance != null ? GameDebugManager.Instance.Config : null;
        if (config == null) return;

        // Design
        if (sliderCarbonMult != null)
        {
            sliderCarbonMult.SetValueWithoutNotify(config.carbonGenerationMultiplier);
            if (textCarbonMult != null) textCarbonMult.text = $"{config.carbonGenerationMultiplier:F2}x";
        }

        if (sliderTreeCost != null)
        {
            sliderTreeCost.SetValueWithoutNotify(config.treePlantCost);
            if (textTreeCost != null) textTreeCost.text = $"{Mathf.RoundToInt(config.treePlantCost)} ppm";
        }

        if (sliderDecisionInterval != null)
        {
            sliderDecisionInterval.SetValueWithoutNotify(config.decisionInterval);
            if (textDecisionInterval != null) textDecisionInterval.text = $"{config.decisionInterval:F1}s";
        }

        if (sliderIndicatorDecay != null)
        {
            sliderIndicatorDecay.SetValueWithoutNotify(config.indicatorDecayMultiplier);
            if (textIndicatorDecay != null) textIndicatorDecay.text = $"{config.indicatorDecayMultiplier:F2}x";
        }

        if (sliderLumberjackInterval != null)
        {
            sliderLumberjackInterval.SetValueWithoutNotify(config.lumberjackSpawnInterval);
            if (textLumberjackInterval != null) textLumberjackInterval.text = $"{config.lumberjackSpawnInterval:F1}s";
        }

        if (sliderLumberjackHits != null)
        {
            sliderLumberjackHits.SetValueWithoutNotify(config.lumberjackHitsToDefeat);
            if (textLumberjackHits != null) textLumberjackHits.text = $"{config.lumberjackHitsToDefeat} golpes";
        }

        if (sliderLumberjackCutLimit != null)
        {
            sliderLumberjackCutLimit.SetValueWithoutNotify(config.lumberjackCutTimeLimit);
            if (textLumberjackCutLimit != null) textLumberjackCutLimit.text = $"{config.lumberjackCutTimeLimit:F1}s";
        }

        // Visual
        if (sliderTimeOfDay != null)
        {
            sliderTimeOfDay.SetValueWithoutNotify(config.timeOfDay);
            if (textTimeOfDay != null) textTimeOfDay.text = FormatTimeOfDay(config.timeOfDay);
        }

        if (togglePauseCycle != null)
        {
            togglePauseCycle.SetIsOnWithoutNotify(config.pauseDayCycle);
        }

        if (sliderDayDuration != null)
        {
            sliderDayDuration.SetValueWithoutNotify(config.dayCycleDuration);
            if (textDayDuration != null) textDayDuration.text = $"{Mathf.RoundToInt(config.dayCycleDuration)}s";
        }

        if (sliderSunIntensity != null)
        {
            sliderSunIntensity.SetValueWithoutNotify(config.sunIntensityMultiplier);
            if (textSunIntensity != null) textSunIntensity.text = $"{config.sunIntensityMultiplier:F2}x";
        }

        if (sliderFogDensity != null)
        {
            sliderFogDensity.SetValueWithoutNotify(config.fogDensityMultiplier);
            if (textFogDensity != null) textFogDensity.text = $"{config.fogDensityMultiplier:F2}x";
        }

        if (sliderRainInterval != null)
        {
            sliderRainInterval.SetValueWithoutNotify(config.rainInterval);
            if (textRainInterval != null) textRainInterval.text = $"{config.rainInterval:F0}s";
        }

        if (sliderRainDuration != null)
        {
            sliderRainDuration.SetValueWithoutNotify(config.rainDuration);
            if (textRainDuration != null) textRainDuration.text = $"{config.rainDuration:F0}s";
        }
    }

    private string FormatTimeOfDay(float hour24)
    {
        hour24 = Mathf.Repeat(hour24, 24f);
        int hours = Mathf.FloorToInt(hour24);
        int minutes = Mathf.FloorToInt((hour24 - hours) * 60f);

        string phase = "Dia";
        if (hour24 >= 5f && hour24 < 8f) phase = "Alvorada";
        else if (hour24 >= 8f && hour24 < 17f) phase = "Dia";
        else if (hour24 >= 17f && hour24 < 19.5f) phase = "Entardecer";
        else phase = "Noite";

        return $"{hours:00}:{minutes:00} ({phase})";
    }
}
