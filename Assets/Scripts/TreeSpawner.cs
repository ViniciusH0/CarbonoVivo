using System.Collections.Generic;
using UnityEngine;

public class TreeSpawner : MonoBehaviour
{
    public GameObject treePrefab;
    public MeshRenderer plane;
    public Collider obstacleCube; 
    
    [Min(0)] public float multiplier = 1f;
    [Min(0.01f)] public float spawnInterval = 0.2f;

    private List<GameObject> spawnedTrees = new List<GameObject>();
    private float timer = 0f;

    void Update()
    {
        int targetCount = Mathf.RoundToInt(multiplier * 10);

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
        while (IsInsideObstacle(spawnPos) && attempts < maxAttempts);

        if (attempts < maxAttempts)
        {
            GameObject newTree = Instantiate(treePrefab, spawnPos, Quaternion.identity, transform);
            spawnedTrees.Add(newTree);
        }
    }

    private void RemoveTree()
    {
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

    private bool IsInsideObstacle(Vector3 pos)
    {
        if (obstacleCube == null) return false;
        
        pos.y = obstacleCube.bounds.center.y;
        return obstacleCube.bounds.Contains(pos);
    }
}