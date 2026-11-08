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
        if (gameOverCanvas != null)
        {
            gameOverCanvas.alpha = 0f;
            gameOverCanvas.blocksRaycasts = false;
            gameOverCanvas.interactable = false;
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        if (menuButton != null)
        {
            menuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    public void Show(string reason)
    {
        transform.SetAsLastSibling();
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

    public void RestartGame()
    {
        var flow = StepController.FindFor(this);
        if (flow != null) flow.RestartGame();
    }

    public void ReturnToMainMenu()
    {
        var flow = StepController.FindFor(this);
        if (flow != null) flow.ReturnToMainMenu();
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
