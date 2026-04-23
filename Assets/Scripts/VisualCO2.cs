using TMPro;
using UnityEngine;

public class VisualCO2 : MonoBehaviour
{
    public TextMeshProUGUI co2Text;
    public float updateInterval = 0.2f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= updateInterval)
        {
            timer = 0f;
            co2Text.text = $"CO2: {Co2.co2Amount:F2}";
        }
    }
}