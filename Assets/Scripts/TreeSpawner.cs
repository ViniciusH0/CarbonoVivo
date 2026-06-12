using System.Collections.Generic;
using UnityEngine;

public class TreeSpawner : MonoBehaviour
{
    public IndicatorManager indicatorManager; // Link no Inspector
    public GameObject[] treePrefabs; 
    public MeshRenderer plane;
    public Collider[] spawnBlockers;
    
    [Header("Multiplier Limits")]
    public float minMultiplier = 0.5f; 
    public float maxMultiplier = 5.0f; 

    [Min(0.01f)] public float spawnInterval = 0.2f;

    [Header("Scale Settings")]
    public float minScaleMultiplier = 0.8f;
    public float maxScaleMultiplier = 1.5f;

    [Header("Rotation Settings")]
    [Tooltip("Ex: Se a árvore nasce deitada em 90 no X, coloque -90 aqui para corrigir.")]
    public Vector3 rotationOffset; 

    private List<GameObject> spawnedTrees = new List<GameObject>();
    private float timer = 0f;

    void Update()
    {
        if (treePrefabs == null || treePrefabs.Length == 0 || indicatorManager == null) return;

        // Conecta o health (0 a 10) aos limites do multiplier
        float currentMultiplier = Mathf.Lerp(minMultiplier, maxMultiplier, indicatorManager.forestHealth / 10f);
        int targetCount = Mathf.RoundToInt(currentMultiplier * 10);

        if (spawnedTrees.Count != targetCount)
        {
            timer += Time.deltaTime;
            
            if (timer >= spawnInterval)
            {
                timer = 0f;
                
                if (spawnedTrees.Count < targetCount)
                    SpawnTree();
                else if (spawnedTrees.Count > targetCount)
                    RemoveTree();
            }
        }
        else
        {
            timer = 0f;
        }
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
            GameObject prefabToSpawn = treePrefabs[Random.Range(0, treePrefabs.Length)];
            
            Quaternion finalRotation = Quaternion.Euler(rotationOffset);
            GameObject newTree = Instantiate(prefabToSpawn, spawnPos, finalRotation, transform);
            
            float randomScale = Random.Range(minScaleMultiplier, maxScaleMultiplier);
            newTree.transform.localScale = Vector3.one * randomScale;
            
            spawnedTrees.Add(newTree);
        }
    }

    private void RemoveTree()
    {
        if (spawnedTrees.Count == 0) return;

        int lastIndex = spawnedTrees.Count - 1;
        Destroy(spawnedTrees[lastIndex]);
        spawnedTrees.RemoveAt(lastIndex);
    }

    private Vector3 GetRandomPositionOnPlane()
    {
        if (plane == null) return transform.position;

        Bounds bounds = plane.bounds;
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, plane.transform.position.y, randomZ); 
    }

    private bool IsInsideAnyBlocker(Vector3 pos) 
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
}