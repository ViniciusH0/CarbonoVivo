using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverScreen : MonoBehaviour
{
    public static GameOverScreen Instance { get; private set; }

    [SerializeField] private CanvasGroup gameOverCanvas;
    [SerializeField] private TextMeshProUGUI reasonText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private float fadeSpeed = 3f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (gameOverCanvas == null)
        {
            gameOverCanvas = GetComponent<CanvasGroup>();
        }

        if (gameOverCanvas != null)
        {
            gameOverCanvas.alpha = 0f;
            gameOverCanvas.blocksRaycasts = false;
            gameOverCanvas.interactable = false;
        }

        FindAndBindButtons();
        EnsureCanvasSetup();
    }

    private void FindAndBindButtons()
    {
        if (restartButton == null) restartButton = transform.Find("Box/BtnRetry")?.GetComponent<Button>();
        if (menuButton == null) menuButton = transform.Find("Box/BtnMenu")?.GetComponent<Button>();

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

    public void Show(string reason)
    {
        transform.SetAsLastSibling();
        EnsureCanvasSetup();
        FindAndBindButtons();

        // Esconde indicadores da floresta e outros widgets do jogo para não sobreporem o menu final
        HideGameHUD();

        var flow = StepController.FindFor(this);
        if (flow != null) flow.SetResultScreen();

        if (reasonText != null)
        {
            reasonText.text = reason;
        }

        if (gameOverCanvas != null)
        {
            gameOverCanvas.blocksRaycasts = true;
            gameOverCanvas.interactable = true;
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
        while (gameOverCanvas != null && Mathf.Abs(gameOverCanvas.alpha - targetAlpha) > 0.01f)
        {
            gameOverCanvas.alpha = Mathf.Lerp(gameOverCanvas.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
            yield return null;
        }
        if (gameOverCanvas != null) gameOverCanvas.alpha = targetAlpha;
    }
}
