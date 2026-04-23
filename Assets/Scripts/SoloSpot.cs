using UnityEngine;

public class SoloSpot : MonoBehaviour, IClickable
{
    private bool isOccupied = false;
    public GameObject treePrefab;

    public void OnClicked()
    {
        if (isOccupied) return;

       GridManager.Instance.TryPlant(transform);
    }
}
