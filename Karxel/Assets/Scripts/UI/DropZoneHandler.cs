using UnityEngine;
using UnityEngine.EventSystems;

public class DropZoneHandler : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedCard = eventData.pointerDrag;

        if (droppedCard == null) 
            return;
        
        droppedCard.transform.SetParent(transform);
            
        HandManager.Instance.DiscardCard(droppedCard.GetComponent<Card>());
    }
}