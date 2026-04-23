using System.Collections;
using UnityEngine;


public class TreeInstance : MonoBehaviour, IClickable
{
    public StateTree currentState = StateTree.sown;
    public TreeData status;
    private SpriteRenderer spriteRenderer;

    public void Setup(TreeData treeData)
    {
        status = treeData;
        ApplyStateVisual();
        StartCoroutine(PassiveClick());
    }
 
    public void OnClicked()
    {
        Co2.AddCo2(Co2PerSecond());
        Debug.Log("Tree clicked!");
    }

    IEnumerator PassiveClick()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            Co2.AddCo2(Co2PerSecond());
        }
    }
    float Co2PerSecond()
    {
        switch (currentState)
        {
            case StateTree.sown:
                return status.co2PerSecond * 0.25f;

            case StateTree.sapling:
                return status.co2PerSecond * 0.5f;

            case StateTree.mature:
                return status.co2PerSecond;

            default:
                return 0f;
        }
    }
    void ApplyStateVisual()
    {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    
            switch (currentState)
            {
                case StateTree.sown:
                    spriteRenderer.sprite = status.sownSprite;
                    break;
                case StateTree.sapling:
                    spriteRenderer.sprite = status.saplingSprite;
                    break;
                case StateTree.mature:
                    spriteRenderer.sprite = status.matureSprite;
                    break;
        }
    }
}

