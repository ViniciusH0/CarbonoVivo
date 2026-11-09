using System.Collections.Generic;
using DTT.UI.ProceduralUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Educational moments for the test scene. All progress belongs to the current run.
public class ForestStoryDirector : MonoBehaviour
{
    public static ForestStoryDirector Instance { get; private set; }
    public bool IsOpen => panel != null && panel.alpha > 0f;

    private class Message
    {
        public string key, title, body, source;
        public bool replay;
    }

    private readonly List<Message> pending = new List<Message>();
    private readonly HashSet<string> scheduled = new HashSet<string>();
    private readonly HashSet<string> read = new HashSet<string>();
    private StepController flow;
    private BiodiversityManager biodiversity;
    private CanvasGroup panel;
    private TextMeshProUGUI titleLabel, bodyLabel, sourceLabel;
    private Button continueButton;
    private float nextMessageTime, safeAfter, openedAt;

    private void Awake() { Instance = this; }

    private void Start()
    {
        flow = StepController.FindFor(this);
        biodiversity = BiodiversityManager.Instance;
        if (biodiversity != null) biodiversity.OnSpeciesUnlocked += OnSpeciesUnlocked;
        InteractivePlanting.OnTreePlanted += OnTreePlanted;
        BuildPanel();
    }

    private bool HasThreat =>
        (ForestFireManager.Instance != null && ForestFireManager.Instance.IsFireActive) ||
        (LumberjackManager.Instance != null && LumberjackManager.Instance.IsAttackActive);

    private void Update()
    {
        if (IsOpen)
        {
            continueButton.interactable = Time.unscaledTime - openedAt >= 0.35f;
            if (flow != null && !flow.IsGameplayActive) CloseMessage();
            return;
        }

        if (HasThreat)
        {
            safeAfter = Time.unscaledTime + 1f;
            if (ForestFireManager.Instance != null && ForestFireManager.Instance.IsFireActive)
                Enqueue("first-fire", "Depois das chamas",
                    "As chamas ameaçam mais que as árvores: elas também atingem o espaço onde a vida está voltando. Recuperar a floresta exige proteger o que já cresceu.", "Carbono Vivo · jornada de restauração");
            if (LumberjackManager.Instance != null && LumberjackManager.Instance.IsAttackActive)
                Enqueue("first-attack", "Cada árvore faz parte de algo maior",
                    "Uma árvore faz parte do abrigo, da sombra e do alimento de outros seres vivos. Defender a floresta também é cuidar dessas relações.", "Carbono Vivo · jornada de restauração");
            return;
        }

        var indicators = IndicatorManager.Instance;
        if (indicators != null && indicators.forestHealth >= 7f &&
            TreeSpawner.Instance != null && TreeSpawner.Instance.GetTreeCount() >= 5)
            Enqueue("recovery", "A vida encontra espaço",
                "Veja o que estamos recuperando juntos! Plantas, animais e solo fazem parte da mesma rede. O próximo desafio é manter essa floresta saudável.", "Carbono Vivo · jornada de restauração");

        if (pending.Count == 0 || panel == null || flow == null || !flow.IsGameplayActive ||
            flow.HasDialogue || StepController.WorldInputBlocked || Time.unscaledTime < safeAfter ||
            (OnboardingManager.Instance != null && OnboardingManager.Instance.IsActive)) return;

        var message = pending[0];
        if (!message.replay && (Time.time < nextMessageTime ||
            (BiodiversityUI.Instance != null && BiodiversityUI.Instance.IsOpen) ||
            (EcologyHUD.Instance != null && EcologyHUD.Instance.IsOpen))) return;
        pending.RemoveAt(0);
        if (!message.replay && read.Contains(message.key)) return;
        read.Add(message.key);
        titleLabel.text = message.title;
        bodyLabel.text = message.body;
        sourceLabel.text = message.source;
        panel.transform.SetAsLastSibling();
        panel.alpha = 1;
        panel.blocksRaycasts = panel.interactable = true;
        openedAt = Time.unscaledTime;
        continueButton.interactable = false;
        flow.BeginDialogue(panel);
    }

    private void Enqueue(string key, string title, string body, string source)
    {
        if (!scheduled.Add(key)) return;
        pending.Add(new Message { key = key, title = title, body = body, source = source });
    }

