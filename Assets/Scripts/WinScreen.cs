using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class WinScreen : MonoBehaviour
{
    public static WinScreen Instance { get; private set; }

    public CanvasGroup winCanvas;
    public float fadeSpeed = 2f;
    
    public bool IsGameWon { get; private set; } = false;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (winCanvas != null)
        {
            winCanvas.alpha = 0f;
            winCanvas.blocksRaycasts = false;
            winCanvas.interactable = false;
        }
    }

    public void Show()
    {
        if (IsGameWon) return;
        
        IsGameWon = true;
        if (winCanvas != null)
        {
            winCanvas.blocksRaycasts = true;
            winCanvas.interactable = true;
            StartCoroutine(FadeCanvas(1f));
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        var stepController = UnityEngine.Object.FindAnyObjectByType<StepController>();
        if (stepController != null)
        {
            if (winCanvas != null)
            {
                winCanvas.alpha = 0f;
                winCanvas.blocksRaycasts = false;
                winCanvas.interactable = false;
            }
            IsGameWon = false;
            stepController.ShowStep(0);
        }
        else
        {
            RestartGame();
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
