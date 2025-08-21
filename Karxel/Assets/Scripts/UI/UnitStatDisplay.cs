using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitStatDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject display;
    [SerializeField] private TextMeshProUGUI unitName;
    [SerializeField] private TextMeshProUGUI unitDescription;
    [SerializeField] private TextMeshProUGUI unitStats;

    private bool _isHovered;
    private bool _hideOnLeave;
    
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

    private void OnUnitHovered(UnitData stats)
    {
        if (stats == null)
        {
            if (!_isHovered) display.SetActive(false);
            else _hideOnLeave = true;
            
            return;
        }
        
        display.SetActive(true);
        
        unitName.text = stats.unitName;
        unitDescription.text = stats.unitDescription;
        unitStats.text = $"{stats.health} LP  {Mathf.Abs(stats.attackDamage)} {(stats.attackDamage > 0 ? "ATK" : "Heal")}";
    }
}
