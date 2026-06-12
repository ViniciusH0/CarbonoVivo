using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class OnboardingManager : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup onboardingCanvas;
    public TextMeshProUGUI tutorialText;
    public Button startGameButton;

    [Header("Tutorial Content")]
    [TextArea(2, 4)]
    public string[] tutorialLines;
    public float fadeSpeed = 5f;

    private int currentLineIndex = 0;
    private bool isOnboardingActive = false;
    private bool isFading = false;

    void Start()
    {
        onboardingCanvas.alpha = 0f;
        onboardingCanvas.blocksRaycasts = false;
        onboardingCanvas.interactable = false;
        
        // Correção: Sintaxe correta para adicionar evento ao botão no Unity
        startGameButton.onClick.AddListener(StartOnboarding);
    }

    void Update()
    {
        if (!isOnboardingActive || isFading) return;

        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            AdvanceTutorial();
        }
    }

    public void StartOnboarding()
    {
        if (tutorialLines.Length == 0) return;

        // Oculta o botão de start para não atrapalhar o jogo/tutorial
        startGameButton.gameObject.SetActive(false);

        currentLineIndex = 0;
        tutorialText.text = tutorialLines[currentLineIndex];
        isOnboardingActive = true;
        
        onboardingCanvas.blocksRaycasts = true;
        onboardingCanvas.interactable = true;

        StartCoroutine(FadeCanvas(1f));
    }

    private void AdvanceTutorial()
    {
        currentLineIndex++;

        if (currentLineIndex < tutorialLines.Length)
        {
            tutorialText.text = tutorialLines[currentLineIndex];
        }
        else
        {
            EndOnboarding();
        }
    }

    private void EndOnboarding()
    {
        isOnboardingActive = false;
        onboardingCanvas.blocksRaycasts = false;
        onboardingCanvas.interactable = false;

        StartCoroutine(FadeCanvas(0f));
        
        // Coloque a lógica de iniciar a gameplay aqui
    }

    private IEnumerator FadeCanvas(float targetAlpha)
    {
        isFading = true;
        while (Mathf.Abs(onboardingCanvas.alpha - targetAlpha) > 0.01f)
        {
            onboardingCanvas.alpha = Mathf.Lerp(onboardingCanvas.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            yield return null;
        }
        onboardingCanvas.alpha = targetAlpha;
        isFading = false;
    }
}