using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector2 startMousePos;
    private Vector3 startCardPos;

    public ChoiceManager choiceManager;

    public float decisionThreshold = 150f;
    public float rotationFactor = 0.2f;
    public float swipeForce = 2000f;

    private bool isSwipingAway = false;
    private Vector3 velocity;

    public System.Action<bool> OnDecisionMade;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isSwipingAway) return;

        startMousePos = eventData.position;
        startCardPos = transform.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isSwipingAway) return;

        float deltaX = eventData.position.x - startMousePos.x;

        transform.position = startCardPos + new Vector3(deltaX, 0, 0);
        transform.rotation = Quaternion.Euler(0, 0, deltaX * rotationFactor);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isSwipingAway) return;

        float deltaX = eventData.position.x - startMousePos.x;

        if (Mathf.Abs(deltaX) > decisionThreshold)
        {
            bool decision = deltaX > 0;

            OnDecisionMade?.Invoke(decision);

            isSwipingAway = true;

            float direction = decision ? 1f : -1f;

            velocity = new Vector3(direction * swipeForce, 0, 0);

              Debug.Log(decision ? "SIM (direita)" : "NÃO (esquerda)");

              choiceManager.AddChoice(decision);
        }
        else
        {
            transform.position = startCardPos;
            transform.rotation = Quaternion.identity;
        }
    }

    void Update()
    {
        if (isSwipingAway)
        {
            transform.position += velocity * Time.deltaTime;

            if (Mathf.Abs(transform.position.x) > Screen.width * 1.5f)
            {
                Destroy(gameObject);
            }
        }
    }
}