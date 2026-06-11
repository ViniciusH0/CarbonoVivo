using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StepController : MonoBehaviour
{
    [System.Serializable]
    public class Step
    {
        public string name;
        public CanvasGroup canvasGroup;
    }

    [Header("Steps")]
    [SerializeField] private List<Step> steps = new List<Step>();

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.3f;

    private Coroutine currentRoutine;
    public void Start()
    {
        ShowStep(0);
    }
    public void ShowStep(int id)
    {
        if (id < 0 || id >= steps.Count) return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(SwitchStepRoutine(id));
    }

    private IEnumerator SwitchStepRoutine(int targetId)
    {
        // fade out todos
        for (int i = 0; i < steps.Count; i++)
        {
            SetInteractable(steps[i].canvasGroup, false);
            yield return StartCoroutine(Fade(steps[i].canvasGroup, 0f));
        }

        // fade in target
        CanvasGroup target = steps[targetId].canvasGroup;
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
            time += Time.deltaTime;
            float t = time / fadeDuration;

            cg.alpha = Mathf.Lerp(start, targetAlpha, t);
            yield return null;
        }

        cg.alpha = targetAlpha;
    }

    public void StartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
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