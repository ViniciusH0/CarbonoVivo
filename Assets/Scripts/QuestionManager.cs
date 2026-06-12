using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class QuestionManager : MonoBehaviour
{
    [Header("References")]
    public IndicatorManager indicatorManager;
    public CanvasGroup questionCanvas;
    public TextMeshProUGUI questionText;
    public Button yesButton;
    public Button noButton;

    [Header("Questions Pool")]
    public List<QuestionData> availableQuestions;

    [Header("Settings")]
    public float fadeSpeed = 5f;

    private QuestionData currentQuestion;
    private bool isFading = false;

    [Header("Question Timing")]
    public float firstQuestionDelay = 30f;
    public float questionInterval = 20f;



    void Start()
{
    questionCanvas.alpha = 0f;
    questionCanvas.blocksRaycasts = false;
    questionCanvas.interactable = false;

    yesButton.onClick.AddListener(OnYesClicked);
    noButton.onClick.AddListener(OnNoClicked);

    StartCoroutine(QuestionRoutine());
}

private IEnumerator QuestionRoutine()
{
    yield return new WaitForSeconds(firstQuestionDelay);

    while (availableQuestions.Count > 0)
    {
        ShowRandomQuestion();

        // Espera o jogador responder
        yield return new WaitUntil(() => currentQuestion == null);

        // Espera até a próxima pergunta
        yield return new WaitForSeconds(questionInterval);
    }
}

    public void ShowRandomQuestion()
    {
        if (isFading || availableQuestions.Count == 0) return;

        int randomIndex = Random.Range(0, availableQuestions.Count);
        currentQuestion = availableQuestions[randomIndex];
        
        availableQuestions.RemoveAt(randomIndex);

        questionText.text = currentQuestion.questionText;
        
        questionCanvas.blocksRaycasts = true;
        questionCanvas.interactable = true;
        StartCoroutine(FadeCanvas(1f));
    }

    private void OnYesClicked()
    {
        if (isFading || currentQuestion == null) return;
        
        indicatorManager.ModifyForestHealth(currentQuestion.yesForestHealth);
        indicatorManager.ModifyWater(currentQuestion.yesWater);
        indicatorManager.ModifyCommunity(currentQuestion.yesCommunity);
        indicatorManager.ModifyResources(currentQuestion.yesResources);
        
        CloseQuestion();
    }

    private void OnNoClicked()
    {
        if (isFading || currentQuestion == null) return;
        
        indicatorManager.ModifyForestHealth(currentQuestion.noForestHealth);
        indicatorManager.ModifyWater(currentQuestion.noWater);
        indicatorManager.ModifyCommunity(currentQuestion.noCommunity);
        indicatorManager.ModifyResources(currentQuestion.noResources);
        
        CloseQuestion();
    }

    private void CloseQuestion()
    {
        questionCanvas.blocksRaycasts = false;
        questionCanvas.interactable = false;
        StartCoroutine(FadeCanvas(0f));
        currentQuestion = null;
    }

    private IEnumerator FadeCanvas(float targetAlpha)
    {
        isFading = true;
        while (Mathf.Abs(questionCanvas.alpha - targetAlpha) > 0.01f)
        {
            questionCanvas.alpha = Mathf.Lerp(questionCanvas.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            yield return null;
        }
        questionCanvas.alpha = targetAlpha;
        isFading = false;
    }
}