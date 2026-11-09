using UnityEngine;

// Identity is captured when planting is ordered, including drone deliveries.
public class ForestTreeSpecies : MonoBehaviour
{
    public string speciesId = "guapuruvu";
    public float growthProgress;
    public TreeBalance Balance => EcologySimulation.Config.Tree(speciesId);
    public bool IsMature => growthProgress >= 1f;
    public float ProductionWeight => IsMature ? Balance.production / 100f : 0f;
}
