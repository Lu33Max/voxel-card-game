using System;
using UnityEngine;

public class MouseFollower : MonoBehaviour
{
    [SerializeField] private Vector2 offset = new(10, 10);
    
    private RectTransform _uiElement;

    private void Awake()
    {
        _uiElement = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if(_uiElement == null)
            return;
        
        Vector2 mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _uiElement.parent as RectTransform,
            Input.mousePosition,
            null,
            out mousePosition
        );
        
        _uiElement.anchoredPosition = mousePosition + offset;
    }
    
    public void SetUIElement(GameObject newElement)
    {
        ClearUIElement();
        
        var cardCopy = Instantiate(newElement.gameObject, transform);
        cardCopy.GetComponent<RectTransform>().anchoredPosition = new Vector2();
        cardCopy.GetComponent<RectTransform>().localScale = new Vector3(0.25f, 0.25f, 0.25f);
    }

    public void ClearUIElement()
    {
        for(int i = 0; i < transform.childCount; i++)
            Destroy(transform.GetChild(i).gameObject);
    }
}
