using System.Collections.Generic;
using DTT.UI.ProceduralUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Visual experiment used exclusively by GameScene_Test. No gameplay changes.</summary>
[DisallowMultipleComponent]
public sealed class GameSceneTestUITheme : MonoBehaviour
{
    public static readonly Color Forest = Hex("183E32");
    public static readonly Color Green = Hex("397458");
    public static readonly Color Cream = Hex("F5F1E6");
    public static readonly Color Paper = Hex("E8EBDD");
    public static readonly Color Muted = Hex("60766A");
    public static readonly Color Gold = Hex("DCC98B");
    public TMP_FontAsset font;
    private float nextRefresh;
    private readonly List<Image> buttonHitAreas = new List<Image>();

    private void Awake()
    {
        if (gameObject.scene.path != "Assets/Scenes/GameScene_Test.unity") return;
        var background = transform.Find("BG (1)");
        if (background == null) return;
        background.gameObject.SetActive(true);
        background.SetAsFirstSibling();
        Stretch(background);
        var image = background.GetComponent<Image>();
        image.enabled = true; image.sprite = null; image.material = null;
        image.color = Cream; image.raycastTarget = false;
    }

    private void Start() => CacheButtonHitAreas();

    public static void ConfigureHitArea(Button button)
    {
        var area = button.GetComponent<Image>();
        if (area == null) return;
        area.enabled = true; area.sprite = null; area.color = Color.clear;
        area.raycastTarget = true; area.raycastPadding = Vector4.zero;
        area.alphaHitTestMinimumThreshold = 0;
        var artwork = button.transform.Find("OrganicSurface");
        if (artwork != null) artwork.GetComponent<Image>().raycastTarget = false;
    }

