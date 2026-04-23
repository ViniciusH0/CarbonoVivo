using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class ShopButton : MonoBehaviour
{
    public int treeID;
    public TextMeshProUGUI treeName;
    public TextMeshProUGUI cost;
    public Image iconImage;

    private TreeData dataTree;

    void Start()
    {
        dataTree = TreeDataBase.Instance.GetTreeDataByID(treeID);
        if (dataTree != null)
        {
            treeName.text = dataTree.treeName;
            cost.text = dataTree.cost.ToString();
            iconImage.sprite = dataTree.matureSprite;
        }
    }

    public void BuyTree()
    {
        if(Co2.co2Amount >= dataTree.cost)
        {
            Co2.co2Amount -= dataTree.cost;
           StartCoroutine(EnterPlantingNextFrame());
            StorePopUp.Instance.Hide();
        }
    }

    IEnumerator EnterPlantingNextFrame()
    {
        yield return null; 
        GridManager.Instance.EnterPlantingMode(dataTree);
    }
}
