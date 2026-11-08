using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StepController : MonoBehaviour
{
    [SerializeField] private List<CanvasGroup> steps = new List<CanvasGroup>();
    [SerializeField] private Button[] returnToMenuButtons;
    [SerializeField] private int mainMenuStepId;
    [SerializeField] private float fadeDuration = .3f;
    private readonly HashSet<CanvasGroup> dialogues = new HashSet<CanvasGroup>();
    private Coroutine currentRoutine;
    private int currentStepId = -1;
    private int gameplayStepId = -1;
    private int instructionsReturnStep;
    private bool transitioning;
    private bool manuallyPaused;
    private bool resultShown;
    private GameObject inputBlocker;
    private EventSystem eventSystem;
    private bool navigationWasEnabled;
    private Image backdrop;
    private Color backdropColor;
    private CanvasGroup pauseButtonGroup;
    private static string reloadPath;
    private static bool playAfterReload;

    public static bool WorldInputBlocked => Time.timeScale <= 0;
    public bool HasDialogue => dialogues.Count > 0;
    public bool IsGameplayActive => gameplayStepId >= 0 && currentStepId == gameplayStepId && !resultShown;

    public static StepController FindFor(Component owner)
    {
        var parent = owner.GetComponentInParent<StepController>();
        if (parent != null) return parent;
        StepController fallback = null;
        foreach (var root in owner.gameObject.scene.GetRootGameObjects())
        foreach (var flow in root.GetComponentsInChildren<StepController>(true))
        {
            if (fallback == null) fallback = flow;
            if (flow.steps.Exists(s => s != null && s.name == "Game")) return flow;
        }
        return fallback;
    }

    private void Awake()
    {
        gameplayStepId = steps.FindIndex(s => s != null && s.name == "Game");
        if (gameplayStepId >= 0) Time.timeScale = 0;
    }

    private IEnumerator Start()
    {
        var background = transform.Find("BG (1)");
        if (background != null)
        {
            backdrop = background.GetComponent<Image>();
            if (backdrop != null)
            {
                background.gameObject.SetActive(true);
                background.SetAsFirstSibling();
                var rect = (RectTransform)background;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                backdrop.enabled = true; backdrop.raycastTarget = false;
                backdropColor = backdrop.color; backdropColor.a = 1;
                backdrop.color = backdropColor;
            }
        }
        var pauseButton = transform.Find("PauseButton");
        if (pauseButton != null)
        {
            pauseButtonGroup = pauseButton.GetComponent<CanvasGroup>();
            if (pauseButtonGroup == null) pauseButtonGroup = pauseButton.gameObject.AddComponent<CanvasGroup>();
        }
        if (returnToMenuButtons != null)
        foreach (var button in returnToMenuButtons)
        {
            if (button == null) continue;
            bool instructions = steps.Count > 1 && steps[1] != null && button.transform.IsChildOf(steps[1].transform);
            if (instructions) button.onClick.AddListener(ReturnFromInstructions);
            else button.onClick.AddListener(() => ShowStep(mainMenuStepId));
        }
        bool restart = reloadPath == gameObject.scene.path && playAfterReload && gameplayStepId >= 0;
        if (reloadPath == gameObject.scene.path && gameplayStepId >= 0) reloadPath = null;
        ShowStep(restart ? gameplayStepId : mainMenuStepId);
        yield return null;
        if (restart)
        {
            var tutorial = steps[gameplayStepId].GetComponentInChildren<OnboardingManager>(true);
            if (tutorial != null) tutorial.StartOnboarding();
        }
    }

    private void LateUpdate()
    {
        if (backdrop != null && gameplayStepId >= 0)
        {
            var color = backdropColor;
            color.a *= 1 - steps[gameplayStepId].alpha;
            backdrop.color = color;
        }
        if (pauseButtonGroup != null)
        {
            bool visible = IsGameplayActive && !transitioning && !HasDialogue;
            pauseButtonGroup.alpha = visible ? 1 : 0;
            pauseButtonGroup.blocksRaycasts = pauseButtonGroup.interactable = visible;
        }
        ApplyPause();
    }

    public void ShowStep(int id)
    {
        if (id < 0 || id >= steps.Count || id == currentStepId) return;
        if (id == 1) instructionsReturnStep = IsGameplayActive ? gameplayStepId : mainMenuStepId;
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SwitchStepRoutine(id));
    }

    public void ReturnFromInstructions() => ShowStep(instructionsReturnStep);

    private IEnumerator SwitchStepRoutine(int targetId)
    {
        transitioning = true;
        ApplyPause();
        if (currentStepId >= 0 && currentStepId < steps.Count)
        {
            SetInteractable(steps[currentStepId], false);
            yield return Fade(steps[currentStepId], 0);
        }
        else foreach (var step in steps)
        {
            step.alpha = 0;
            SetInteractable(step, false);
        }
        currentStepId = targetId;
        yield return Fade(steps[targetId], 1);
        SetInteractable(steps[targetId], true);
        transitioning = false;
        ApplyPause();
    }

    private IEnumerator Fade(CanvasGroup group, float target)
    {
        float initial = group.alpha;
        for (float elapsed = 0; elapsed < fadeDuration; elapsed += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(initial, target, elapsed / fadeDuration);
            yield return null;
        }
        group.alpha = target;
    }

    public void BeginDialogue(CanvasGroup dialogue)
    {
        if (dialogue == null || !dialogues.Add(dialogue)) return;
        if (dialogues.Count == 1)
        {
            eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                navigationWasEnabled = eventSystem.sendNavigationEvents;
                eventSystem.SetSelectedGameObject(null);
                eventSystem.sendNavigationEvents = false;
            }
        }
        if (inputBlocker == null)
        {
            inputBlocker = new GameObject("DialogueInputBlocker", typeof(RectTransform), typeof(Canvas),
                typeof(GraphicRaycaster), typeof(Image));
            inputBlocker.transform.SetParent(transform, false);
            var rect = inputBlocker.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = inputBlocker.GetComponent<Image>();
            image.color = Color.clear; image.raycastTarget = true;
            var canvas = inputBlocker.GetComponent<Canvas>();
            canvas.overrideSorting = true; canvas.sortingOrder = 190;
        }
        inputBlocker.SetActive(true);
        var layer = dialogue.GetComponent<Canvas>();
        if (layer == null) layer = dialogue.gameObject.AddComponent<Canvas>();
        layer.overrideSorting = true; layer.sortingOrder = 200;
        var parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null) layer.additionalShaderChannels = parentCanvas.additionalShaderChannels;
        if (dialogue.GetComponent<GraphicRaycaster>() == null) dialogue.gameObject.AddComponent<GraphicRaycaster>();
        ApplyPause();
    }

    public void EndDialogue(CanvasGroup dialogue)
    {
        dialogues.Remove(dialogue);
        if (!HasDialogue)
        {
            if (inputBlocker != null) inputBlocker.SetActive(false);
            if (eventSystem != null) eventSystem.sendNavigationEvents = navigationWasEnabled;
            eventSystem = null;
        }
        ApplyPause();
    }

    public void SetPauseState(bool paused) { manuallyPaused = paused; ApplyPause(); }
    public void SetResultScreen() { resultShown = true; ApplyPause(); }

    private void ApplyPause()
    {
        if (gameplayStepId < 0) return;
        Time.timeScale = !IsGameplayActive || transitioning || manuallyPaused || HasDialogue ? 0 : 1;
    }

    public void RestartGame() => Reload(true);
    public void ReturnToMainMenu() => Reload(false);

    private void Reload(bool begin)
    {
        reloadPath = gameObject.scene.path;
        playAfterReload = begin;
        Time.timeScale = 1;
        SceneManager.LoadScene(gameObject.scene.name);
    }

    private void OnDestroy()
    {
        if (eventSystem != null) eventSystem.sendNavigationEvents = navigationWasEnabled;
        if (gameplayStepId >= 0) Time.timeScale = 1;
    }

    public void ExitGame() => Application.Quit();
    private static void SetInteractable(CanvasGroup group, bool value)
    { group.interactable = group.blocksRaycasts = value; }
}
