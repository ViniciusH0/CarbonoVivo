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
        // As árvores só surgem quando o jogador clica para plantar uma árvore
        spawnedTrees.RemoveAll(t => t == null);
    }

    public bool TryFindValidSpawnPosition(bool visibleInCamera, out Vector3 validPos)
    {
        validPos = visibleInCamera ? GetVisiblePositionInCamera() : GetRandomPositionOnPlane();
        int maxAttempts = 30;
        int attempts = 0;

        while (IsInsideAnyBlocker(validPos) && attempts < maxAttempts)
        {
            validPos = visibleInCamera ? GetVisiblePositionInCamera() : GetRandomPositionOnPlane();
            attempts++;
        }

        return attempts < maxAttempts;
    }

    public GameObject SpawnTreeFromDrone(Vector3 spawnPos)
    {
        manualPlantedCount++;
        return CreateTreeAtPosition(spawnPos, true);
    }

    public GameObject ForceSpawnSingleTree(bool isManual = false)
    {
        Vector3 spawnPos;
        if (TryFindValidSpawnPosition(isManual, out spawnPos))
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

        var clickable = newTree.GetComponent<TreeClickable>();
        if (clickable == null)
        {
            clickable = newTree.AddComponent<TreeClickable>();
        }
        clickable.SetTargetScale(targetScale);

        spawnedTrees.Add(newTree);

        if (withEffect)
        {
            // Começa como uma pequena muda recém-plantada e cresce lentamente ao longo de 10 segundos
            newTree.transform.localScale = targetScale * 0.08f;
            StartCoroutine(GrowTreeRoutine(newTree, targetScale, clickable));
            SproutVisualEffect.CreateAt(spawnPos, pixelFont);
        }
        else
        {
            newTree.transform.localScale = targetScale;
        }

        return newTree;
    }

    private IEnumerator GrowTreeRoutine(GameObject tree, Vector3 targetScale, TreeClickable clickable)
    {
        if (clickable != null) clickable.IsGrowing = true;

        float duration = 10.0f; // Ciclo de 10 segundos até atingir o tamanho total
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (tree == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Crescimento orgânico progressivo de 8% até 100% da escala final ao longo de 10 segundos
            float smoothT = Mathf.SmoothStep(0.08f, 1.0f, t);
            tree.transform.localScale = targetScale * smoothT;
            yield return null;
        }

        if (tree != null) tree.transform.localScale = targetScale;
        if (clickable != null) clickable.IsGrowing = false;
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