    private void OnTreePlanted(GameObject tree)
    {
        Enqueue("first-tree", "Um começo para a floresta",
            "Uma muda é um começo. Para essa floresta se recuperar, vamos precisar de muitas formas de vida. No catálogo de biodiversidade, você vai conhecer algumas delas.", "Carbono Vivo · jornada de restauração");
    }

    private void OnSpeciesUnlocked(SpeciesData species)
    {
        var message = SpeciesMessage(species);
        if (message != null) Enqueue(message.key, message.title, message.body, message.source);
    }

    public void ShowSpeciesCuriosity(SpeciesData species)
    {
        if (species == null || !species.isUnlocked || IsOpen) return;
        var message = SpeciesMessage(species);
        if (message == null) return;
        message.replay = true;
        pending.Insert(0, message);
    }

    public void OnDecisionResolved()
    {
        Enqueue("first-choice", "A floresta também chega à cidade",
            "Uma decisão pode beneficiar um indicador e exigir recursos de outro. Observe os impactos das suas escolhas: cuidar da floresta também envolve a água e a comunidade que depende dela.", "Inspiração: PIME 2 · relação floresta, água e cidade, pp. 6–8");
    }

    private static Message SpeciesMessage(SpeciesData species)
    {
        if (species == null) return null;
        string body, source;
        switch (species.id)
        {
            case "guapuruvu":
                body = "O guapuruvu é uma árvore pioneira: pode participar das primeiras etapas de recuperação da floresta. Uma área em restauração precisa de diferentes espécies ao longo do tempo.";
                source = "Fonte: Programa Arboretum · ficha Schizolobium parahyba";
                break;
            case "araucaria":
                body = "O pinhão é a semente da araucária e serve de alimento para animais como a gralha-azul. Uma árvore também pode fazer parte da história de muitas outras espécies.";
                source = "Fonte: Paraná Turismo · Gralha Azul do Paraná";
                break;
            case "gralha_mico":
                body = "<b>Gralha-azul:</b> utiliza pinhões como alimento e pode transportá-los e armazená-los.\n\n<b>Mico-leão-dourado:</b> pode dispersar sementes de frutos que consome.\n\nSão dois exemplos de como animais participam da renovação das florestas.";
                source = "Fontes: Paraná Turismo · Gralha Azul; USP · estudo de dispersão pelo mico-leão-dourado";
                break;
            case "mico":
                body = "O mico-leão-dourado pode dispersar sementes de frutos que consome. Assim, participa da renovação da floresta.\n\nNo jogo, seu brotamento automático cria novas árvores sem custo de plantio. Elas passam a fazer parte da manutenção da floresta.";
                source = "Fonte: USP · estudo de dispersão de sementes pelo mico-leão-dourado";
                break;
            case "gralha":
                body = "A gralha-azul utiliza pinhões como alimento e pode transportá-los e armazená-los. O pinhão é a semente da araucária.\n\nNo jogo, a gralha amplia o bônus de captura de uma floresta com espécies variadas.";
                source = "Fonte: Paraná Turismo · Gralha Azul do Paraná";
                break;
            case "embauba":
                body = "A embaúba é uma espécie pioneira e pode participar da recuperação de áreas degradadas. Uma floresta em restauração precisa de diferentes espécies ao longo do tempo.";
                source = "Fonte: Embrapa · Guia de plantas forrageiras, Cecropia pachystachya";
                break;
            case "jequitiba":
                body = "O jequitibá-rosa é uma árvore de grande porte da Mata Atlântica.\n\nNo jogo, sua muda demora mais para crescer e exige mais manutenção, mas oferece maior captura quando adulta. Esses valores são regras de balanceamento.";
                source = "Fonte: ICMBio · Biodiversidade da Reserva Biológica de Sooretama";
                break;
            case "besouro_rola_bosta":
                body = "Ao enterrar fezes, os besouros rola-bosta participam da reciclagem de matéria orgânica e ajudam a enriquecer o solo. Até os pequenos habitantes têm um papel na floresta!";
                source = "Fonte: Prefeitura de Curitiba · Proteção Animal, Besouro rola-bosta";
                break;
            case "tamandua_mirim":
                body = "O tamanduá-mirim se alimenta principalmente de formigas e cupins. Conhecer sua alimentação ajuda a perceber as ligações entre os animais e o ambiente.\n\nNo jogo, afastar lenhadores é um bônus de proteção da floresta.";
                source = "Fonte: Instituto Água e Terra do Paraná · Tamanduá-mirim";
                break;
            default: return null;
        }
        return new Message { key = "species-" + species.id, title = species.commonName,
            body = body, source = source };
    }

