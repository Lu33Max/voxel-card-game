using TMPro;
using UnityEngine;

public class UnitStatDisplay : MonoBehaviour
{
    [SerializeField] private GameObject display;
    [SerializeField] private TextMeshProUGUI unitName;
    [SerializeField] private TextMeshProUGUI unitDescription;
    [SerializeField] private TextMeshProUGUI unitStats;

    public void UpdateVisibility(bool isVisible)
    {
        display.SetActive(isVisible);
    }

    public void UpdateDisplayText(UnitData stats)
    {
        unitName.text = stats.unitName;
        unitDescription.text = stats.unitDescription;
        unitStats.text = $"{stats.health} LP  {Mathf.Abs(stats.attackDamage)} {(stats.attackDamage > 0 ? "ATK" : "Heal")}";
    }
}
