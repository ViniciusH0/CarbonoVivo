using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    public InputMode currentMode = InputMode.None;
    public GameObject treePrefab;
    private TreeData selectedTree;


    private void Awake()
    {
        Instance = this;
    }

    public void EnterPlantingMode(TreeData treeData)
    {
        selectedTree = treeData;
        currentMode = InputMode.Planting;

    }
   
    public void TryPlant(Transform spot)
    {
        if (currentMode != InputMode.Planting || selectedTree == null) return;
      
        
        GameObject tree = Instantiate(treePrefab, spot.position, Quaternion.identity, spot);
        tree.GetComponent<TreeInstance>().Setup(selectedTree);
        spot.GetComponent<Collider2D>().enabled = false; 
        ExitPlantingMode();


    }

    public void ExitPlantingMode()
    {
        selectedTree = null;
        currentMode = InputMode.Interacting;
    }


    public bool IsPlanting()
    {
        return currentMode == InputMode.Planting;
    }
}
