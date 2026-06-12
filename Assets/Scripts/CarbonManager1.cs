using UnityEngine;
using TMPro; 

public class CarbonManager1 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TreeSpawner treeSpawner;
    [SerializeField] private TextMeshProUGUI carbonText;
    [SerializeField] private WinScreen winScreen; 

    [Header("Carbon Settings")]
    [SerializeField] private float maxCarbon = 1000f;
    [SerializeField] private float generationPower = 0.5f; 

    [Header("Current Status")]
    [SerializeField] private float currentCarbon = 0f;

    public float CurrentCarbon => currentCarbon;

    void Update()
    {
        if (treeSpawner == null || (winScreen != null && winScreen.IsGameWon)) return;

        int treeCount = treeSpawner.GetTreeCount();

        currentCarbon += treeCount * generationPower * Time.deltaTime;
        currentCarbon = Mathf.Clamp(currentCarbon, 0f, maxCarbon);

        if (carbonText != null)
        {
            carbonText.text = Mathf.RoundToInt(currentCarbon).ToString();
        }

        // Checa condição de vitória
        if (currentCarbon >= maxCarbon && winScreen != null)
        {
            winScreen.Show();
        }
    }
}