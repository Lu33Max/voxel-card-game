using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIAudioFeedback : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        if(GetComponent<Button>().interactable)
            AudioManager.Instance.PlaySfx(AudioManager.Instance.ButtonHover);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.Instance.PlaySfx(AudioManager.Instance.ButtonPressed);
    }
}
