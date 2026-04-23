using UnityEngine;

public class StorePopUp : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    public static StorePopUp Instance;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        Instance = this;
        Hide();
    }
    public void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
