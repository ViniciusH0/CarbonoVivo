using UnityEngine;
using System.Collections;

public class WinScreen : MonoBehaviour
{
    public CanvasGroup winCanvas;
    public float fadeSpeed = 2f;
    
    public bool IsGameWon { get; private set; } = false;

    void Start()
    {
        winCanvas.alpha = 0f;
        winCanvas.blocksRaycasts = false;
        winCanvas.interactable = false;
    }

    public void Show()
    {
        if (IsGameWon) return;
        
        IsGameWon = true;
        winCanvas.blocksRaycasts = true;
        winCanvas.interactable = true;
        
        StartCoroutine(FadeCanvas(1f));
    }

    private IEnumerator FadeCanvas(float targetAlpha)
    {
        while (Mathf.Abs(winCanvas.alpha - targetAlpha) > 0.01f)
        {
            winCanvas.alpha = Mathf.Lerp(winCanvas.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            yield return null;
        }
        winCanvas.alpha = targetAlpha;
    }
}