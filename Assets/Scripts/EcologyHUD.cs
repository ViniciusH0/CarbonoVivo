using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Opens the management catalog from the same action bar as Biodiversity.
public class EcologyHUD : MonoBehaviour
{
    public static EcologyHUD Instance { get; private set; }
    public bool IsOpen { get; private set; }
    private CanvasGroup panel, abilities;
    private BiodiversityUI trees;
    private TMP_FontAsset font;
    private Button treeTab, abilityTab;
    private float refreshAt, lastToggleAt = -1;
    private bool showingTrees = true;
    private readonly List<AbilityCard> cards = new List<AbilityCard>();

    private class AbilityCard
    {
        public string id;
        public TMP_Text effect, cost, status;
        public Button button;
    }

    private void Awake() { Instance = this; }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    private void OnDisable() { CloseCatalog(); }

    private void Start()
    {
        var root = GameObject.Find("UI_Canvas")?.transform.Find("Game");
        if (root == null) return;
        font = QuestionManager.Instance != null && QuestionManager.Instance.questionText != null
            ? QuestionManager.Instance.questionText.font : TMP_Settings.defaultFontAsset;
        BuildButton(root);
        BuildPanel(root);
    }

    private void BuildButton(Transform game)
    {
        var bar = game.Find("BottomActionsBar") as RectTransform;
        var original = bar != null ? bar.Find("BiodiversityButton") : null;
        if (original == null) return;
        var copy = Instantiate(original.gameObject, bar, false);
        copy.name = "ForestManagementButton";
        var button = copy.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(ToggleCatalog);
        copy.GetComponentInChildren<TMP_Text>().text = "Manejo da Floresta";
        original.GetComponentInChildren<TMP_Text>().text = "Biodiversidade (Animais)";
        bar.sizeDelta = new Vector2(1120, bar.sizeDelta.y);
        var layout = bar.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.enabled = true;
            layout.spacing = 20;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(bar);
    }

    private void BuildPanel(Transform game)
    {
        var go = new GameObject("ForestManagementPanel", typeof(RectTransform), typeof(CanvasGroup));
        go.SetActive(false);
        go.layer = 5;
        go.transform.SetParent(game, false);
        var rect = go.GetComponent<RectTransform>();
        var source = game.Find("BiodiversityPanel") as RectTransform;
        rect.anchorMin = source != null ? source.anchorMin : new Vector2(1, .5f);
        rect.anchorMax = source != null ? source.anchorMax : new Vector2(1, .5f);
        rect.pivot = source != null ? source.pivot : new Vector2(.5f, .5f);
        rect.anchoredPosition = source != null ? source.anchoredPosition : new Vector2(-392, 0);
        rect.sizeDelta = source != null ? source.sizeDelta : new Vector2(720, 860);
        panel = go.GetComponent<CanvasGroup>();
        go.AddComponent<Image>();
        GameSceneTestUITheme.Surface(go.transform, GameSceneTestUITheme.Paper, 24);
        Label(go.transform, "Title", "Manejo da Floresta", 30, new Vector2(.5f, 1), new Vector2(-18, -62), new Vector2(600, 58));
        var close = MakeButton(go.transform, "CloseButton", "×", new Vector2(1, 1), new Vector2(-48, -48), new Vector2(52, 52));
        close.onClick.AddListener(CloseCatalog);
        treeTab = MakeButton(go.transform, "TreesTab", "Árvores", new Vector2(.5f, 1), new Vector2(-158, -124), new Vector2(304, 55));
        abilityTab = MakeButton(go.transform, "AbilitiesTab", "Habilidades", new Vector2(.5f, 1), new Vector2(158, -124), new Vector2(304, 55));
        treeTab.onClick.AddListener(() => ShowTab(true));
        abilityTab.onClick.AddListener(() => ShowTab(false));

        var treeFrame = Frame(go.transform, "TreesCatalog");
        treeFrame.gameObject.AddComponent<CanvasGroup>();
        BuildScroll(treeFrame);
        trees = treeFrame.gameObject.AddComponent<BiodiversityUI>();
        trees.TreesOnly = true;

        var abilityFrame = Frame(go.transform, "AbilitiesCatalog");
        abilities = abilityFrame.gameObject.AddComponent<CanvasGroup>();
        var content = BuildScroll(abilityFrame);
        foreach (var ability in EcologySimulation.Config.abilities) BuildAbilityCard(content, ability.id);
        go.SetActive(true);
        CloseCatalog();
    }

    public void ToggleCatalog()
    {
        if (Time.unscaledTime - lastToggleAt < .15f) return;
        lastToggleAt = Time.unscaledTime;
        if (IsOpen) CloseCatalog(); else OpenCatalog();
    }

    public void OpenCatalog()
    {
        if (panel == null || StepController.WorldInputBlocked) return;
        BiodiversityUI.Instance?.CloseCatalog();
        IsOpen = true;
        panel.transform.SetAsLastSibling();
        panel.alpha = 1;
        panel.interactable = panel.blocksRaycasts = true;
        ShowTab(showingTrees);
    }

    public void CloseCatalog()
    {
        IsOpen = false;
        if (panel == null) return;
        panel.alpha = 0;
        panel.interactable = panel.blocksRaycasts = false;
        if (trees != null) trees.CloseCatalog();
    }

