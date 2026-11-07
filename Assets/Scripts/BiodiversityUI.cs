using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BiodiversityUI : MonoBehaviour
{
    public static BiodiversityUI Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private CanvasGroup catalogCanvas;
    [SerializeField] private Transform cardsContainer;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_FontAsset fontAsset;

    private bool isOpen = false;
    private int lastToggleFrame = -1;
    private float lastToggleTime = -1f;
    private float refreshTimer = 0f;

    private class CardItemEntry
    {
        public SpeciesData species;
        public GameObject rootGo;
        public Image bgImage;
        public Button actionButton;
        public Image actionButtonImage;
        public TextMeshProUGUI actionButtonText;
    }

    private List<CardItemEntry> cardEntries = new List<CardItemEntry>();

    private void Awake()
    {
        Instance = this;

        if (catalogCanvas == null)
        {
            catalogCanvas = GetComponent<CanvasGroup>();
        }

        if (cardsContainer == null)
        {
            var contentT = transform.Find("ScrollView/Viewport/Content");
            if (contentT != null) cardsContainer = contentT;
        }
    }

    private void Start()
    {
        if (fontAsset == null)
        {
            fontAsset = TMP_Settings.defaultFontAsset;
            if (fontAsset == null)
            {
                fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }
        }

        if (toggleButton == null)
        {
            var btnGo = GameObject.Find("BiodiversityButton");
            if (btnGo == null) btnGo = GameObject.Find("UI_Canvas/Game/BottomActionsBar/BiodiversityButton");
            if (btnGo != null)
            {
                toggleButton = btnGo.GetComponent<Button>();
            }
        }

        if (toggleButton != null)
        {
            var lbl = toggleButton.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null)
            {
                lbl.raycastTarget = false;
                if (fontAsset != null) lbl.font = fontAsset;
            }

            toggleButton.onClick.RemoveListener(ToggleCatalog);
            toggleButton.onClick.AddListener(ToggleCatalog);
        }

        if (closeButton == null)
        {
            var closeGo = transform.Find("CloseButton");
            if (closeGo != null) closeButton = closeGo.GetComponent<Button>();
        }

        if (closeButton != null)
        {
            var lbl = closeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null)
            {
                lbl.raycastTarget = false;
                if (fontAsset != null) lbl.font = fontAsset;
            }

            closeButton.onClick.RemoveListener(CloseCatalog);
            closeButton.onClick.AddListener(CloseCatalog);
        }

        if (catalogCanvas != null)
        {
            catalogCanvas.alpha = 0f;
            catalogCanvas.blocksRaycasts = false;
            catalogCanvas.interactable = false;
        }

        var bioMgr = GetBiodiversityManager();
        if (bioMgr != null)
        {
            bioMgr.OnSpeciesUnlocked += OnSpeciesUnlocked;
        }

        BuildOrRefreshCards();
    }

    private void Update()
    {
        if (isOpen)
        {
            refreshTimer += Time.unscaledDeltaTime;
            if (refreshTimer >= 0.25f)
            {
                refreshTimer = 0f;
                UpdateExistingCardStates();
            }
        }
    }

    private void OnDestroy()
    {
        var bioMgr = GetBiodiversityManager();
        if (bioMgr != null)
        {
            bioMgr.OnSpeciesUnlocked -= OnSpeciesUnlocked;
        }
    }

    private BiodiversityManager GetBiodiversityManager()
    {
        return BiodiversityManager.Instance != null ? BiodiversityManager.Instance : Object.FindAnyObjectByType<BiodiversityManager>();
    }

    public void ToggleCatalog()
    {
        // Proteção contra múltiplos disparos no mesmo frame (ex: persistent + runtime listener)
        if (Time.frameCount == lastToggleFrame) return;
        if (lastToggleTime >= 0f && Time.unscaledTime - lastToggleTime < 0.15f) return;

        lastToggleFrame = Time.frameCount;
        lastToggleTime = Time.unscaledTime;

        if (isOpen)
        {
            CloseCatalog();
        }
        else
        {
            OpenCatalog();
        }
    }

    public void OpenCatalog()
    {
        isOpen = true;
        transform.SetAsLastSibling();
        BuildOrRefreshCards();

        if (catalogCanvas != null)
        {
            catalogCanvas.alpha = 1f;
            catalogCanvas.blocksRaycasts = true;
            catalogCanvas.interactable = true;
        }
    }

    public void CloseCatalog()
    {
        isOpen = false;
        if (catalogCanvas != null)
        {
            catalogCanvas.alpha = 0f;
            catalogCanvas.blocksRaycasts = false;
            catalogCanvas.interactable = false;
        }
    }

    private void OnSpeciesUnlocked(SpeciesData species)
    {
        if (isOpen)
        {
            UpdateExistingCardStates();
        }
    }

    public void BuildOrRefreshCards()
    {
        var bioMgr = GetBiodiversityManager();
        if (cardsContainer == null || bioMgr == null) return;

        var catalog = bioMgr.speciesCatalog;
        if (catalog == null || catalog.Count == 0) return;

        // Se os cards já foram criados e a quantidade confere, apenas atualiza estados
        if (cardEntries.Count == catalog.Count && cardsContainer.childCount == catalog.Count)
        {
            UpdateExistingCardStates();
            return;
        }

        // Limpa crianças existentes detachando o transform para childCount ser zerado imediatamente
        for (int i = cardsContainer.childCount - 1; i >= 0; i--)
        {
            var child = cardsContainer.GetChild(i).gameObject;
            child.transform.SetParent(null, false);
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
        cardEntries.Clear();

        float currentCo2 = CarbonManager1.Instance != null ? CarbonManager1.Instance.CurrentCarbon : 0f;

        foreach (var species in catalog)
        {
            var entry = CreateCardEntry(species, currentCo2);
            cardEntries.Add(entry);
        }

        var contentRt = cardsContainer.GetComponent<RectTransform>();
        if (contentRt != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        }
    }

    private CardItemEntry CreateCardEntry(SpeciesData species, float currentCo2)
    {
        var entry = new CardItemEntry { species = species };

        GameObject cardGo = new GameObject("Card_" + species.id);
        cardGo.transform.SetParent(cardsContainer, false);
        entry.rootGo = cardGo;

        var rt = cardGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500, 135);

        var le = cardGo.AddComponent<LayoutElement>();
        le.preferredWidth = 500;
        le.preferredHeight = 135;
        le.flexibleWidth = 1;
        le.flexibleHeight = 0;

        entry.bgImage = cardGo.AddComponent<Image>();
        entry.bgImage.color = species.isUnlocked ? new Color(0.12f, 0.28f, 0.16f, 0.95f) : new Color(0.14f, 0.17f, 0.22f, 0.95f);

        var hlg = cardGo.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(16, 16, 10, 10);
        hlg.spacing = 14;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        // Coluna de Texto
        GameObject infoGo = new GameObject("Info");
        infoGo.transform.SetParent(cardGo.transform, false);
        var infoLayout = infoGo.AddComponent<LayoutElement>();
        infoLayout.flexibleWidth = 1;
        infoLayout.preferredHeight = 115;

        var infoVlg = infoGo.AddComponent<VerticalLayoutGroup>();
        infoVlg.childAlignment = TextAnchor.MiddleLeft;
        infoVlg.spacing = 3;
        infoVlg.childControlWidth = true;
        infoVlg.childControlHeight = false;

        CreateText(infoGo.transform, $"<b>{species.commonName}</b> <size=80%><i>({species.scientificName})</i></size>", 17.5f, Color.white);
        CreateText(infoGo.transform, $"<b>Efeito:</b> {species.gameplayEffect}", 13.5f, new Color(0.42f, 1f, 0.65f));
        CreateText(infoGo.transform, species.ecologicalRole, 11.5f, new Color(0.85f, 0.9f, 0.95f));

        // Botão de Ação
        GameObject btnGo = new GameObject("ActionBtn");
        btnGo.transform.SetParent(cardGo.transform, false);
        var btnLayout = btnGo.AddComponent<LayoutElement>();
        btnLayout.minWidth = 140;
        btnLayout.preferredWidth = 140;
        btnLayout.preferredHeight = 58;

        entry.actionButtonImage = btnGo.AddComponent<Image>();
        entry.actionButton = btnGo.AddComponent<Button>();
        entry.actionButton.targetGraphic = entry.actionButtonImage;

        entry.actionButtonText = CreateButtonText(btnGo.transform, "", Color.white);

        ApplyCardState(entry, currentCo2);

        entry.actionButton.onClick.AddListener(() =>
        {
            var bioMgr = GetBiodiversityManager();
            if (bioMgr == null) return;

            if (!entry.species.isUnlocked)
            {
                float co2 = CarbonManager1.Instance != null ? CarbonManager1.Instance.CurrentCarbon : 0f;
                if (co2 >= entry.species.costInPpm)
                {
                    if (bioMgr.TryUnlock(entry.species.id))
                    {
                        if (BioacousticAudioManager.Instance != null)
                        {
                            BioacousticAudioManager.Instance.PlayPlantSound();
                        }
                        UpdateExistingCardStates();
                    }
                }
                else
                {
                    if (LumberjackManager.Instance != null)
                    {
                        float missing = entry.species.costInPpm - co2;
                        LumberjackManager.Instance.ShowAlert($"Faltam {Mathf.CeilToInt(missing)} ppm para despertar {entry.species.commonName}!", 2f, new Color(1f, 0.8f, 0.3f));
                    }
                }
            }
        });

        return entry;
    }

    private void UpdateExistingCardStates()
    {
        float currentCo2 = CarbonManager1.Instance != null ? CarbonManager1.Instance.CurrentCarbon : 0f;
        foreach (var entry in cardEntries)
        {
            if (entry != null && entry.rootGo != null)
            {
                ApplyCardState(entry, currentCo2);
            }
        }
    }

    private void ApplyCardState(CardItemEntry entry, float currentCo2)
    {
        if (entry.species.isUnlocked)
        {
            entry.bgImage.color = new Color(0.12f, 0.28f, 0.16f, 0.95f);
            entry.actionButtonImage.color = new Color(0.2f, 0.55f, 0.3f);
            entry.actionButton.interactable = false;
            entry.actionButtonText.text = "<b>Ativo</b>\n<size=80%>(Lv 1)</size>";
            entry.actionButtonText.color = Color.white;
        }
        else if (currentCo2 >= entry.species.costInPpm)
        {
            entry.bgImage.color = new Color(0.14f, 0.18f, 0.24f, 0.95f);
            entry.actionButtonImage.color = new Color(0.18f, 0.55f, 0.85f);
            entry.actionButton.interactable = true;
            entry.actionButtonText.text = $"<b>Despertar</b>\n<size=85%>({entry.species.costInPpm} ppm)</size>";
            entry.actionButtonText.color = Color.white;
        }
        else
        {
            entry.bgImage.color = new Color(0.14f, 0.16f, 0.20f, 0.95f);
            entry.actionButtonImage.color = new Color(0.28f, 0.32f, 0.38f);
            entry.actionButton.interactable = true;
            entry.actionButtonText.text = $"<b>{entry.species.costInPpm} ppm</b>\n<size=80%>(Falta CO2)</size>";
            entry.actionButtonText.color = new Color(0.95f, 0.8f, 0.8f);
        }
    }

    private TextMeshProUGUI CreateText(Transform parent, string content, float fontSize, Color color)
    {
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(parent, false);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null)
        {
            tmp.font = fontAsset;
        }
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private TextMeshProUGUI CreateButtonText(Transform parent, string content, Color color)
    {
        GameObject textGo = new GameObject("BtnText");
        textGo.transform.SetParent(parent, false);
        var txtRt = textGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null)
        {
            tmp.font = fontAsset;
        }
        tmp.text = content;
        tmp.fontSize = 13.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }
}