    private void CacheButtonHitAreas()
    {
        buttonHitAreas.Clear();
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            ConfigureHitArea(button);
            var image = button.GetComponent<Image>();
            if (image != null) buttonHitAreas.Add(image);
        }
    }

    private void LateUpdate()
    {
        if (gameObject.scene.path != "Assets/Scenes/GameScene_Test.unity") return;
        // BiodiversityUI updates its original Images in Update. Keep those input
        // graphics invisible before rendering, without interfering with its state.
        foreach (var image in buttonHitAreas)
            if (image != null && image.color != Color.clear) image.color = Color.clear;
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .15f;
        var clock = transform.Find("Game/TimeClockWidget");
        if (clock != null && clock.Find("OrganicSurface") == null)
        {
            Place(clock, 205, -162, 330, 38, 0, 1);
            Surface(clock, Cream, 12);
            var text = clock.GetComponentInChildren<TMP_Text>();
            text.font = font; text.fontSize = 17; text.color = Forest;
        }
        var debug = transform.Find("DebugPanel_TriggerButton");
        if (debug != null && debug.Find("OrganicSurface") == null)
        {
            Place(debug, 110, 38, 150, 42, 0, 0);
            Surface(debug, Forest, 12);
            var outline = debug.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
            var text = debug.GetComponentInChildren<TMP_Text>();
            text.font = font; text.fontSize = 15; text.color = Cream;
        }
        var panel = transform.Find("Game/BiodiversityPanel");
        if (panel != null)
        {
            StyleCatalog(panel, font);
            CacheButtonHitAreas();
        }
    }

    public static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var c); return c; }

    public static void Place(Transform t, float x, float y, float w, float h, float ax = .5f, float ay = .5f)
    {
        if (t == null) return;
        var r = t as RectTransform;
        r.anchorMin = r.anchorMax = new Vector2(ax, ay);
        r.pivot = new Vector2(.5f, .5f);
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(w, h);
        r.localScale = Vector3.one;
    }

    public static void Stretch(Transform t, float inset = 0)
    {
        var r = t as RectTransform;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.one * inset; r.offsetMax = Vector2.one * -inset;
        r.localScale = Vector3.one;
    }

    // Retain the original Image so serialized game references remain valid.
    // The rounded child is the visible graphic; input bubbles to the existing Button.
    public static RoundedImage Surface(Transform t, Color color, float radius = 20)
    {
        var child = t.Find("OrganicSurface");
        RoundedImage rounded;
        if (child == null)
        {
            var go = new GameObject("OrganicSurface", typeof(RectTransform), typeof(LayoutElement), typeof(RoundedImage));
            go.transform.SetParent(t, false);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            rounded = go.GetComponent<RoundedImage>();
        }
        else rounded = child.GetComponent<RoundedImage>();
        rounded.transform.SetAsFirstSibling();
        Stretch(rounded.transform);
        rounded.color = color;
        rounded.RoundingUnit = RoundingUnit.PERCENTAGE;
        var bounds = rounded.rectTransform.rect.size;
        rounded.SetCornerRounding(Mathf.Clamp01(2 * radius / Mathf.Max(1, Mathf.Min(bounds.x, bounds.y))));
        rounded.DistanceFalloff = .8f;
        var old = t.GetComponent<Image>();
        if (old != null && old != rounded) old.enabled = t.GetComponent<Button>() != null;
        var button = t.GetComponent<Button>();
        rounded.raycastTarget = button != null || (old != null && old.raycastTarget);
        if (button != null && button.targetGraphic != rounded)
        {
            button.targetGraphic = rounded;
            var cb = ColorBlock.defaultColorBlock;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.14f, 1.14f, 1.08f);
            cb.pressedColor = new Color(.78f, .86f, .79f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(.7f, .77f, .7f, .85f);
            cb.fadeDuration = .12f;
            button.colors = cb;
        }
        if (button != null) ConfigureHitArea(button);
        return rounded;
    }

    public static TMP_Text Label(Transform parent, string name, string content, float size, Color color, TMP_FontAsset font)
    {
        var t = parent.Find(name);
        if (t == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false); t = go.transform;
        }
        var tmp = t.GetComponent<TMP_Text>();
        tmp.text = content; tmp.font = font; tmp.fontSize = size; tmp.color = color;
        tmp.raycastTarget = false; tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    public static void StyleCatalog(Transform panel, TMP_FontAsset font)
    {
        var content = panel.Find("ScrollView/Viewport/Content");
        if (content == null) return;
        var list = content.GetComponent<VerticalLayoutGroup>();
        list.spacing = 14; list.padding = new RectOffset(4, 4, 4, 12);
        list.childControlHeight = true;
        list.childForceExpandHeight = false;
        foreach (Transform card in content)
        {
            if (!card.name.StartsWith("Card_")) continue;
            bool fresh = card.Find("OrganicSurface") == null;
            var button = card.Find("ActionBtn")?.GetComponent<Button>();
            if (button == null) continue;
            bool active = !button.interactable;
            Surface(card, active ? Hex("DDE9D7") : Cream, 18);
            Surface(button.transform, active ? Muted : Green, 16);
            if (fresh)
            {
                var le = card.GetComponent<LayoutElement>(); le.preferredHeight = 204; le.minHeight = 204;
                var row = card.GetComponent<HorizontalLayoutGroup>();
                row.padding = new RectOffset(22, 22, 20, 20); row.spacing = 22;
                row.childControlWidth = true; row.childControlHeight = true;
                row.childForceExpandWidth = false; row.childForceExpandHeight = false;
                var info = card.Find("Info");
                var infoLe = info.GetComponent<LayoutElement>(); infoLe.preferredHeight = 164;
                var col = info.GetComponent<VerticalLayoutGroup>(); col.spacing = 8;
                col.childControlHeight = true; col.childForceExpandHeight = false;
                var texts = info.GetComponentsInChildren<TMP_Text>();
                for (int i = 0; i < texts.Length; i++)
                {
                    var txt = texts[i]; txt.font = font; txt.fontSize = i == 0 ? 21 : 16;
                    txt.color = i == 2 ? Muted : Forest;
                    txt.enableAutoSizing = true; txt.fontSizeMin = i == 0 ? 18 : 14;
                    txt.fontSizeMax = i == 0 ? 21 : 16;
                    txt.alignment = TextAlignmentOptions.MidlineLeft;
                    var tl = txt.GetComponent<LayoutElement>();
                    if (tl == null) tl = txt.gameObject.AddComponent<LayoutElement>();
                    tl.preferredHeight = i == 0 ? 52 : 48; tl.flexibleHeight = 0;
                }
                var btnLe = button.GetComponent<LayoutElement>();
                btnLe.minWidth = 148; btnLe.preferredWidth = 148; btnLe.preferredHeight = 70;
                var bt = button.GetComponentInChildren<TMP_Text>(); bt.font = font; bt.fontSize = 18;
            }
            var label = button.GetComponentInChildren<TMP_Text>();
            label.color = Cream;
        }
    }
}
