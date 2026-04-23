using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class TreeDataBase : MonoBehaviour
{
    public static TreeDataBase Instance;
    public List<TreeData> allTrees;

    private void Awake()
    { 
        Instance = this;

    }

    public TreeData GetTreeDataByID(int id)
    {
        return allTrees.Find(tree => tree.treeID == id);
    }
}
