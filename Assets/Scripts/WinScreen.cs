using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class WinScreen : MonoBehaviour
{
    public static WinScreen Instance { get; private set; }

    public CanvasGroup winCanvas;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;
    public float fadeSpeed = 2f;
    
    public bool IsGameWon { get; private set; } = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (winCanvas == null)
        {
            winCanvas = GetComponent<CanvasGroup>();
        }

        if (winCanvas != null)
        {
            winCanvas.alpha = 0f;
            winCanvas.blocksRaycasts = false;
            winCanvas.interactable = false;
        }

        FindAndBindButtons();
        EnsureCanvasSetup();
    }

    private void FindAndBindButtons()
    {
        if (restartButton == null) restartButton = transform.Find("WinButtons/PlayAgainBtn")?.GetComponent<Button>();
        if (menuButton == null) menuButton = transform.Find("WinButtons/MenuBtn")?.GetComponent<Button>();

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
            restartButton.onClick.AddListener(RestartGame);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(ReturnToMainMenu);
            menuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    public void EnsureCanvasSetup()
    {
        var rootCanvas = GetComponentInParent<Canvas>();
        var c = GetComponent<Canvas>();
        if (c == null) c = gameObject.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 300; // Fica acima de Indicators (210), Question (200) e Blocker (190)

        if (rootCanvas != null)
        {
            c.additionalShaderChannels = rootCanvas.additionalShaderChannels;
        }

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    public void Show()
    {
        if (IsGameWon) return;
        
        IsGameWon = true;
        transform.SetAsLastSibling();
        EnsureCanvasSetup();
        FindAndBindButtons();

        // Esconde indicadores da floresta e outros widgets do jogo para não sobreporem o menu final
        HideGameHUD();

        var flow = StepController.FindFor(this);
        if (flow != null) flow.SetResultScreen();

        if (winCanvas != null)
        {
            winCanvas.blocksRaycasts = true;
            winCanvas.interactable = true;
            StartCoroutine(FadeCanvas(1f));
        }
    }

    private void HideGameHUD()
    {
        var ind = GameObject.Find("UI_Canvas/Game/Indicators");
        if (ind != null) ind.SetActive(false);

        var timeClock = GameObject.Find("UI_Canvas/Game/TimeClockWidget");
        if (timeClock != null) timeClock.SetActive(false);

        var debugBtn = GameObject.Find("UI_Canvas/DebugPanel_TriggerButton");
        if (debugBtn != null) debugBtn.SetActive(false);

        var pauseBtn = GameObject.Find("UI_Canvas/PauseButton");
        if (pauseBtn != null) pauseBtn.SetActive(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        var flow = StepController.FindFor(this);
        if (flow != null)
        {
            flow.RestartGame();
        }
        else
        {
            SceneManager.LoadScene(gameObject.scene.name);
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        var flow = StepController.FindFor(this);
        if (flow != null)
        {
            flow.ReturnToMainMenu();
        }
        else
        {
            SceneManager.LoadScene(gameObject.scene.name);
        }
    }

    private IEnumerator FadeCanvas(float targetAlpha)
    {
        while (winCanvas != null && Mathf.Abs(winCanvas.alpha - targetAlpha) > 0.01f)
        {
            winCanvas.alpha = Mathf.Lerp(winCanvas.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
            yield return null;
        }
        if (winCanvas != null) winCanvas.alpha = targetAlpha;
    }
}
