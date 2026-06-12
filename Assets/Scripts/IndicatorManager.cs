using UnityEngine;
using TMPro;

public class IndicatorManager : MonoBehaviour
{
    [Header("Values (0 to 10)")]
    [Range(0, 10)] public float forestHealth = 0f; // Começa em 0 (0%)
    [Range(0, 10)] public float water = 1f;
    [Range(0, 10)] public float community = 1f;
    [Range(0, 10)] public float resources = 1f;

    [Header("UI Text References")]
    public TextMeshProUGUI forestHealthText;
    public TextMeshProUGUI waterText;
    public TextMeshProUGUI communityText;
    public TextMeshProUGUI resourcesText;

    void Start()
    {
        UpdateUI();
    }

    public void ModifyForestHealth(float amount)
    {
        forestHealth = Mathf.Clamp(forestHealth + amount, 0f, 10f);
        UpdateUI();
    }

    public void ModifyWater(float amount)
    {
        water = Mathf.Clamp(water + amount, 0f, 10f);
        UpdateUI();
    }

    public void ModifyCommunity(float amount)
    {
        community = Mathf.Clamp(community + amount, 0f, 10f);
        UpdateUI();
    }

    public void ModifyResources(float amount)
    {
        resources = Mathf.Clamp(resources + amount, 0f, 10f);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (forestHealthText) forestHealthText.text = $"{Mathf.RoundToInt((forestHealth / 10f) * 100f)}%";
        if (waterText) waterText.text = $"{Mathf.RoundToInt((water / 10f) * 100f)}%";
        if (communityText) communityText.text = $"{Mathf.RoundToInt((community / 10f) * 100f)}%";
        if (resourcesText) resourcesText.text = $"{Mathf.RoundToInt((resources / 10f) * 100f)}%";
    }
}