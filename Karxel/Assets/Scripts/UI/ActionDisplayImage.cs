using UnityEngine;
using UnityEngine.UI;

public class ActionDisplayImage : MonoBehaviour
{
    [SerializeField] private Sprite moveIcon;
    [SerializeField] private Sprite attackIcon;
    [SerializeField] private Sprite stunIcon;

    public void SetIcon(ActionDisplayType type)
    {
        var image = GetComponent<Image>();
        image.sprite = type switch
        {
            ActionDisplayType.Move => moveIcon,
            ActionDisplayType.Attack => attackIcon,
            _ => stunIcon
        };
    }
}

public enum ActionDisplayType
{
    Move,
    Attack,
    Stun
}