    private void ShowTab(bool treesTab)
    {
        showingTrees = treesTab;
        if (treesTab)
        {
            trees.OpenCatalog();
            GameSceneTestUITheme.StyleCatalog(trees.transform, font);
        }
        else trees.CloseCatalog();
        abilities.alpha = treesTab ? 0 : 1;
        abilities.interactable = abilities.blocksRaycasts = !treesTab;
        treeTab.transform.Find("OrganicSurface").GetComponent<Image>().color = treesTab ? GameSceneTestUITheme.Green : GameSceneTestUITheme.Muted;
        abilityTab.transform.Find("OrganicSurface").GetComponent<Image>().color = treesTab ? GameSceneTestUITheme.Muted : GameSceneTestUITheme.Green;
        RefreshAbilities();
    }

    private void Update()
    {
        if (!IsOpen || Time.unscaledTime < refreshAt) return;
        refreshAt = Time.unscaledTime + .2f;
        if (showingTrees) GameSceneTestUITheme.StyleCatalog(trees.transform, font);
        else RefreshAbilities();
    }

    private void BuildAbilityCard(Transform parent, string id)
    {
        var card = Rect(parent, "Ability_" + id, new Vector2(.5f, 1), Vector2.zero, new Vector2(664, 228));
        card.gameObject.AddComponent<LayoutElement>().preferredHeight = 228;
        GameSceneTestUITheme.Surface(card, GameSceneTestUITheme.Cream, 18);
        var ability = EcologySimulation.Config.Ability(id);
        Label(card, "Title", ability.name, 23, new Vector2(0, 1), new Vector2(220, -33), new Vector2(396, 48));
        var entry = new AbilityCard { id = id };
        entry.effect = Label(card, "Effect", "", 19, new Vector2(0, .5f), new Vector2(220, -2), new Vector2(396, 90));
        entry.cost = Label(card, "Cost", "", 17, new Vector2(0, 0), new Vector2(220, 35), new Vector2(396, 52));
        entry.cost.color = GameSceneTestUITheme.Muted;
        entry.button = MakeButton(card, "Activate", "Ativar", new Vector2(1, .5f), new Vector2(-110, 0), new Vector2(174, 76));
        entry.status = entry.button.GetComponentInChildren<TMP_Text>();
        entry.button.onClick.AddListener(() => { EcologySimulation.Instance.TryUseAbility(id); RefreshAbilities(); });
        cards.Add(entry);
    }

    private void RefreshAbilities()
    {
        var sim = EcologySimulation.Instance;
        if (sim == null) return;
        foreach (var card in cards)
        {
            var ability = EcologySimulation.Config.Ability(card.id);
            card.effect.text = card.id == "agua" ? $"Recupera {ability.effect:0.#} pontos percentuais de Água imediatamente."
                : card.id == "manejo" ? $"+{ability.effect:0.#}% de captura por {ability.duration:0.#} s."
                : $"Reduz os danos de fogo em {ability.effect:0.#}% por {ability.duration:0.#} s.";
            card.cost.text = $"Custo: {ability.cost:0.#} p.p. de Recursos\nRecarga: {ability.cooldown:0.#} s";
            float cooldown = sim.RemainingCooldown(card.id);
            bool afford = IndicatorManager.Instance != null && IndicatorManager.Instance.resources > ability.cost / 10f;
            card.button.interactable = cooldown <= 0 && afford && !StepController.WorldInputBlocked;
            card.status.text = sim.AbilityActive(card.id) ? $"Ativa\n<size=75%>{Mathf.CeilToInt(cooldown)} s de recarga</size>"
                : cooldown > 0 ? $"Recarga\n<size=80%>{Mathf.CeilToInt(cooldown)} s</size>" : afford ? "Ativar" : "Recursos\ninsuficientes";
        }
    }

    private static RectTransform Frame(Transform parent, string name)
    {
        var frame = Rect(parent, name, Vector2.zero, Vector2.zero, Vector2.zero);
        frame.anchorMax = Vector2.one;
        frame.offsetMin = new Vector2(24, 30); frame.offsetMax = new Vector2(-24, -170);
        return frame;
    }

    private static Transform BuildScroll(Transform parent)
    {
        var scrollGo = Rect(parent, "ScrollView", Vector2.zero, Vector2.zero, Vector2.zero);
        scrollGo.anchorMax = Vector2.one; scrollGo.offsetMin = scrollGo.offsetMax = Vector2.zero;
        var scroll = scrollGo.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.scrollSensitivity = 25;
        var viewport = Rect(scrollGo, "Viewport", Vector2.zero, Vector2.zero, Vector2.zero);
        viewport.anchorMax = Vector2.one; viewport.offsetMin = viewport.offsetMax = Vector2.zero;
        viewport.gameObject.AddComponent<Image>().color = Color.white;
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect(viewport, "Content", new Vector2(0, 1), Vector2.zero, Vector2.zero);
        content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 12); layout.spacing = 14;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport; scroll.content = content;
        return content;
    }

    private Button MakeButton(Transform parent, string name, string text, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var rect = Rect(parent, name, anchor, position, size);
        rect.gameObject.AddComponent<Image>();
        var button = rect.gameObject.AddComponent<Button>();
        GameSceneTestUITheme.Surface(rect, GameSceneTestUITheme.Green, 12);
        var label = Label(rect, "Text", text, 20, new Vector2(.5f, .5f), Vector2.zero, size - new Vector2(12, 8));
        label.alignment = TextAlignmentOptions.Center; label.color = GameSceneTestUITheme.Cream;
        return button;
    }

    private TMP_Text Label(Transform parent, string name, string text, float size, Vector2 anchor, Vector2 position, Vector2 bounds)
    {
        var label = Rect(parent, name, anchor, position, bounds).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text; label.font = font; label.fontSize = size;
        label.alignment = TextAlignmentOptions.MidlineLeft; label.color = GameSceneTestUITheme.Forest;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5; go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
}
