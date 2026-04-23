using UnityEngine;
[CreateAssetMenu(fileName = "New Tree Status", menuName = "Tree Status")]
public class TreeData : ScriptableObject
{
    public int treeID;
    public string treeName;

    public Sprite sownSprite;
    public Sprite saplingSprite;
    public Sprite matureSprite;

    public int health = 100;
    public float regenerationHealth = 1f; 

    public float co2PerSecond = 1f;
    public float co2PerClick = 5f;

    public int cost = 10;


}
