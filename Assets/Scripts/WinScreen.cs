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
        transform.SetAsLastSibling();
        var flow = StepController.FindFor(this);
        if (flow != null) flow.SetResultScreen();
        if (winCanvas != null)
        {
            winCanvas.blocksRaycasts = true;
            winCanvas.interactable = true;
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
        while (winCanvas != null && Mathf.Abs(winCanvas.alpha - targetAlpha) > 0.01f)
        {
            winCanvas.alpha = Mathf.Lerp(winCanvas.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
            yield return null;
        }
        if (winCanvas != null) winCanvas.alpha = targetAlpha;
    }
}
