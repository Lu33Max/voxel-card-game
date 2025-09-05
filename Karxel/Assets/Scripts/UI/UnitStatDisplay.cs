using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnitStatDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Stat Display")]
    [SerializeField, Tooltip("UI Element to show upon hover")] private GameObject display;
    [SerializeField, Tooltip("Element that displays the unit name")] private TextMeshProUGUI unitName;
    [SerializeField, Tooltip("Element that displays the unit description")] private TextMeshProUGUI unitDescription;
    [SerializeField, Tooltip("Element that displays the unit stats")] private TextMeshProUGUI unitStats;
    
    [Header("Move Display")]
    [SerializeField] private Transform moveDisplayParent;
    [SerializeField] private Sprite moveSprite;
    [SerializeField] private Sprite attackSprite;
    
    private bool _isHovered;
    private bool _hideOnLeave;
    private List<Image> _moveDisplays = new();
    
    private void Start()
    {
        GridMouseInteraction.UnitHovered.AddListener(OnUnitHovered);
    }

    private void OnDestroy()
    {
        GridMouseInteraction.UnitHovered.RemoveListener(OnUnitHovered);
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        
        if(!_hideOnLeave) return;
        
        display.SetActive(false);
        _hideOnLeave = false;
    }

    private void OnUnitHovered(Unit hoveredUnit)
    {
        if (hoveredUnit == null)
        {
            if (!_isHovered) display.SetActive(false);
            else _hideOnLeave = true;
            
            return;
        }
        
        display.SetActive(true);

        var stats = hoveredUnit.Data;
        
        unitName.text = stats.unitName;
        unitDescription.text = stats.unitDescription;
        unitStats.text =
            $"{stats.health} LP  {Mathf.Abs(stats.attackDamage)} {(stats.attackDamage > 0 ? "ATK" : "Heal")}";

        if (hoveredUnit.owningTeam != GameManager.Instance.localPlayer.team)
        {
            moveDisplayParent.gameObject.SetActive(false);
            return;
        }
        
        moveDisplayParent.gameObject.SetActive(true);
        
        for (var i = 0; i < hoveredUnit.MoveAmountLeft; i++)
        {
            if (_moveDisplays.Count <= i)
            {
                var newDisplay = new GameObject();
                newDisplay.transform.SetParent(moveDisplayParent);

                var rect = newDisplay.AddComponent<RectTransform>();
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 30);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 30);
                
                _moveDisplays.Add(newDisplay.AddComponent<Image>());
            }

            _moveDisplays[i].sprite = GameManager.Instance.gameState switch
            {
                GameState.Movement => moveSprite,
                GameState.Attack => attackSprite,
                _ => null
            };
            
            _moveDisplays[i].gameObject.SetActive(_moveDisplays[i].sprite != null);
        }

        for (var i = hoveredUnit.MoveAmountLeft; i < _moveDisplays.Count; i++)
        {
            _moveDisplays[i].sprite = null;
            _moveDisplays[i].gameObject.SetActive(false);
        }
    }
}
