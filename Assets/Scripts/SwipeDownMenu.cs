using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SwipeDownMenu : MonoBehaviour
{
    public static SwipeDownMenu Instance { get; private set; }

    [Header("Menu Canvas")]
    [SerializeField] private CanvasGroup pauseMenuCanvas;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button instructionsButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button returnMenuButton;
    [SerializeField] private Button hudPauseButton;

    [Header("Swipe Settings")]
    [SerializeField] private float minSwipeDistance = 80f;

    private Vector2 touchStartPos;
    private bool isTrackingSwipe = false;
    private bool isPaused = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.alpha = 0f;
            pauseMenuCanvas.blocksRaycasts = false;
            pauseMenuCanvas.interactable = false;
        }

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (instructionsButton != null) instructionsButton.onClick.AddListener(ShowInstructions);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (returnMenuButton != null) returnMenuButton.onClick.AddListener(ReturnToMenu);

        if (hudPauseButton != null)
        {
            hudPauseButton.onClick.AddListener(TogglePauseMenu);
        }
    }

    private void Update()
    {
        var flow = StepController.FindFor(this);
        if (flow != null && (!flow.IsGameplayActive || flow.HasDialogue)) return;
        // Gesto de deslizar para baixo (Swipe Down) a partir do topo da tela
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                // Inicia no terço superior da tela
                if (touch.position.y > Screen.height * 0.75f)
                {
                    touchStartPos = touch.position;
                    isTrackingSwipe = true;
                }
            }
            else if (touch.phase == TouchPhase.Ended && isTrackingSwipe)
            {
                isTrackingSwipe = false;
                float deltaY = touchStartPos.y - touch.position.y; // Deslizar para baixo
                if (deltaY > minSwipeDistance)
                {
                    TogglePauseMenu();
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }

    public void TogglePauseMenu()
    {
        var flow = StepController.FindFor(this);
        if (flow != null && (!flow.IsGameplayActive || flow.HasDialogue)) return;
        if (isPaused) Resume();
        else OpenPauseMenu();
    }

    public void OpenPauseMenu()
    {
        isPaused = true;
        var flow = StepController.FindFor(this);
        if (flow != null) flow.SetPauseState(true);
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.alpha = 1f;
            pauseMenuCanvas.blocksRaycasts = true;
            pauseMenuCanvas.interactable = true;
        }
    }

    public void Resume()
    {
        isPaused = false;
        var flow = StepController.FindFor(this);
        if (flow != null) flow.SetPauseState(false);
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.alpha = 0f;
            pauseMenuCanvas.blocksRaycasts = false;
            pauseMenuCanvas.interactable = false;
        }
    }

    public void ShowInstructions()
    {
        Resume();
        var stepController = StepController.FindFor(this);
        if (stepController != null)
        {
            stepController.ShowStep(1); // 1 = Instruções
        }
    }

    public void RestartGame()
    {
        var flow = StepController.FindFor(this);
        if (flow != null) flow.RestartGame();
    }

    public void ReturnToMenu()
    {
        var flow = StepController.FindFor(this);
        if (flow != null) flow.ReturnToMainMenu();
    }
}
