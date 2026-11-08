using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector2 startMousePos;
    private Vector3 startCardPos;

    public ChoiceManager choiceManager;

    public float decisionThreshold = 120f;
    public float rotationFactor = 0.15f;
    public float swipeForce = 2000f;

    private bool isSwipingAway = false;
    private Vector3 velocity;

    public System.Action<bool> OnDecisionMade;

    private void Awake()
    {
        startCardPos = transform.position;
    }

    private void OnEnable()
    {
        ResetCardPosition();
    }

    public void ResetCardPosition()
    {
        isSwipingAway = false;
        transform.position = startCardPos;
        transform.rotation = Quaternion.identity;
    }

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
            isSwipingAway = true;
            bool decision = deltaX > 0;
            OnDecisionMade?.Invoke(decision);

            float direction = decision ? 1f : -1f;
            velocity = new Vector3(direction * swipeForce, 0, 0);

            if (choiceManager != null)
            {
                choiceManager.AddChoice(decision);
            }

            if (QuestionManager.Instance != null)
            {
                if (decision) QuestionManager.Instance.OnYesClicked();
                else QuestionManager.Instance.OnNoClicked();
            }

            // Reposiciona o card mantendo isSwipingAway = true até a próxima questão
            transform.position = startCardPos;
            transform.rotation = Quaternion.identity;
        }
        else
        {
            transform.position = startCardPos;
            transform.rotation = Quaternion.identity;
        }
    }
}
