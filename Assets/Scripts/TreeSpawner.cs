using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TreeSpawner : MonoBehaviour
{
    public static TreeSpawner Instance { get; private set; }

    public IndicatorManager indicatorManager; // Link no Inspector
    public GameObject[] treePrefabs; 
    public MeshRenderer plane;
    public Collider[] spawnBlockers;
    public TMP_FontAsset pixelFont;
    
    [Header("Multiplier Limits")]
    public float minMultiplier = 0.5f; 
    public float maxMultiplier = 50.0f; 

    [Min(0.01f)] public float spawnInterval = 0.5f;

    [Header("Scale Settings")]
    public float minScaleMultiplier = 2.5f;
    public float maxScaleMultiplier = 3.0f;

    [Header("Rotation Settings")]
    [Tooltip("Ex: Se a árvore nasce deitada em 90 no X, coloque -90 aqui para corrigir.")]
    public Vector3 rotationOffset = new Vector3(-90, 0, 0); 

    private List<GameObject> spawnedTrees = new List<GameObject>();
    private float timer = 0f;
    private int manualPlantedCount = 0;

    public IReadOnlyList<GameObject> SpawnedTrees => spawnedTrees;

    private void Awake()
    {
        Instance = this;
        if (pixelFont == null || pixelFont.name.Contains("BoldPixels"))
        {
            pixelFont = TMPro.TMP_Settings.defaultFontAsset;
        }
    }

    void Update()
    {
        if (treePrefabs == null || treePrefabs.Length == 0 || indicatorManager == null) return;

        // Conecta o health (0 a 10) aos limites do multiplier, respeitando as mudas manuais
        float currentMultiplier = Mathf.Lerp(minMultiplier, maxMultiplier, indicatorManager.forestHealth / 10f);
        int targetCount = Mathf.Max(Mathf.RoundToInt(currentMultiplier * 10), manualPlantedCount);

        if (spawnedTrees.Count != targetCount)
        {
            timer += Time.deltaTime;
            
            if (timer >= spawnInterval)
            {
                timer = 0f;
                
                if (spawnedTrees.Count < targetCount)
                    SpawnTree();
                else if (spawnedTrees.Count > targetCount && spawnedTrees.Count > manualPlantedCount)
                    RemoveTree();
            }
        }
        else
        {
            timer = 0f;
        }
    }

    public GameObject ForceSpawnSingleTree(bool isManual = false)
    {
        Vector3 spawnPos = isManual ? GetVisiblePositionInCamera() : GetRandomPositionOnPlane();
        int maxAttempts = 30;
        int attempts = 0;

        while (IsInsideAnyBlocker(spawnPos) && attempts < maxAttempts)
        {
            spawnPos = isManual ? GetVisiblePositionInCamera() : GetRandomPositionOnPlane();
            attempts++;
        }

        if (attempts < maxAttempts)
        {
            if (isManual) manualPlantedCount++;
            return CreateTreeAtPosition(spawnPos, true);
        }

        return null;
    }

    public GameObject ForceSpawnAt(Vector3 spawnPos, bool isManual = true)
    {
        if (IsInsideAnyBlocker(spawnPos)) return null;
        if (isManual) manualPlantedCount++;
        return CreateTreeAtPosition(spawnPos, true);
    }

    private void SpawnTree()
    {
        Vector3 spawnPos;
        int maxAttempts = 30;
        int attempts = 0;

        do
        {
            spawnPos = GetRandomPositionOnPlane();
            attempts++;
        } 
        while (IsInsideAnyBlocker(spawnPos) && attempts < maxAttempts);

        if (attempts < maxAttempts)
        {
            CreateTreeAtPosition(spawnPos, false);
        }
    }

    private GameObject CreateTreeAtPosition(Vector3 spawnPos, bool withEffect)
    {
        if (treePrefabs == null || treePrefabs.Length == 0) return null;

        GameObject prefabToSpawn = treePrefabs[Random.Range(0, treePrefabs.Length)];
        Quaternion finalRotation = Quaternion.Euler(rotationOffset);
        GameObject newTree = Instantiate(prefabToSpawn, spawnPos, finalRotation, transform);

        float randomScale = Random.Range(minScaleMultiplier, maxScaleMultiplier);
        Vector3 targetScale = Vector3.one * randomScale;

        // Adiciona colisor e script de interação para permitir cliques no mundo 3D
        var col = newTree.GetComponent<Collider>();
        if (col == null)
        {
            var box = newTree.AddComponent<BoxCollider>();
            box.size = new Vector3(1.2f, 2.5f, 1.2f);
            box.center = new Vector3(0, 1.2f, 0);
        }

        if (newTree.GetComponent<TreeClickable>() == null)
        {
            newTree.AddComponent<TreeClickable>();
        }

        spawnedTrees.Add(newTree);

        if (withEffect)
        {
            newTree.transform.localScale = Vector3.zero;
            StartCoroutine(GrowTreeRoutine(newTree, targetScale));
            SproutVisualEffect.CreateAt(spawnPos, pixelFont);
        }
        else
        {
            newTree.transform.localScale = targetScale;
        }

        return newTree;
    }

    private IEnumerator GrowTreeRoutine(GameObject tree, Vector3 targetScale)
    {
        float duration = 0.55f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (tree == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Curva com overshoot (efeito mola/elástico de broto crescendo com vigor)
            float c4 = (2f * Mathf.PI) / 3f;
            float bounce = t == 0 ? 0 : t == 1 ? 1 : Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1;

            tree.transform.localScale = targetScale * Mathf.Clamp(bounce, 0f, 1.2f);
            yield return null;
        }

        if (tree != null) tree.transform.localScale = targetScale;
    }

    public Vector3 GetVisiblePositionInCamera()
    {
        Camera cam = Camera.main;
        float planeY = plane != null ? plane.transform.position.y : 0f;

        if (cam != null)
        {
            // Pega um ponto projetado à frente da câmera no plano
            Ray ray = cam.ViewportPointToRay(new Vector3(Random.Range(0.35f, 0.65f), Random.Range(0.35f, 0.65f), 0));
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, planeY, 0));
            float enter;
            if (groundPlane.Raycast(ray, out enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                hitPoint += new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
                hitPoint.y = planeY;
                if (!IsInsideAnyBlocker(hitPoint)) return hitPoint;
            }
        }

        return GetRandomPositionOnPlane();
    }

    public void RemoveTree()
    {
        if (spawnedTrees.Count == 0) return;

        int lastIndex = spawnedTrees.Count - 1;
        if (spawnedTrees[lastIndex] != null)
        {
            Destroy(spawnedTrees[lastIndex]);
        }
        spawnedTrees.RemoveAt(lastIndex);
        if (manualPlantedCount > 0) manualPlantedCount--;
    }

    public void RemoveSpecificTree(GameObject tree)
    {
        if (tree == null) return;
        spawnedTrees.Remove(tree);
        Destroy(tree);
        if (manualPlantedCount > 0) manualPlantedCount--;
    }

    public GameObject GetRandomTree()
    {
        spawnedTrees.RemoveAll(t => t == null);
        if (spawnedTrees.Count == 0) return null;
        return spawnedTrees[Random.Range(0, spawnedTrees.Count)];
    }

    public Vector3 GetRandomPositionOnPlane()
    {
        if (plane == null) return transform.position;

        Bounds bounds = plane.bounds;
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, plane.transform.position.y, randomZ); 
    }

    public bool IsInsideAnyBlocker(Vector3 pos) 
    {
        if (spawnBlockers == null || spawnBlockers.Length == 0) return false;

        foreach (Collider blocker in spawnBlockers)
        {
            if (blocker == null) continue;
            
            Vector3 checkPos = pos;
            checkPos.y = blocker.bounds.center.y;
            
            if (blocker.bounds.Contains(checkPos))
            {
                return true; 
            }
        }
        
        return false;
    }

    public int GetTreeCount()
    {
        spawnedTrees.RemoveAll(t => t == null);
        return spawnedTrees.Count;
    }
}
