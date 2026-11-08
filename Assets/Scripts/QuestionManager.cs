using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class QuestionManager : MonoBehaviour
{
    public static QuestionManager Instance { get; private set; }

    [Header("References")]
    public IndicatorManager indicatorManager;
    public WinScreen winScreen; 
    public CanvasGroup questionCanvas;
    public TextMeshProUGUI questionText;
    public Button yesButton;
    public Button noButton;

    [Header("Audio Focus (Soundtrack vs Humming Sound)")]
    public AudioSource soundtrackSource;
    public AudioSource hummingSource;
    public AudioClip hummingClip;
    private float defaultSoundtrackVolume = 0.72f;
    private bool isQuestionOpen = false;

    private readonly List<CanvasGroup> hiddenHudGroups = new List<CanvasGroup>();
    private Canvas indicatorsOverrideCanvas;

    [Header("Questions Pool (Baralho de 20 Dilemas SOS Mata Atlântica)")]
    public List<QuestionData> availableQuestions = new List<QuestionData>();
    private List<QuestionData> usedQuestions = new List<QuestionData>();

    [Header("Settings")]
    public float fadeSpeed = 5f;

    private QuestionData currentQuestion;
    private bool isFading = false;
    private StepController flow;

    [Header("Question Timing")]
    public float firstQuestionDelay = 18f;
    public float questionInterval = 22f;
    public float QuestionInterval
    {
        get => questionInterval;
        set => questionInterval = Mathf.Max(3f, value);
    }

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        flow = StepController.FindFor(this);
        if (questionCanvas != null)
        {
            questionCanvas.alpha = 0f;
            questionCanvas.blocksRaycasts = false;
            questionCanvas.interactable = false;
        }

        if (yesButton != null) yesButton.onClick.AddListener(OnYesClicked);
        if (noButton != null) noButton.onClick.AddListener(OnNoClicked);

        EnsureIndicatorsCanvasSetup();
        InitializeAudioSources();

        // Se a lista estiver vazia no inspetor, carrega todos os QuestionData do projeto
        if (availableQuestions == null || availableQuestions.Count == 0)
        {
            LoadAllQuestionsFromResources();
        }

        StartCoroutine(QuestionRoutine());
    }

    private void InitializeAudioSources()
    {
        if (soundtrackSource == null)
        {
            var stObj = GameObject.Find("Soundtrack");
            if (stObj != null) soundtrackSource = stObj.GetComponent<AudioSource>();
        }

        if (soundtrackSource != null)
        {
            defaultSoundtrackVolume = soundtrackSource.volume > 0.05f ? soundtrackSource.volume : 0.72f;
        }

        if (hummingSource == null)
        {
            var humObj = GameObject.Find("Soundtrack/HummingSound") ?? GameObject.Find("HummingSound");
            if (humObj != null)
            {
                hummingSource = humObj.GetComponent<AudioSource>();
            }
            else
            {
                GameObject parentGo = soundtrackSource != null ? soundtrackSource.gameObject : gameObject;
                GameObject newHum = new GameObject("HummingSound");
                newHum.transform.SetParent(parentGo.transform, false);
                hummingSource = newHum.AddComponent<AudioSource>();
                hummingSource.loop = true;
                hummingSource.playOnAwake = false;
                hummingSource.spatialBlend = 0f;
                hummingSource.volume = 0f;
            }
        }

        if (hummingSource != null && hummingSource.clip == null)
        {
            if (hummingClip != null)
            {
                hummingSource.clip = hummingClip;
            }
            else
            {
                // Tenta localizar HummingSound carregado na memória
                foreach (var clip in Resources.FindObjectsOfTypeAll<AudioClip>())
                {
                    if (clip != null && clip.name.IndexOf("Humming", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hummingSource.clip = clip;
                        hummingClip = clip;
                        break;
                    }
                }
            }
        }
    }

    private void Update()
    {
        // Transição suave entre a Soundtrack (jogo rolando normal) e o Humming Sound (painel de escolha aberto)
        if (isQuestionOpen)
        {
            if (soundtrackSource != null)
            {
                soundtrackSource.volume = Mathf.MoveTowards(soundtrackSource.volume, 0f, Time.unscaledDeltaTime * 2.2f);
            }

            if (hummingSource != null && hummingSource.clip != null)
            {
                if (!hummingSource.isPlaying) hummingSource.Play();
                hummingSource.volume = Mathf.MoveTowards(hummingSource.volume, 0.78f, Time.unscaledDeltaTime * 2.2f);
            }
        }
        else
        {
            if (soundtrackSource != null)
            {
                if (!soundtrackSource.isPlaying && soundtrackSource.clip != null) soundtrackSource.Play();
                soundtrackSource.volume = Mathf.MoveTowards(soundtrackSource.volume, defaultSoundtrackVolume, Time.unscaledDeltaTime * 1.8f);
            }

            if (hummingSource != null)
            {
                hummingSource.volume = Mathf.MoveTowards(hummingSource.volume, 0f, Time.unscaledDeltaTime * 2.5f);
                if (hummingSource.volume <= 0.005f && hummingSource.isPlaying)
                {
                    hummingSource.Pause();
                }
            }
        }
    }

    private void EnsureIndicatorsCanvasSetup()
    {
        var indicatorsGo = GameObject.Find("UI_Canvas/Game/Indicators");
        if (indicatorsGo != null)
        {
            var rootCanvas = GameObject.Find("UI_Canvas")?.GetComponent<Canvas>() ?? indicatorsGo.GetComponentInParent<Canvas>();
            var c = indicatorsGo.GetComponent<Canvas>();
            if (c == null) c = indicatorsGo.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = 210; // Fica acima do Question (200) e do InputBlocker (190)
            if (rootCanvas != null)
            {
                // CRUCIAL: herda os canais de vértice para que os RoundedImage continuem renderizando suas molduras brancas
                c.additionalShaderChannels = rootCanvas.additionalShaderChannels;
            }
            if (indicatorsGo.GetComponent<GraphicRaycaster>() == null)
                indicatorsGo.AddComponent<GraphicRaycaster>();
        }
    }

    private void SetQuestionUIFocusMode(bool focusActive)
    {
        isQuestionOpen = focusActive;

        // Garante que o painel de status da floresta tenha seu Canvas de alta prioridade com shader channels intactos
        EnsureIndicatorsCanvasSetup();

        // Esconde o resto da UI quando o painel de escolha aparece, e restaura quando fecha
        if (focusActive)
        {
            hiddenHudGroups.Clear();
            var canvasTrans = GameObject.Find("UI_Canvas")?.transform;
            if (canvasTrans != null)
            {
                string[] toHide = new string[]
                {
                    "Game/CO2",
                    "Game/TreesWidget",
                    "Game/TimeClockWidget",
                    "Game/BottomActionsBar",
                    "Game/AlertBanner",
                    "Game/WorldHint",
                    "DebugPanel_TriggerButton",
                    "PauseButton"
                };

                foreach (string p in toHide)
                {
                    Transform t = canvasTrans.Find(p);
                    if (t == null) continue;
                    var cg = t.GetComponent<CanvasGroup>();
                    if (cg == null) cg = t.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = 0f;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                    hiddenHudGroups.Add(cg);
                }
            }
        }
        else
        {
            foreach (var cg in hiddenHudGroups)
            {
                if (cg == null) continue;
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            hiddenHudGroups.Clear();
        }
    }

    private void LoadAllQuestionsFromResources()
    {
        var loaded = Resources.FindObjectsOfTypeAll<QuestionData>();
        if (loaded != null && loaded.Length > 0)
        {
            availableQuestions = new List<QuestionData>(loaded);
        }
    }

    private IEnumerator QuestionRoutine()
    {
        yield return new WaitForSeconds(firstQuestionDelay);

        while (true)
        {
            if (winScreen != null && winScreen.IsGameWon) yield break; 
            if (indicatorManager != null && indicatorManager.IsGameOver) yield break;

            if (availableQuestions.Count == 0 && usedQuestions.Count > 0)
            {
                // Recicla o baralho embaralhado para manter o jogo infinito
                availableQuestions.AddRange(usedQuestions);
                usedQuestions.Clear();
            }

            if (availableQuestions.Count > 0)
            {
                ShowRandomQuestion();
            }

            yield return new WaitUntil(() => currentQuestion == null || (winScreen != null && winScreen.IsGameWon) || (indicatorManager != null && indicatorManager.IsGameOver));

            if ((winScreen != null && winScreen.IsGameWon) || (indicatorManager != null && indicatorManager.IsGameOver))
            {
                if (currentQuestion != null) CloseQuestion();
                yield break;
            }

            yield return new WaitForSeconds(questionInterval);
        }
    }

    public void ShowRandomQuestion()
    {
        if (isFading || availableQuestions.Count == 0) return;

        int randomIndex = Random.Range(0, availableQuestions.Count);
        currentQuestion = availableQuestions[randomIndex];
        
        availableQuestions.RemoveAt(randomIndex);
        usedQuestions.Add(currentQuestion);

        if (questionText != null) questionText.text = currentQuestion.questionText;

        var contextLbl = questionCanvas != null ? questionCanvas.transform.Find("ContextLabel")?.GetComponent<TMPro.TMP_Text>() : null;
        if (contextLbl != null) contextLbl.text = "DILEMA CLIMÁTICO";

        SetQuestionUIFocusMode(true);
        
        if (questionCanvas != null)
        {
            questionCanvas.blocksRaycasts = true;
            questionCanvas.interactable = true;
            if (flow != null) flow.BeginDialogue(questionCanvas);
            StartCoroutine(FadeCanvas(1f));
        }

        if (BioacousticAudioManager.Instance != null)
        {
            BioacousticAudioManager.Instance.PlayAlertSound();
        }
    }

    public void OnYesClicked()
    {
        if (isFading || currentQuestion == null) return;
        
        if (indicatorManager == null) indicatorManager = IndicatorManager.Instance ?? FindAnyObjectByType<IndicatorManager>();

        QuestionData answered = currentQuestion;
        CloseQuestion();

        if (indicatorManager != null && answered != null)
        {
            indicatorManager.ApplyChoiceImpacts(
                answered.yesForestHealth,
                answered.yesWater,
                answered.yesCommunity,
                answered.yesResources
            );
        }

        ShowFeedbackToast(true);
    }

    public void OnNoClicked()
    {
        if (isFading || currentQuestion == null) return;
        
        if (indicatorManager == null) indicatorManager = IndicatorManager.Instance ?? FindAnyObjectByType<IndicatorManager>();

        QuestionData answered = currentQuestion;
        CloseQuestion();

        if (indicatorManager != null && answered != null)
        {
            indicatorManager.ApplyChoiceImpacts(
                answered.noForestHealth,
                answered.noWater,
                answered.noCommunity,
                answered.noResources
            );
        }

        ShowFeedbackToast(false);
    }

    private void ShowFeedbackToast(bool decision)
    {
        string choice = decision ? "Decisão: SIM" : "Decisão: NÃO";
        if (LumberjackManager.Instance != null)
        {
            LumberjackManager.Instance.ShowAlert($"{choice} - Impactos ecológicos aplicados!", 2.2f, decision ? new Color(0.3f, 0.9f, 0.5f) : new Color(0.9f, 0.5f, 0.3f));
        }
    }

    public void CloseQuestion()
    {
        SetQuestionUIFocusMode(false);
        if (questionCanvas != null)
        {
            questionCanvas.blocksRaycasts = false;
            questionCanvas.interactable = false;
            StartCoroutine(FadeCanvas(0f));
        }
        currentQuestion = null;
    }

    private IEnumerator FadeCanvas(float targetAlpha)
    {
        isFading = true;
        while (questionCanvas != null && Mathf.Abs(questionCanvas.alpha - targetAlpha) > 0.01f)
        {
            questionCanvas.alpha = Mathf.Lerp(questionCanvas.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
            yield return null;
        }
        if (questionCanvas != null) questionCanvas.alpha = targetAlpha;
        isFading = false;
        if (targetAlpha == 0 && flow != null) flow.EndDialogue(questionCanvas);
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        StopAllCoroutines();
        SetQuestionUIFocusMode(false);
        if (questionCanvas != null)
        {
            questionCanvas.alpha = 0;
            questionCanvas.blocksRaycasts = questionCanvas.interactable = false;
            if (flow != null) flow.EndDialogue(questionCanvas);
        }
    }
}
