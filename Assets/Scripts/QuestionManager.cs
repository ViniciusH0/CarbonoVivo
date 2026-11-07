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

    [Header("Questions Pool (Baralho de 20 Dilemas SOS Mata Atlântica)")]
    public List<QuestionData> availableQuestions = new List<QuestionData>();
    private List<QuestionData> usedQuestions = new List<QuestionData>();

    [Header("Settings")]
    public float fadeSpeed = 5f;

    private QuestionData currentQuestion;
    private bool isFading = false;

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
        if (questionCanvas != null)
        {
            questionCanvas.alpha = 0f;
            questionCanvas.blocksRaycasts = false;
            questionCanvas.interactable = false;
        }

        if (yesButton != null) yesButton.onClick.AddListener(OnYesClicked);
        if (noButton != null) noButton.onClick.AddListener(OnNoClicked);

        // Se a lista estiver vazia no inspetor, carrega todos os QuestionData do projeto
        if (availableQuestions == null || availableQuestions.Count == 0)
        {
            LoadAllQuestionsFromResources();
        }

        StartCoroutine(QuestionRoutine());
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
        
        if (questionCanvas != null)
        {
            questionCanvas.blocksRaycasts = true;
            questionCanvas.interactable = true;
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
        
        indicatorManager.ModifyForestHealth(currentQuestion.yesForestHealth);
        indicatorManager.ModifyWater(currentQuestion.yesWater);
        indicatorManager.ModifyCommunity(currentQuestion.yesCommunity);
        indicatorManager.ModifyResources(currentQuestion.yesResources);

        ShowFeedbackToast(true);
        CloseQuestion();
    }

    public void OnNoClicked()
    {
        if (isFading || currentQuestion == null) return;
        
        indicatorManager.ModifyForestHealth(currentQuestion.noForestHealth);
        indicatorManager.ModifyWater(currentQuestion.noWater);
        indicatorManager.ModifyCommunity(currentQuestion.noCommunity);
        indicatorManager.ModifyResources(currentQuestion.noResources);

        ShowFeedbackToast(false);
        CloseQuestion();
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
    }
}