    public void CloseMessage()
    {
        if (!IsOpen || Time.unscaledTime - openedAt < 0.35f) return;
        HidePanel();
        nextMessageTime = Time.time + 12f;
    }

    private void HidePanel()
    {
        if (panel == null) return;
        panel.alpha = 0;
        panel.blocksRaycasts = panel.interactable = false;
        if (flow != null) flow.EndDialogue(panel);
    }

    private void OnDestroy()
    {
        InteractivePlanting.OnTreePlanted -= OnTreePlanted;
        if (biodiversity != null) biodiversity.OnSpeciesUnlocked -= OnSpeciesUnlocked;
        if (Instance == this) Instance = null;
    }

    private void OnDisable() { HidePanel(); }

    private void BuildPanel()
    {
        var question = FindAnyObjectByType<QuestionManager>();
        var root = question != null && question.questionCanvas != null
            ? question.questionCanvas.GetComponentInParent<Canvas>() : null;
        if (root == null) return;
        root = root.rootCanvas;
        var go = new GameObject("ForestStoryPopup", typeof(RectTransform), typeof(CanvasGroup));
        go.layer = 5;
        go.transform.SetParent(root.transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel = go.GetComponent<CanvasGroup>();
        var backdrop = go.AddComponent<Image>();
        backdrop.color = new Color(0.03f, 0.09f, 0.06f, 0.78f);
        var canvas = go.AddComponent<Canvas>();
        canvas.overrideSorting = true; canvas.sortingOrder = 200;
        canvas.additionalShaderChannels = root.additionalShaderChannels;
        go.AddComponent<GraphicRaycaster>();

        var robot = question.questionCanvas.transform.Find("Robot");
        if (robot != null)
        {
            var copy = Instantiate(robot.gameObject, go.transform, false);
            var r = copy.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(-620f, -100f);
            r.sizeDelta = new Vector2(390f, 490f);
            foreach (var graphic in copy.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
        }

        var card = NewRect("StoryCard", go.transform, new Vector2(140f, 0f), new Vector2(1020f, 550f));
        var surface = card.gameObject.AddComponent<RoundedImage>();
        var original = question.questionCanvas.transform.Find("DialogBox2/OrganicSurface")?.GetComponent<RoundedImage>();
        if (original != null) surface.CopyFrom(original);
        else surface.SetCornerRounding(0.12f);
        surface.color = new Color(0.957f, 0.945f, 0.898f);
        var font = question.questionText != null ? question.questionText.font : TMP_Settings.defaultFontAsset;
        var ink = new Color(0.07f, 0.24f, 0.19f);
        Label("Eyebrow", card, "DIÁRIO DA FLORESTA", font, 18f, new Vector2(0f, 225f), new Vector2(920f, 28f), ink);
        titleLabel = Label("Title", card, "", font, 30f, new Vector2(0f, 165f), new Vector2(920f, 75f), ink);
        titleLabel.fontStyle = FontStyles.Bold;
        bodyLabel = Label("Body", card, "", font, 25f, new Vector2(0f, 0f), new Vector2(900f, 230f), ink);
        bodyLabel.alignment = TextAlignmentOptions.MidlineLeft;
        sourceLabel = Label("Source", card, "", font, 15f, new Vector2(0f, -155f), new Vector2(920f, 50f), ink);
        var buttonRect = NewRect("ContinueButton", card, new Vector2(0f, -225f), new Vector2(320f, 62f));
        var buttonImage = buttonRect.gameObject.AddComponent<Image>();
        continueButton = buttonRect.gameObject.AddComponent<Button>();
        continueButton.targetGraphic = buttonImage;
        GameSceneTestUITheme.Surface(buttonRect, GameSceneTestUITheme.Green, 16f);
        continueButton.onClick.AddListener(CloseMessage);
        Label("Text", buttonRect, "Continuar", font, 22f, Vector2.zero, new Vector2(300f, 50f), Color.white);
        HidePanel();
    }

    private static RectTransform NewRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }

    private static TextMeshProUGUI Label(string name, Transform parent, string text, TMP_FontAsset font,
        float size, Vector2 position, Vector2 bounds, Color color)
    {
        var label = NewRect(name, parent, position, bounds).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.fontSize = size; label.text = text; label.color = color;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        return label;
    }
}
