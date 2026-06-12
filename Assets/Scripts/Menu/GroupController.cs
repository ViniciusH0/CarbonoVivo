using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Necessário para a classe Button

public class StepController : MonoBehaviour
{
    [Header("Steps")]
    [SerializeField] private List<CanvasGroup> steps = new List<CanvasGroup>();

    [Header("Menu Navigation")]
    [SerializeField] private Button[] returnToMenuButtons; 
    [SerializeField] private int mainMenuStepId = 0; 

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.3f;

    private Coroutine currentRoutine;
    private int currentStepId = -1;

    public void Start()
    {
        // Vincula o evento de voltar ao menu a todos os botões da array
        foreach (Button btn in returnToMenuButtons)
        {
            if (btn != null)
                btn.onClick.AddListener(() => ShowStep(mainMenuStepId));
        }

        ShowStep(mainMenuStepId);
    }

    public void ShowStep(int id)
    {
        if (id < 0 || id >= steps.Count || id == currentStepId) return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(SwitchStepRoutine(id));
    }

    private IEnumerator SwitchStepRoutine(int targetId)
    {
        if (currentStepId >= 0 && currentStepId < steps.Count)
        {
            SetInteractable(steps[currentStepId], false);
            yield return StartCoroutine(Fade(steps[currentStepId], 0f));
        }
        else
        {
            foreach (var step in steps)
            {
                step.alpha = 0f;
                SetInteractable(step, false);
            }
        }

        currentStepId = targetId;

        CanvasGroup target = steps[targetId];
        yield return StartCoroutine(Fade(target, 1f));

        SetInteractable(target, true);
    }

    private IEnumerator Fade(CanvasGroup cg, float targetAlpha)
    {
        float start = cg.alpha;
        float time = 0f;

        cg.blocksRaycasts = false;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime; 
            float t = time / fadeDuration;

            cg.alpha = Mathf.Lerp(start, targetAlpha, t);
            yield return null;
        }

        cg.alpha = targetAlpha;
    }

    public void SetPauseState(bool isPaused)
    {
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    private void SetInteractable(CanvasGroup cg, bool value)
    {
        cg.interactable = value;
        cg.blocksRaycasts = value;
    }
}