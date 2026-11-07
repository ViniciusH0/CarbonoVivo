using System.Collections;
using UnityEngine;

public class TreeClickable : MonoBehaviour
{
    private Vector3 originalScale;
    private bool isAnimating = false;

    private void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPlayerTapped()
    {
        if (isAnimating) return;

        // Bonificação ativa por toque (Loop de Idle/Clicker Zen)
        float bonus = 1.0f;
        if (BiodiversityManager.Instance != null && BiodiversityManager.Instance.IsUnlocked("guapuruvu"))
        {
            bonus += 1.0f;
        }

        if (CarbonManager1.Instance != null)
        {
            CarbonManager1.Instance.AddCarbon(bonus);
        }

        // Animação de pulsar/balançar
        StartCoroutine(BounceRoutine());

        // Efeito sonoro / feedback
        if (BioacousticAudioManager.Instance != null)
        {
            BioacousticAudioManager.Instance.PlayTreeClickSound();
        }
    }

    private IEnumerator BounceRoutine()
    {
        isAnimating = true;
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float wave = Mathf.Sin(t * Mathf.PI);
            transform.localScale = originalScale * (1f + wave * 0.18f);
            yield return null;
        }

        transform.localScale = originalScale;
        isAnimating = false;
    }
}

