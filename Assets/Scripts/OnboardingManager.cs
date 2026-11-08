using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OnboardingManager : MonoBehaviour, IPointerClickHandler
{
    public CanvasGroup onboardingCanvas;
    public TextMeshProUGUI tutorialText;
    public Button startGameButton;
    [TextArea(2, 4)] public string[] tutorialLines;
    public float fadeSpeed = 5;
    private int currentLineIndex;
    private bool active;
    private bool fading;
    private StepController flow;

    private void Start()
    {
        flow = StepController.FindFor(this);
        onboardingCanvas.alpha = 0;
        onboardingCanvas.blocksRaycasts = onboardingCanvas.interactable = false;
        if (startGameButton != null) startGameButton.onClick.AddListener(StartOnboarding);
    }

    public void StartOnboarding()
    {
        if (active || tutorialLines == null || tutorialLines.Length == 0 || onboardingCanvas == null) return;
        if (flow == null) flow = StepController.FindFor(this);
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);
        currentLineIndex = 0; active = true;
        tutorialText.text = tutorialLines[0];
        onboardingCanvas.blocksRaycasts = onboardingCanvas.interactable = true;
        if (flow != null) flow.BeginDialogue(onboardingCanvas);
        StartCoroutine(FadeCanvas(1));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!active || fading || eventData.button != PointerEventData.InputButton.Left) return;
        currentLineIndex++;
        if (currentLineIndex < tutorialLines.Length) tutorialText.text = tutorialLines[currentLineIndex];
        else
        {
            active = false;
            onboardingCanvas.interactable = false;
            StartCoroutine(FadeCanvas(0));
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

    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        StopAllCoroutines();
        active = fading = false;
        if (onboardingCanvas != null)
        {
            onboardingCanvas.alpha = 0;
            onboardingCanvas.blocksRaycasts = onboardingCanvas.interactable = false;
            if (flow != null) flow.EndDialogue(onboardingCanvas);
        }
    }
}
