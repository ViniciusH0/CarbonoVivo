using System.Collections;
using UnityEngine;

public class TreeClickable : MonoBehaviour
{
    private Vector3 originalScale;
    private bool isAnimating = false;
    public bool IsGrowing { get; set; } = false;

    public void SetTargetScale(Vector3 scale)
    {
        originalScale = scale;
    }

    private void Start()
    {
        if (originalScale.sqrMagnitude < 0.001f)
        {
            originalScale = transform.localScale;
        }
    }

    public void OnPlayerTapped()
    {
        if (StepController.WorldInputBlocked) return;

        // Se a árvore estiver pegando fogo, o clique ajuda a apagar o incêndio!
        var burning = GetComponent<BurningTreeEffect>();
        if (burning != null && burning.IsBurning)
        {
            burning.OnPlayerExtinguishClick();
            return;
        }

        if (isAnimating) return;

        // Bonificação ativa por toque (Loop de Idle/Clicker Zen)
        float bonus = 1.0f;
        if (EcologySimulation.Instance == null && BiodiversityManager.Instance != null && BiodiversityManager.Instance.IsUnlocked("guapuruvu"))
        {
            bonus += 1.0f;
        }

        if (CarbonManager1.Instance != null)
        {
            CarbonManager1.Instance.AddCarbon(bonus);
        }

        // Animação de pulsar/balançar (apenas se já terminou o ciclo de crescimento de 10s)
        if (!IsGrowing)
        {
            StartCoroutine(BounceRoutine());
        }

        // Efeito sonoro / feedback
        if (BioacousticAudioManager.Instance != null)
        {
            BioacousticAudioManager.Instance.PlayTreeClickSound();
        }
    }

    private IEnumerator BounceRoutine()
    {
        isAnimating = true;
        Vector3 baseScale = originalScale.sqrMagnitude > 0.01f ? originalScale : transform.localScale;
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (IsGrowing) { isAnimating = false; yield break; }
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float wave = Mathf.Sin(t * Mathf.PI);
            transform.localScale = baseScale * (1f + wave * 0.18f);
            yield return null;
        }

        transform.localScale = baseScale;
        isAnimating = false;
    }
}

