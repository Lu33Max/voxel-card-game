using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionDisplayImage : MonoBehaviour
{
    [SerializeField] private Sprite moveIcon;
    [SerializeField] private Sprite attackIcon;

    public void SetIcon(bool isMove)
    {
        var image = GetComponent<Image>();
        image.sprite = isMove ? moveIcon : attackIcon;
    }
}
