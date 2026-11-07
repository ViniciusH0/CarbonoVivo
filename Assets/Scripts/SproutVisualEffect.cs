using System.Collections;
using UnityEngine;
using TMPro;

public class SproutVisualEffect : MonoBehaviour
{
    public static void CreateAt(Vector3 position, TMP_FontAsset font)
    {
        GameObject fxGo = new GameObject("SproutEffect");
        fxGo.transform.position = position;
        var script = fxGo.AddComponent<SproutVisualEffect>();
        script.StartCoroutine(script.PlayRoutine(position, font));
    }

    private IEnumerator PlayRoutine(Vector3 position, TMP_FontAsset font)
    {
        // 1. Cria anel de luz/raiz verde no chão que expande
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "GrowthRing";
        ring.transform.position = position + new Vector3(0, 0.05f, 0);
        ring.transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);

        var ringCol = ring.GetComponent<Collider>();
        if (ringCol != null) Destroy(ringCol);

        var rend = ring.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
        Color startColor = new Color(0.3f, 0.95f, 0.45f, 0.85f);
        mat.color = startColor;
        rend.material = mat;

        // 2. Cria texto flutuante em World Space ("+1 Muda Plantada!")
        GameObject textObj = new GameObject("FloatingText");
        textObj.transform.SetParent(transform, true);
        textObj.transform.position = position + Vector3.up * 1.2f;
        textObj.transform.rotation = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity;

        var tmp = textObj.AddComponent<TextMeshPro>();
        tmp.font = (font != null && !font.name.Contains("BoldPixels")) ? font : TMPro.TMP_Settings.defaultFontAsset;
        tmp.text = "<b>+1 Muda Plantada!</b>";
        tmp.fontSize = 5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.4f, 1f, 0.5f, 1f);

        // 3. Animação de expansão do anel e elevação do texto
        float duration = 1.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Expande o anel de 0.1 a 3.2 e reduz o alpha
            float ringScale = Mathf.Lerp(0.2f, 3.2f, Mathf.Sqrt(t));
            if (ring != null)
            {
                ring.transform.localScale = new Vector3(ringScale, 0.02f, ringScale);
                mat.color = new Color(startColor.r, startColor.g, startColor.b, (1f - t) * 0.85f);
            }

            // Eleva o texto e reduz alpha
            if (textObj != null)
            {
                textObj.transform.position += Vector3.up * Time.deltaTime * 1.5f;
                if (Camera.main != null) textObj.transform.rotation = Camera.main.transform.rotation;
                tmp.color = new Color(0.4f, 1f, 0.5f, 1f - (t * t));
            }

            yield return null;
        }

        if (ring != null) Destroy(ring);
        if (textObj != null) Destroy(textObj);
        Destroy(gameObject);
    }
}

