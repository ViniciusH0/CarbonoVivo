using UnityEngine;

[CreateAssetMenu(fileName = "NewQuestion", menuName = "Game/Question Data")]
public class QuestionData : ScriptableObject
{
    [TextArea(3, 5)]
    public string questionText;

    [Header("Yes Consequences")]
    public float yesForestHealth;
    public float yesWater;
    public float yesCommunity;
    public float yesResources;

    [Header("No Consequences")]
    public float noForestHealth;
    public float noWater;
    public float noCommunity;
    public float noResources;
}