using UnityEngine;

[ExecuteInEditMode]
public class WorldGridLayout : MonoBehaviour
{
    [Header("Configurações do Grid")]
    [SerializeField] private int columns = 3;
    [SerializeField] private Vector2 spacing = new Vector2(1.5f, 1.5f);

    private void OnTransformChildrenChanged() => UpdateLayout();
    private void OnValidate() => UpdateLayout();

    [ContextMenu("Update Layout")]
    public void UpdateLayout()
    {
        int count = transform.childCount;
        if (count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            int row = i / columns;
            int col = i % columns;


            int itemsInThisRow = Mathf.Min(columns, count - (row * columns));


            float rowWidth = (itemsInThisRow - 1) * spacing.x;
            float xOffset = rowWidth / 2f;

            // X é centralizado com base na largura da linha
            // Y é fixo: primeira linha sempre em 0, segunda em -spacing.y, etc.
            float posX = (col * spacing.x) - xOffset;
            float posY = row * spacing.y;

            transform.GetChild(i).localPosition = new Vector3(posX, posY, 0);
        }
    }
}
