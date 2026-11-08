using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OnboardingManager : MonoBehaviour, IPointerClickHandler
{
    public static OnboardingManager Instance { get; private set; }

    public CanvasGroup onboardingCanvas;
    public TextMeshProUGUI tutorialText;
    public Button startGameButton;
    [TextArea(2, 4)] public string[] tutorialLines;
    public float fadeSpeed = 5;

    [Header("First Tree Tutorial Settings")]
    [TextArea(2, 4)]
    [SerializeField] private string plantFirstTreeInstruction = "Para começar a recuperação, plante sua primeira muda de árvore!\n\nToque no solo da ilha ou clique no botão 'Plantar Muda' abaixo.";

    [TextArea(2, 4)]
    [SerializeField] private string treePlantedCongratulation = "Parabens, agora continue plantando arvores e recuperando a vida da floresta";

    private TextMeshProUGUI continueHintText;
    private int currentLineIndex;
    private bool active;
    private bool fading;
    private bool isWaitingForFirstTree;
    private bool waitingForFinalTap;
    private StepController flow;
    private List<Graphic> cachedGraphics = new List<Graphic>();

    public bool IsActive => active;
    public bool IsWaitingForFirstTree => isWaitingForFirstTree;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        flow = StepController.FindFor(this);
        if (continueHintText == null)
        {
            var hint = transform.Find("ContinueHint");
            if (hint != null) continueHintText = hint.GetComponent<TextMeshProUGUI>();
        }

        if (onboardingCanvas != null)
        {
            onboardingCanvas.alpha = 0;
            onboardingCanvas.blocksRaycasts = onboardingCanvas.interactable = false;
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(StartOnboarding);
            startGameButton.onClick.AddListener(StartOnboarding);
        }
    }

    private void OnEnable()
    {
        InteractivePlanting.OnTreePlanted += HandleTreePlanted;
    }

    private void OnDisable()
    {
        InteractivePlanting.OnTreePlanted -= HandleTreePlanted;

        if (!Application.isPlaying) return;
        StopAllCoroutines();
        active = fading = isWaitingForFirstTree = waitingForFinalTap = false;
        if (onboardingCanvas != null)
        {
            onboardingCanvas.alpha = 0;
            onboardingCanvas.blocksRaycasts = onboardingCanvas.interactable = false;
            if (flow != null) flow.EndDialogue(onboardingCanvas);
        }
    }

    public void StartOnboarding()
    {
        if (active || tutorialLines == null || tutorialLines.Length == 0 || onboardingCanvas == null) return;
        if (flow == null) flow = StepController.FindFor(this);
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);

        currentLineIndex = 0;
        active = true;
        isWaitingForFirstTree = false;
        waitingForFinalTap = false;

        if (tutorialText != null) tutorialText.text = tutorialLines[0];
        if (continueHintText != null) continueHintText.text = "CLIQUE PARA CONTINUAR";

        SetCanvasRaycasts(true);
        onboardingCanvas.blocksRaycasts = onboardingCanvas.interactable = true;

        if (flow != null) flow.BeginDialogue(onboardingCanvas);
        StartCoroutine(FadeCanvas(1));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!active || fading || eventData.button != PointerEventData.InputButton.Left) return;

        // Se está aguardando o jogador plantar a primeira árvore, ignora cliques no diálogo
        if (isWaitingForFirstTree) return;

        if (waitingForFinalTap)
        {
            DismissTutorial();
            return;
        }

        currentLineIndex++;
        if (currentLineIndex < tutorialLines.Length)
        {
            if (tutorialText != null) tutorialText.text = tutorialLines[currentLineIndex];
        }
        else
        {
            EnterPlantFirstTreeStep();
        }
    }

    private void Update()
    {
        // Se a primeira árvore já foi plantada e estamos aguardando o toque de encerramento
        if (waitingForFinalTap && !fading && active)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                DismissTutorial();
            }
        }
    }

    private void EnterPlantFirstTreeStep()
    {
        isWaitingForFirstTree = true;
        waitingForFinalTap = false;

        if (tutorialText != null)
        {
            tutorialText.text = plantFirstTreeInstruction;
        }

        if (continueHintText != null)
        {
            continueHintText.text = "[ PLANTE SUA 1ª MUDA NO SOLO OU CLIQUE EM 'PLANTAR MUDA' ]";
        }

        // Garante que o jogador tem carbono suficiente para plantar a primeira muda
        if (CarbonManager1.Instance != null && CarbonManager1.Instance.CurrentCarbon < 10f)
        {
            CarbonManager1.Instance.AddCarbon(20f);
        }

        // Desbloqueia a física do mundo e os cliques na UI/HUD (como o botão de plantar)
        if (flow != null) flow.EndDialogue(onboardingCanvas);
        Time.timeScale = 1;

        // Desativa interceptação de raycasts nos painéis de fundo para que o clique atinja o chão 3D ou o HUD
        SetCanvasRaycasts(false);
        if (onboardingCanvas != null)
        {
            onboardingCanvas.blocksRaycasts = false;
            onboardingCanvas.interactable = false;
        }
    }

    private void HandleTreePlanted(GameObject tree)
    {
        if (!active || !isWaitingForFirstTree) return;

        isWaitingForFirstTree = false;
        waitingForFinalTap = true;

        if (tutorialText != null)
        {
            tutorialText.text = treePlantedCongratulation;
        }

        if (continueHintText != null)
        {
            continueHintText.text = "CLIQUE PARA CONTINUAR";
        }

        // Reativa a interceptação de cliques para que qualquer toque na tela encerre o tutorial
        SetCanvasRaycasts(true);
        if (onboardingCanvas != null)
        {
            onboardingCanvas.blocksRaycasts = true;
            onboardingCanvas.interactable = true;
        }
    }

    private void DismissTutorial()
    {
        if (!active || fading) return;

        waitingForFinalTap = false;
        isWaitingForFirstTree = false;
        active = false;

        if (onboardingCanvas != null)
        {
            onboardingCanvas.interactable = false;
            StartCoroutine(FadeCanvas(0));
        }
    }

    private void SetCanvasRaycasts(bool enable)
    {
        if (onboardingCanvas == null) return;

        cachedGraphics.Clear();
        onboardingCanvas.GetComponentsInChildren(true, cachedGraphics);

        foreach (var graphic in cachedGraphics)
        {
            if (graphic == null) continue;

            // Mantém os textos sem bloquear raycasts
            if (graphic is TextMeshProUGUI)
            {
                graphic.raycastTarget = false;
                continue;
            }

            graphic.raycastTarget = enable;
        }
    }

    private IEnumerator FadeCanvas(float target)
    {
        fading = true;
        while (!Mathf.Approximately(onboardingCanvas.alpha, target))
        {
            onboardingCanvas.alpha = Mathf.MoveTowards(onboardingCanvas.alpha, target,
                Time.unscaledDeltaTime * Mathf.Max(.1f, fadeSpeed));
            yield return null;
        }
        fading = false;
        if (target == 0)
        {
            onboardingCanvas.blocksRaycasts = false;
            if (flow != null) flow.EndDialogue(onboardingCanvas);
        }
    }
}
